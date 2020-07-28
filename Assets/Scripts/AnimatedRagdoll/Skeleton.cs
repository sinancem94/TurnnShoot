using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

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

public class Skeleton : MonoBehaviour
{
	public GameObject animatedRagdoll;

	//PlayerAnimation pAnimation;
	public Rigidbody rootBone;
	public List<Limb> AllLimbs = new List<Limb>();
	[HideInInspector] public List<Transform> animatedTransforms;

	[Range(0f, 1f)] public float animationRate; //character limbs target rotation will set at this rate
	public RangeAttribute animationRange = new RangeAttribute(0f, 1f);

	[Range(10f, 150f)] public float maxJointSpring;
	public RangeAttribute jointSpringRange = new RangeAttribute(10f, 150f);

	[Range(0f, 100f)] public float Torque = 0.5f; // For all limbs Torque strength
	[Range(0f, 100f)] public float PForce = 5f; // For all limbs Force strength
	[Range(0f, 100f)] public float DForce = 0.05f;

	[SerializeField] bool hideAnimated = true;
	[SerializeField] public bool useGravity = false; // Ragdoll is affected by Unitys gravity

	[Range(0f, 100f)] public float angularDrag = 100f; // Rigidbodies angular drag. Unitys parameter
	[Range(0f, 10f)] public float drag = 10f; // Rigidbodies drag. Unitys parameter

	[SerializeField] public CollisionDetectionMode collisionDetectionMode = CollisionDetectionMode.Discrete;
	[SerializeField] public float BreakForce = Single.PositiveInfinity;
	
	
	[HideInInspector] public bool torque = true; // Use World torque to controll the ragdoll (if true)
	[HideInInspector] public bool force = true; // Use force to controll the ragdoll
	[HideInInspector] public bool follow = true; // Use force to controll the ragdoll

	private readonly object stateLock = new object();

	[SerializeField] RagdollState rState = RagdollState.Animated;

	public GameObject groundObject;
	[HideInInspector] public Vector3 groundPosition;

	private readonly object collisionLock = new object();

	[HideInInspector] public int collisionCount; // other than terrain
	[HideInInspector] public float totalCollisionSpeed;
	[HideInInspector] public int groundCollidingFoot;
	//[HideInInspector] public float headDıstanceToGround;
	[HideInInspector] public float rootDistanceToGround;
	[HideInInspector] public int breakedPartCount;

	[HideInInspector] public Vector3 playerForce; //Please change that in controller in order to move

	//State whether character is animated or ragdoll
	// On animated state ragdoll will follow animated character. Otherwise animated will only animate and will follow ragdoll. 
	public enum RagdollState
	{
		Animated,
		Ragdoll
	}

	private void Awake()
	{
		if (!animatedRagdoll)
		{
			UnityEngine.Debug.LogWarning("animatedRagdoll not assigned in AnimFollow script on " + this.name + "\n");

			//Try to assign manually
			bool found = false;

			foreach (Transform tmpMaster in this.transform.parent.GetComponentInChildren<Transform>())
			{
				if (tmpMaster.name.ToLower().Contains("animated"))
				{
					animatedRagdoll = tmpMaster.gameObject;
					found = true;
					break;
				}
			}

			if (!found)
				UnityEngine.Debug.LogError($"Could not found animatedRagdoll in {this.name} \n" );
			else
				UnityEngine.Debug.LogWarning("animatedRagdoll " + animatedRagdoll.name + "assigned manually in " + this.name + " could be wrong\n");
		}

		
		List<Transform> ragdollTransforms;

		ragdollTransforms = new List<Transform>(GetComponentsInChildren<Transform>()); // Get all transforms in ragdoll. THE NUMBER OF TRANSFORMS MUST BE EQUAL IN RAGDOLL AS IN MASTER!
		animatedTransforms = new List<Transform>(animatedRagdoll.GetComponentsInChildren<Transform>()); // Get all transforms in animatedRagdoll. 		

		if (!(ragdollTransforms.Count == animatedTransforms.Count))
			UnityEngine.Debug.LogError(this.name + " does not have a valid animatedRagdoll.\animatedRagdoll transform count does not equal slave transform count." + "\n");

		var allRigidbodies = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>());

