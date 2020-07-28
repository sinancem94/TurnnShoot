using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//Attach this script to ragdoll

public class FollowAnimation : MonoBehaviour
{
#region variables

    public GameObject master; //animated mesh

    Transform[] masterTransforms;
	Transform[] slaveTransforms;

	public Rigidbody[] slaveRigidbodies;
	Transform[] masterRigidbodies;

	Vector3[] rigidbodiesPosToCOM; // assigned in awake. inverse of slave rigidbody rotation according to its world center of mass.
	float reciprocalFixedDeltaTime; // 1f / fixedDeltaTime

	int frameCounter = 0;

	public int followRate = 1;

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	public struct RagdollProfile 
	{
		[Range(0f, 100f)] public float maxTorque; // Limits the world space torque
		[Range(0f, 10f)] public float maxForce; // Limits the force
		[Range(0f, 2000f)] public float maxJointTorque; // Limits the force

		//public List<float> maxTorqueProfile;
		//public List<float> maxForceProfile;
		public List<float> maxJointTorqueProfile;

		[Range(0f, .64f)] public float PTorque; // For all limbs Torque strength
		[Range(0f, 160f)] public float PForce; // For all limbs Force strength

		[Range(0f, .008f)] public float DTorque; // Derivative multiplier to PD controller
		[Range(0f, .064f)] public float DForce;

		public List<float> PTorqueProfile;
		public List<float> PForceProfile;

		public List<float> followRateProfile;
		
		// The ranges are not set in stone. Feel free to extend the ranges
		[Range(0f, 340f)] public float angularDrag; // Rigidbodies angular drag. Unitys parameter
		[Range(0f, 2f)] public float drag; // Rigidbodies drag. Unitys parameter
	};

	public Profile ragdollProfile;
	[HideInInspector] public RagdollProfile myProfile;

	public enum Profile
	{
		Default,
		Dundee,
		Manual
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	[SerializeField] bool torque = false; // Use World torque to controll the ragdoll (if true)
	[SerializeField] bool force = true; // Use force to controll the ragdoll
	public bool hideMaster = true;
	public bool useGravity = true; // Ragdoll is affected by Unitys gravity
	public CollisionDetectionMode collisionDetectionMode = CollisionDetectionMode.Discrete;

	float torqueAngle; 
	Vector3 torqueAxis;
	Vector3 torqueError;
	Vector3 torqueSignal;
	Vector3[] torqueLastError = new Vector3[1];

	[HideInInspector] public Vector3 totalTorqueError; // Total world space angular error of all limbs. This is a vector.

	Vector3 forceSignal;
	Vector3 forceError;
	Vector3[] forceLastError = new Vector3[1];
	[HideInInspector] public Vector3 totalForceError; // Total world position error. a vector.

	public float[] forceErrorWeightProfile = { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f }; // Per limb error weight

	Vector3[] lastRigibbodyPositions = new Vector3[1];

	Quaternion[] startLocalRotation = new Quaternion[1];
	ConfigurableJoint[] configurableJoints = new ConfigurableJoint[1];
	Quaternion[] localToJointSpace = new Quaternion[1];
	JointDrive jointDrive = new JointDrive();