		int currentTransform = 0;
		int currentRb = 0;

		List<Transform> tmpAnimatedRigids = new List<Transform>();

		foreach (var ragdollTransform in ragdollTransforms) // Sort the transform arrays
		{
			if (ragdollTransform.GetComponent<Rigidbody>())
			{
				if (!rootBone)
					rootBone = ragdollTransform.GetComponent<Rigidbody>();

				DistributeLimbs(ragdollTransform, currentRb);

				tmpAnimatedRigids.Add(animatedTransforms[currentTransform]);

				currentRb++;
			}
			currentTransform++;
		}

		//update animated transforms only to correspond ragdolls rigidbodies
		animatedTransforms.Clear();
		animatedTransforms.AddRange(tmpAnimatedRigids);
		tmpAnimatedRigids.Clear();

		if (allRigidbodies.Count == 0 || !rootBone)
			UnityEngine.Debug.LogError("There are no rigid body components on the ragdoll " + this.name + "\n");
		
		

	}


	private void Start()
	{
		if (hideAnimated) //master needed some time for debugging
		{
			HideAnimated();
		}
	}

	//Move Ragdoll according to state. If animated character will follow animatedCharacter fully, otherwise animated will follow ragdoll and force is applied as force to all limbs
	private void FixedUpdate()
	{
		//ControllRagdoll();

		//if (rState == RagdollState.Animated)
		{
			Vector3 newPosition = animatedRagdoll.transform.position + (playerForce * Time.deltaTime);
			animatedRagdoll.transform.position = Vector3.MoveTowards(animatedRagdoll.transform.position, newPosition, 1f * Time.deltaTime);
		}
		///else
		{
			//ApplyForcesToLimbs(playerForce);
			
			//Vector3 pos = new Vector3(rootBone.transform.position.x, /*rootBone.transform.position.y - rootAnimatedDiff*/ animatedRagdoll.transform.position.y, rootBone.transform.position.z);
			//animatedRagdoll.transform.position = pos;
		}
	}

	public RagdollState getState()
	{
		return rState;
	}

	public void changeState(RagdollState newState)
	{
		lock(stateLock)
		{
			if(rState != newState)
				rState = newState;
		}
	}

	//Set collision numbers
	public void LimbCollided(float collSpeed)
	{
		lock(collisionLock)
		{
			collisionCount++;
			//it will be resetted in next fixedUpdate. Total per frame
			totalCollisionSpeed += collSpeed;
		}
	}

	public void LimbCollideStay(float collSpeed)
	{
		lock (collisionLock)
		{
			//it will be resetted in next fixedUpdate. Total per frame
			totalCollisionSpeed += collSpeed;
		}
	}

	public void LimbCollisionExit()
	{
		lock (collisionLock)
		{
			collisionCount--;
		}
	}


	public void ApplyForcesToLimbs(Vector3 force)
	{
		foreach(Limb currlimb in AllLimbs)
		{
			currlimb.ApplyPlayerForce(force);
		}
	}

	
	public void FullAnimated()
	{
		rState = RagdollState.Animated;

		animationRate = animationRange.max;
		maxJointSpring = jointSpringRange.max;

		follow = true;
		force = true;
		torque = true;
	}

	public void FullRagdoll()
	{
		rState = RagdollState.Ragdoll;

		animationRate = animationRange.min;//animationRange.min;
		maxJointSpring = jointSpringRange.min;

		follow = false;
		force = false;
		torque = false;
	}

	void ControllRagdoll()
	{
		//Debug.Log(groundCollidingFoot);
		//Check whether ragdoll or animated
		switch (rState)
		{
			case RagdollState.Animated:

				//1.Check if character is colliding with something. If not colliding break case
				//2.Check forces applied to character. - total collision force - main limbs total collision force - arms legs collision per limb etc.
				//3.If total collision force is bigger than thershold set state as ragdoll (character false) || if there is too much force on one limb (something collided with high speed) set state as ragdoll (character false)
				//4.If character did not fall and main limbs total collision bigger than threshold go to set state as goingRagdoll

				//if checks condition character will fall.After fall character will not retain full force until conditions satisfied (check case RagdollState.Ragdoll:)
				if (collisionCount > 0)
				{
					rState = RagdollState.Ragdoll;
					//FullRagdoll();
				}


				break;
			case RagdollState.Ragdoll:
				//Character will retain full force if this conditions satisfies

				if (collisionCount > 0)
				{
					if (animationRate > 0.1f)
					{
						animationRate = Mathf.Lerp(animationRate, animationRange.min, totalCollisionSpeed / 1000f);
						maxJointSpring = Mathf.Lerp(maxJointSpring, jointSpringRange.min, totalCollisionSpeed / 1000f);
					}
					else
					{
						FullRagdoll();
					}
				}
				else
				{
					if (animationRate < animationRange.max - 0.1f)
					{
						animationRate = Mathf.Lerp(animationRate, animationRange.max, 0.05f);
						maxJointSpring = Mathf.Lerp(maxJointSpring, jointSpringRange.max, 0.05f);
					}
					else
					{
						FullAnimated();
					}
				}

				//if maximumImpact is bigger than threshold character will use control untill collided to ground
				//Debug.Log(collisionCount);
				if (rootBone.velocity.magnitude < 0.5f)
				{
					rState = RagdollState.Animated;
					FullAnimated();
				}


				break;

			default:
				break;
		}

		//Reset total collision in each fixedUpdate
		totalCollisionSpeed = 0f;
	}

	void DistributeLimbs(Transform ragdollTransform,int no)
	{
		Limb limb;

		if(Object.Equals(ragdollTransform,rootBone.transform))
			limb = ragdollTransform.gameObject.AddComponent<LimbRoot>();
		else if(ragdollTransform.name.ToLower().Contains("leg") /*|| ragdollTransform.name.ToLower().Contains("thigh")*/)
			limb = ragdollTransform.gameObject.AddComponent<LimbLeg>();
		else if(ragdollTransform.name.ToLower().Contains("arm"))
			limb = ragdollTransform.gameObject.AddComponent<LimbArm>();
		else if(ragdollTransform.name.ToLower().Contains("head"))
			limb = ragdollTransform.gameObject.AddComponent<LimbHead>();
		else
			limb = ragdollTransform.gameObject.AddComponent<LimbDefault>();

		limb.No = no;

		AllLimbs.Add(limb);
	}

	//IF DEBUGGING THE ANIMATEDRAGDOLL OPEN ANIMATED
	void HideAnimated()
	{
		Debug.Log("Hiding Animated");

		SkinnedMeshRenderer visible = animatedRagdoll.GetComponentInChildren<SkinnedMeshRenderer>();
		MeshRenderer visible2 = animatedRagdoll.GetComponentInChildren<MeshRenderer>();
		if (visible )
		{
			visible.enabled = false;
			SkinnedMeshRenderer[] visibles;
			visibles = animatedRagdoll.GetComponentsInChildren<SkinnedMeshRenderer>();
			foreach (SkinnedMeshRenderer visbl in visibles)
				visbl.enabled = false;
		}
		if (visible2)
		{
			visible2.enabled = false;
			MeshRenderer[] visibles2;
			visibles2 = animatedRagdoll.GetComponentsInChildren<MeshRenderer>();
			foreach (MeshRenderer visiblen2 in visibles2)
				visiblen2.enabled = false;
		}
	}

}
/*
void SetStates()
{
	//Check if character isOnAir
	if (groundCollidingFoot == 0)
	{
		if (aState != AnimatedState.OnAir)
		{
			aState = AnimatedState.OnAir;
			//OnAir();				
		}

		onAirTimer += Time.deltaTime;
	}
	else if (aState == AnimatedState.OnAir)
	{
		aState = AnimatedState.Idle;
		onAirTimer = 0f;
		//Grounded();
	}

	if ((rootBone.velocity.magnitude > 2f) && (PlayerAppliedForce.magnitude > 0.1f) && (aState != AnimatedState.OnAir && aState != AnimatedState.Walking))
	{
		aState = AnimatedState.Walking;
	}
	else if((rootBone.velocity.magnitude < 2f) && (PlayerAppliedForce.magnitude < 0.1f) && (aState != AnimatedState.OnAir && aState != AnimatedState.Idle))
	{
		aState = AnimatedState.Idle;
	}
}*/