	[Range(0f, 1f)] public float ragdollToFollower = 1f; //RagdollMovement script will change this value when player is airborne or falls collide etc. in order to go full ragdoll or animation follower
	#endregion

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	/// <summary>
	/// AWAKE
	/// Assign profiles
	/// rigidbodies
	/// local and world transforms
	/// if something is not right set needToAssignStuff to false and return
	/// 
	/// </summary>
	private void Awake()
	{
		reciprocalFixedDeltaTime = 1f / Time.fixedDeltaTime; // Cache the reciprocal

		if (!master)
		{
			UnityEngine.Debug.LogWarning("master not assigned in AnimFollow script on " + this.name + "\n");

			//Try to assign manually
			bool found = false;

			foreach(Transform tmpMaster in this.transform.parent.GetComponentInChildren<Transform>() )
			{
				if(tmpMaster.name.ToLower().Contains("master"))
				{
					master = tmpMaster.gameObject;
					found = true;
					break;
				}
			}

			if(!found)
				UnityEngine.Debug.LogError("Could not found master in " + this.name + "\n");
			else
				UnityEngine.Debug.LogWarning("master " + master.name + "assigned manually in " + this.name + " could be wrong\n");
		}

		else if (hideMaster) //master needed some time for debugging
		{
			SkinnedMeshRenderer visible;
			MeshRenderer visible2;
			if (visible = master.GetComponentInChildren<SkinnedMeshRenderer>())
			{
				visible.enabled = false;
				SkinnedMeshRenderer[] visibles;
				visibles = master.GetComponentsInChildren<SkinnedMeshRenderer>();
				foreach (SkinnedMeshRenderer visiblen in visibles)
					visiblen.enabled = false;
			}
			if (visible2 = master.GetComponentInChildren<MeshRenderer>())
			{
				visible2.enabled = false;
				MeshRenderer[] visibles2;
				visibles2 = master.GetComponentsInChildren<MeshRenderer>();
				foreach (MeshRenderer visiblen2 in visibles2)
					visiblen2.enabled = false;
			}
		}


		slaveTransforms = GetComponentsInChildren<Transform>(); // Get all transforms in ragdoll. THE NUMBER OF TRANSFORMS MUST BE EQUAL IN RAGDOLL AS IN MASTER!
		masterTransforms = master.GetComponentsInChildren<Transform>(); // Get all transforms in master. 		

		if (!(masterTransforms.Length == slaveTransforms.Length))
			UnityEngine.Debug.LogError(this.name + " does not have a valid master.\nMaster transform count does not equal slave transform count." + "\n");


		slaveRigidbodies = GetComponentsInChildren<Rigidbody>();

		int rigidCount = slaveRigidbodies.Length;

		myProfile = SetRagdollProfile(ragdollProfile);

		System.Array.Resize(ref masterRigidbodies, rigidCount);

		System.Array.Resize(ref forceErrorWeightProfile, rigidCount);
		System.Array.Resize(ref torqueLastError, rigidCount);
		System.Array.Resize(ref forceLastError, rigidCount);
		
		System.Array.Resize(ref startLocalRotation, rigidCount);
		System.Array.Resize(ref configurableJoints, rigidCount);
		System.Array.Resize(ref localToJointSpace, rigidCount);
		System.Array.Resize(ref rigidbodiesPosToCOM, rigidCount);
		System.Array.Resize(ref lastRigibbodyPositions, rigidCount);
		
		

		int currentJoint = 0;
		int configurableCount = 0;
		int currentTransform = 0;

		foreach (Transform slaveTransform in slaveTransforms) // Sort the transform arrays
		{
			if (slaveTransform.GetComponent<Rigidbody>())
			{
				masterRigidbodies[currentJoint] = masterTransforms[currentTransform];

				lastRigibbodyPositions[currentJoint] = slaveTransform.GetComponent<Rigidbody>().worldCenterOfMass;

				if (slaveTransform.GetComponent<ConfigurableJoint>())
				{
					configurableJoints[currentJoint] = slaveTransform.GetComponent<ConfigurableJoint>();
					Vector3 forward = Vector3.Cross(configurableJoints[currentJoint].axis, configurableJoints[currentJoint].secondaryAxis);
					Vector3 up = configurableJoints[currentJoint].secondaryAxis;
					localToJointSpace[currentJoint] = Quaternion.LookRotation(forward, up);
					startLocalRotation[currentJoint] = slaveTransform.localRotation * localToJointSpace[currentJoint];
					jointDrive = configurableJoints[currentJoint].slerpDrive;
					//jointDrive.mode = JointDriveMode.Position;
					configurableJoints[currentJoint].slerpDrive = jointDrive;
					configurableCount++;
				}
				else if (currentJoint > 0)
					UnityEngine.Debug.LogWarning("Rigidbody " + slaveTransform.name + " on " + this.name + " is not connected to a configurable joint" + "\n");

				Quaternion rot = slaveTransform.rotation;
				rot.eulerAngles = new Vector3(rot.eulerAngles.x * 1, rot.eulerAngles.y * -1, rot.eulerAngles.z * -1);
				
				if(slaveTransform.name.ToLower().Contains("thigh") || slaveTransform.name.ToLower().Contains("calf"))
					rigidbodiesPosToCOM[currentJoint] = Quaternion.Inverse(rot) * (slaveRigidbodies[currentJoint].worldCenterOfMass - slaveTransform.position);//* (slaveTransform.position - slaveRigidbodies[currentJoint].worldCenterOfMass);
				else
					rigidbodiesPosToCOM[currentJoint] = Quaternion.Inverse(slaveTransform.rotation) * (slaveRigidbodies[currentJoint].worldCenterOfMass - slaveTransform.position);

				currentJoint++;
			}

			currentTransform++;
		}

		if (slaveRigidbodies.Length == 0)
			UnityEngine.Debug.LogError("There are no rigid body components on the ragdoll " + this.name + "\n");
		else if (configurableCount == 0)
			UnityEngine.Debug.LogError("There are no configurable joints on the ragdoll " + this.name + "\nDrag and drop the ReplaceJoints script on the ragdoll." + "\n");
		else
		{
			SetJointTorque(myProfile.maxJointTorque);
			JointsLimited(false);
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	void Start()
	{

		foreach (Rigidbody slaveRigidbody in slaveRigidbodies) // Set some of the Unity parameters
		{
			slaveRigidbody.useGravity = useGravity;
			slaveRigidbody.angularDrag = myProfile.angularDrag;
			slaveRigidbody.drag = myProfile.drag;
			//slaveRigidbody.maxAngularVelocity = maxAngularVelocity; eskiden 1000
			slaveRigidbody.collisionDetectionMode = collisionDetectionMode;
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	private void LateUpdate()
	{
		Vector3 pos = new Vector3(slaveRigidbodies[0].transform.position.x, master.transform.position.y, slaveRigidbodies[0].transform.position.z);
		master.transform.position = pos;
	}

	void FixedUpdate()
	{
		FollowMasterAnimation();
	}

	public void FollowMasterAnimation()
	{
		totalTorqueError = Vector3.zero;
		totalForceError = Vector3.zero;

		for (int i = 0; i < slaveRigidbodies.Length; i++) // Do for all rigid bodies
		{
			slaveRigidbodies[i].angularDrag = myProfile.angularDrag; // Set rigidbody drag and angular drag in real-time
			slaveRigidbodies[i].drag = myProfile.drag;
			
			if (torque) // Calculate and apply world torque
			{
				Quaternion targetRotation;

				targetRotation = masterRigidbodies[i].rotation * Quaternion.Inverse(slaveRigidbodies[i].rotation);
				targetRotation.ToAngleAxis(out torqueAngle, out torqueAxis);
				torqueError = FixEuler(torqueAngle) * torqueAxis;

				if (torqueAngle != 360f)
				{
					totalTorqueError += torqueError;
					PDControl(myProfile.PTorque * myProfile.PTorqueProfile[i], myProfile.DTorque, out torqueSignal, torqueError, ref torqueLastError[i], reciprocalFixedDeltaTime);
				}
				else
					torqueSignal = new Vector3(0f, 0f, 0f);

				torqueSignal = Vector3.ClampMagnitude(torqueSignal, myProfile.maxTorque * ragdollToFollower);
				slaveRigidbodies[i].AddTorque(torqueSignal, ForceMode.VelocityChange); // Add torque to the limbs
			}


			if (force) // Calculate and apply world force
			{
				Vector3 masterRigidTransformsWCOM = lastRigibbodyPositions[i] + (masterRigidbodies[i].rotation * rigidbodiesPosToCOM[i]);

				forceError = masterRigidTransformsWCOM - slaveRigidbodies[i].worldCenterOfMass; // Doesn't work if collider is trigger

				totalForceError += forceError * forceErrorWeightProfile[i];

				PDControl(myProfile.PForce * myProfile.PForceProfile[i], myProfile.DForce, out forceSignal, forceError, ref forceLastError[i], reciprocalFixedDeltaTime);
				forceSignal = Vector3.ClampMagnitude(forceSignal, myProfile.maxForce * ragdollToFollower);

				forceSignal.y = 0f;

				slaveRigidbodies[i].AddForce(forceSignal, ForceMode.VelocityChange);
			}

			if (i > 0 && frameCounter % followRate == 0 && frameCounter % myProfile.followRateProfile[i] == 0  && ragdollToFollower >= 0.5f)
				configurableJoints[i].targetRotation = Quaternion.Inverse(localToJointSpace[i]) * Quaternion.Inverse(masterRigidbodies[i].localRotation) * startLocalRotation[i];

			if(frameCounter % followRate == 0)
				lastRigibbodyPositions[i] = slaveRigidbodies[i].transform.position;

		}

		frameCounter++;
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

	float FixEuler(float angle) // For the angle in angleAxis, to make the error a scalar
	{
		if (angle > 180f)
			return angle - 360f;
		else
			return angle;
	}

	void PDControl(float P, float D, out Vector3 signal, Vector3 error, ref Vector3 lastError, float reciDeltaTime) // A PD controller
	{
		// theSignal = P * (theError + D * theDerivative) This is the implemented algorithm.
		signal = P * (error + D * (error - lastError) * reciDeltaTime);
		lastError = error;
	}

	public void SetJointTorque(float positionSpring)
	{
		for (int i = 1; i < configurableJoints.Length; i++) // Do for all configurable joints
		{
			jointDrive.positionSpring = positionSpring * myProfile.maxJointTorqueProfile[i] * ragdollToFollower;
			configurableJoints[i].slerpDrive = jointDrive;
		}
		myProfile.maxJointTorque = positionSpring;
	}

	void JointsLimited(bool limited)
	{
		for (int i = 1; i < configurableJoints.Length; i++) // Do for all configurable joints
		{
			if (limited)
			{
				configurableJoints[i].angularXMotion = ConfigurableJointMotion.Limited;
				configurableJoints[i].angularYMotion = ConfigurableJointMotion.Limited;
				configurableJoints[i].angularZMotion = ConfigurableJointMotion.Limited;
			}
			else
			{
				configurableJoints[i].angularXMotion = ConfigurableJointMotion.Free;
				configurableJoints[i].angularYMotion = ConfigurableJointMotion.Free;
				configurableJoints[i].angularZMotion = ConfigurableJointMotion.Free;
			}
		}
	}

	// Stickman rigidbody configuration
	//
	// 0 : pelvis
	// 1 : spine 2
	// 2 : spine 3 
	// 3 : UpperArm Left
	// 4 : LowerArm Left
	// 5 : UpperArm Right
	// 6 : LowerArm Right
	// 7 : Head
	// 8 : Thigh Left
	// 9 : Calf Left
	// 10 : Thigh Right
	// 11 : Calf Right
	RagdollProfile SetRagdollProfile(Profile prof)
	{
		RagdollProfile thisProf = new RagdollProfile();

		if (slaveRigidbodies.Length != 12)
		{
			Debug.LogError("There are not 12 rigidboides in ragdoll. You must assign Profiles manually..");
			return thisProf;
		}

		switch (prof)
		{
			case Profile.Default:
			case Profile.Dundee:

				// The ranges are not set in stone. Feel free to extend the ranges
				thisProf.maxTorque = 100f; // Limits the world space torque
				thisProf.maxForce = 10f; // Limits the force
				thisProf.maxJointTorque = 700f; // Limits the force

				//thisProf.maxTorqueProfile = new List<float> { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f }; // Individual limits per limb
				//thisProf.maxForceProfile = new List<float> { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };
				thisProf.maxJointTorqueProfile = new List<float>  { 1f, 1f, 1f, .3f, .3f, .3f, .3f, 1f, 1f, 1f, 1f, 1f };

				thisProf.PTorque = 0.3f; // For all limbs Torque strength
				thisProf.PForce = 20f;

				thisProf.DTorque = 0.002f; // Derivative multiplier to PD controller
				thisProf.DForce = 0.02f;

				//	public float[] PTorqueProfile = {20f, 30f, 10f, 30f, 10f, 30f, 30f, 30f, 10f, 30f, 10f}; // Per limb world space torque strength
				thisProf.PTorqueProfile = new List<float> { 20f, 30f, 10f, 30f, 10f, 30f, 30f, 30f, 30f, 10f, 30f, 10f }; // Per limb world space torque strength 

				//thisProf.PForceProfile = new List<float> { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 0.5f, 0.3f, 0.5f, 0.3f };
				thisProf.PForceProfile = new List<float> { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

				thisProf.followRateProfile = new List<float> { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

				// The ranges are not set in stone. Feel free to extend the ranges
				thisProf.angularDrag = 50f; // Rigidbodies angular drag. Unitys parameter
				thisProf.drag = 0.2f; // Rigidbodies drag. Unitys parameter

				break;
			case Profile.Manual:
				break;
		}

		return thisProf;
	}
}