/* void ControllRagdoll()
	{
		
		//Check whether ragdoll or animated
		switch (rState)
		{
			case RagdollState.Animated:

				//1.Check if character is colliding with something. If not colliding break case
				//2.Check forces applied to character. - total collision force - main limbs total collision force - arms legs collision per limb etc.
				//3.If total collision force is bigger than thershold set state as ragdoll (character false) || if there is too much force on one limb (something collided with high speed) set state as ragdoll (character false)
				//4.If character did not fall and main limbs total collision bigger than threshold go to set state as goingRagdoll

				//if checks condition character will fall.After fall character will not retain full force until conditions satisfied (check case RagdollState.Ragdoll:)
				if (collisionCount > 0)
				{
					/*if((totalCollisionSpeed > 100f || (totalCollisionSpeed / collisionCount > 15f)))
					{
					//	Debug.LogWarning(totalCollisionSpeed / collisionCount);
					//	Debug.LogWarning(collisionCount + "\n");

						rState = RagdollState.Ragdoll;
						FullRagdoll();
					}
					else */
/*bool totalForceExceeds = ((totalCollisionSpeed > 100f || (totalCollisionSpeed / collisionCount > 15f)));
bool mainLimbForceExceeds = (mainLimbsCollisionCount > 0 && (mainLimbsCollisionSpd / mainLimbsCollisionCount > 5f));

					if (totalForceExceeds || mainLimbForceExceeds)
					{

						rState = RagdollState.GoingRagdoll;

						maximumImpact = totalCollisionSpeed;
						impactForce = totalCollisionSpeed;
					}
				}

				break;
			case RagdollState.GoingRagdoll:
				
				//Check if the character is still colliding. If not colliding set state as goingAnimated and break
				//if colliding ease up the ragdoll smoothly according to speed of collision
				//if characters animationRate is reached 0 set state as ragdoll

				//When main limbs collided with some force ease up the character. Smoothly decrease applied force, torque and decrease animationRate
				if(mainLimbsCollisionCount > 0)
				{
					//if GoingRagdoll returns true character is ragdoll
					if (GoingRagdoll(maximumImpact) && maximumImpact > fallThreshold) //if not ragdoll yet continue setting impactforce and easing up
					{
						FullRagdoll();
rState = RagdollState.Ragdoll;
					} 

					impactForce = totalCollisionSpeed;

					//update maximum applied impact force each frame
					maximumImpact = (impactForce > maximumImpact) ? impactForce : maximumImpact;

				}
				else //character stopped colliding when going to ragdoll. Set state as going animated and break case
				{
					rState = RagdollState.GoingAnimated;
				}
				
				break;
			case RagdollState.Ragdoll:
				//Character will retain full force if this conditions satisfies

				//if maximumImpact is bigger than threshold character will use control untill collided to ground
				if (maximumImpact > fallThreshold && (collisionCount > 2 && rootBone.velocity.magnitude< 1f))
				{
					maximumImpact = 0f;
					rState = RagdollState.Animated;
					FullAnimated();
				}

				break;
			case RagdollState.GoingAnimated:
				
				if(GoingAnimated(maximumImpact))
				{
					rState = RagdollState.Animated;
					FullAnimated();
				}

				break;
			default:
				break;
		}

		//Reset total collision in each fixedUpdate
		totalCollisionSpeed = 0f;
		mainLimbsCollisionSpd = 0f;
	}
	*/
