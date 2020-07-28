#define AUTOASSIGNLEGS
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFootIK : MonoBehaviour
{

	void Awake()
	{
		Awake2();
	}

	//Load Foot IK
	private void Start()
	{
		//lerp transform y lerp to maximum
		StartCoroutine(increaseTransformLerp());
	}

	IEnumerator increaseTransformLerp()
	{
		yLerp = 0f;
		
		while(yLerp < TransformYLerp)
		{
			yLerp += 2;
			yield return new WaitForSeconds(0.01f);
		}

		yLerp = TransformYLerp;
		StopCoroutine(increaseTransformLerp());

	}

	void FixedUpdate()
	{
		deltaTime = Time.fixedDeltaTime;
		DoSimpleFootIK();
	}

	void DoSimpleFootIK()
	{
		if (userNeedsToFixStuff)
		{
			return;
		}

		//before sending rays to ground adjust height if animated is not on object which ragdoll is
		if(Mathf.Abs(transform.position.y - skeletonGroundHeight) > 0.1f)
		{
			transform.position = new Vector3(transform.position.x, skeletonGroundHeight, transform.position.z);
		}

		ShootIKRays();

		if(didLeftHit || didRightHit)
			PositionFeet();


	}

	public float skeletonGroundHeight;

	bool didRightHit;
	bool didLeftHit;

	// Declare properties
	public LayerMask layerMask;
	public string[] ignoreLayers = { "ragdoll" };
	float deltaTime;

	RaycastHit raycastHitLeftFoot;
	RaycastHit raycastHitRightFoot;
	RaycastHit raycastHitToe;
	[Range(1f, 20f)] public float raycastLength = 5f; // Character must not be higher above ground than this.
	[Range(.2f, .9f)] public float maxStepHeight = .5f;

	[Range(0f, 1f)] public float footIKWeight = 1f;

	[Range(1f, 100f)] public float footNormalLerp = 40f; // Lerp smoothing of foot normals
	[Range(1f, 100f)] public float footTargetLerp = 40f; // Lerp smoothing of foot position
	[Range(1f, 100f)] public float TransformYLerp = 20f;
	[Range(1f, 100f)] float yLerp = 20f; // Lerp smoothing of transform following terrain

	[Range(0f, 1f)] public float maxIncline = .8f; // Foot IK not aktiv on inclines steeper than arccos(maxIncline);

	public bool followTerrain = true;
	[HideInInspector] public bool userNeedsToFixStuff = false;

	void Awake2()
	{
		foreach (string ignoreLayer in ignoreLayers)
		{
			layerMask = layerMask | (1 << LayerMask.NameToLayer(ignoreLayer)); // Use to avoid IK raycasts to hit colliders on the character (ragdoll must be on an ignored layer)
		}
		layerMask = ~layerMask;

#if AUTOASSIGNLEGS
			// For the auto assigning to work the characters legs must be the same transform structure as Ethan in the example scene and
			// the character should be humanoid with feets named something like RightFoot and LeftFoot.
			Transform[] characterTransforms = GetComponentsInChildren<Transform>();
			for (int n = 0; n < characterTransforms.Length; n++)
			{
				if ((characterTransforms[n].name.ToLower().Contains("foot") && characterTransforms[n].name.ToLower().Contains("l")))
				{
					leftToe = characterTransforms[n + 1];
					leftFoot = characterTransforms[n];
					leftCalf = characterTransforms[n - 1];
					leftThigh = characterTransforms[n - 2];
					if (rightFoot)
						break;
				}
				if (characterTransforms[n].name.ToLower().Contains("foot") && characterTransforms[n].name.ToLower().Contains("r"))
				{
					rightToe = characterTransforms[n + 1];
					rightFoot = characterTransforms[n];
					rightCalf = characterTransforms[n - 1];
					rightThigh = characterTransforms[n - 2];
					if (leftFoot)
						break;
				}
			}
			if (!(leftToe && rightToe))
			{
				Debug.LogWarning("Auto assigning of legs failed." + "\n");
				userNeedsToFixStuff = true;
				return;
			}
#endif

		thighLength = (rightThigh.position - rightCalf.position).magnitude;
		thighLengthSquared = (rightThigh.position - rightCalf.position).sqrMagnitude;
		calfLength = (rightCalf.position - rightFoot.position).magnitude;
		calfLengthSquared = (rightCalf.position - rightFoot.position).sqrMagnitude;
		reciDenominator = -.5f / calfLength / thighLength;

#if AUTOASSIGNFOOTHEIGHT
			// Character should be spawned upright (line from feets to head points as vector3.up)
			footHeight = (rightFoot.position.y + leftFoot.position.y) * .5f - transform.position.y;
#else
		if (footHeight == 0f)
			footHeight = .132f;
#endif
	}


	void ShootIKRays()
	{
		leftFootPosition = new Vector3(leftFoot.position.x, /*leftFootPosition.y*/leftFoot.position.y, leftFoot.position.z);
		rightFootPosition = new Vector3(rightFoot.position.x, /*rightFootPosition.y*/rightFoot.position.y, rightFoot.position.z);
		didRightHit = true;
		didLeftHit = true;

		// Shoot ray to determine where the feet should be placed.
		Debug.DrawRay(rightFootPosition + Vector3.up * maxStepHeight, Vector3.down * raycastLength, Color.green);
		if (!Physics.Raycast(rightFootPosition + Vector3.up * maxStepHeight, Vector3.down, out raycastHitRightFoot, raycastLength, layerMask))
		{
			didRightHit = false;
			//raycastHitRightFoot.normal = Vector3.up;
			raycastHitRightFoot.point = Vector3.zero;
		}
		//Debug.Log(rightToe.position + "   " + rightFoot.position);

		footForward = rightToe.position - rightFoot.position;
		footForward = new Vector3(footForward.x, 0f, footForward.z);
		footForward = Quaternion.FromToRotation(Vector3.up, raycastHitRightFoot.normal) * footForward;
		if (!Physics.Raycast(rightFootPosition + footForward + Vector3.up * maxStepHeight, Vector3.down, out raycastHitToe, maxStepHeight * 2f, layerMask))
		{
			raycastHitToe.normal = raycastHitRightFoot.normal;
			raycastHitToe.point = raycastHitRightFoot.point + footForward;
		}
		else
		{

			if (raycastHitRightFoot.point.y < raycastHitToe.point.y - footForward.y)
				raycastHitRightFoot.point = new Vector3(raycastHitRightFoot.point.x, raycastHitToe.point.y - footForward.y, raycastHitRightFoot.point.z);

			// Put avgNormal in foot normal
			raycastHitRightFoot.normal = (raycastHitRightFoot.normal + raycastHitToe.normal).normalized;
		}
		
		//			Debug.DrawRay(leftFootPosition + Vector3.up * maxStepHeight, Vector3.down * raycastLength , Color.red);
		if (!Physics.Raycast(leftFootPosition + Vector3.up * maxStepHeight, Vector3.down, out raycastHitLeftFoot, raycastLength, layerMask))
		{
			didLeftHit = false;
			//raycastHitLeftFoot.normal = Vector3.up;
			raycastHitLeftFoot.point = Vector3.zero;//leftFoot.position - raycastLength * Vector3.up;
		}
		
		footForward = leftToe.position - leftFoot.position;
		footForward = new Vector3(footForward.x, 0f, footForward.z);
		footForward = Quaternion.FromToRotation(Vector3.up, raycastHitLeftFoot.normal) * footForward;
		if (!Physics.Raycast(leftFootPosition + footForward + Vector3.up * maxStepHeight, Vector3.down, out raycastHitToe, maxStepHeight * 2f, layerMask))
		{
			raycastHitToe.normal = raycastHitLeftFoot.normal;
			raycastHitToe.point = raycastHitLeftFoot.point + footForward;
		}
		else
		{
			if (raycastHitLeftFoot.point.y < raycastHitToe.point.y - footForward.y)
				raycastHitLeftFoot.point = new Vector3(raycastHitLeftFoot.point.x, raycastHitToe.point.y - footForward.y, raycastHitLeftFoot.point.z);

			// Put avgNormal in foot normal
			raycastHitLeftFoot.normal = (raycastHitLeftFoot.normal + raycastHitToe.normal).normalized;
		}
		
		// Do not tilt feet if on to steep an angle
		if (raycastHitLeftFoot.normal.y < maxIncline)
		{
			raycastHitLeftFoot.normal = Vector3.RotateTowards(Vector3.up, raycastHitLeftFoot.normal, Mathf.Acos(maxIncline), 0f);
		}
		if (raycastHitRightFoot.normal.y < maxIncline)
		{
			raycastHitRightFoot.normal = Vector3.RotateTowards(Vector3.up, raycastHitRightFoot.normal, Mathf.Acos(maxIncline), 0f);
		}
		
		float minDistance = Mathf.Min(raycastHitLeftFoot.distance, raycastHitRightFoot.distance);
		float lerp = (minDistance > 1f) ? yLerp / (minDistance * 5f) : yLerp;  
		if (followTerrain && (didRightHit || didLeftHit))
		{
			transform.position = new Vector3(transform.position.x, Mathf.Lerp(transform.position.y, Mathf.Min(raycastHitLeftFoot.point.y, raycastHitRightFoot.point.y), lerp * deltaTime), transform.position.z);
							Debug.DrawLine(raycastHitLeftFoot.point, raycastHitRightFoot.point);
		}
	}




	//not working properly

#if AUTOASSIGNFOOTHEIGHT
		float footHeight; // Is set in Awake as the difference between foot positon and transform.position. At Awake the character's transform.position must be level with feet soles.
#else
	public float footHeight; // Set manually in inspector
#endif

#if AUTOASSIGNLEGS
	Transform leftToe;
	Transform leftFoot;
	Transform leftCalf;
	Transform leftThigh;
	Transform rightToe;
	Transform rightFoot;
	Transform rightCalf;
	Transform rightThigh;
#else
	public Transform leftToe; // Set manually in inspector
	public Transform leftFoot;
	public Transform leftCalf;
	public Transform leftThigh;
	public Transform rightToe;
	public Transform rightFoot;
	public Transform rightCalf;
	public Transform rightThigh;
#endif

	Quaternion leftFootRotation;
	Quaternion rightFootRotation;

	Vector3 leftFootTargetPos;
	Vector3 leftFootTargetNormal;
	Vector3 lastLeftFootTargetPos;
	Vector3 lastLeftFootTargetNormal;
	Vector3 rightFootTargetPos;
	Vector3 rightFootTargetNormal;
	Vector3 lastRightFootTargetPos;
	Vector3 lastRightFootTargetNormal;

	Vector3 footForward;

	float leftLegTargetLength;
	float rightLegTargetLength;
	float thighLength;
	float thighLengthSquared;
	float calfLength;
	float calfLengthSquared;
	float reciDenominator;

	float leftKneeAngle;
	float leftThighAngle;
	float rightKneeAngle;
	float rightThighAngle;

	[HideInInspector] public Vector3 leftFootPosition;
	[HideInInspector] public Vector3 rightFootPosition;


	void PositionFeet()
	{
		float leftLegTargetLength;
		float rightLegTargetLength;
		float leftKneeAngle;
		float rightKneeAngle;

		// Save before PositionFeet
		Quaternion leftFootRotation = leftFoot.rotation;
		Quaternion rightFootRotation = rightFoot.rotation;

		float leftFootElevationInAnim = Vector3.Dot(leftFoot.position - transform.position, transform.up) - footHeight;
		float rightFootElevationInAnim = Vector3.Dot(rightFoot.position - transform.position, transform.up) - footHeight;

		// Here goes the maths			
		leftFootTargetNormal = Vector3.Lerp(Vector3.up, raycastHitLeftFoot.normal, footIKWeight);
		leftFootTargetNormal = Vector3.Lerp(lastLeftFootTargetNormal, leftFootTargetNormal, footNormalLerp * deltaTime);
		lastLeftFootTargetNormal = leftFootTargetNormal;
		rightFootTargetNormal = Vector3.Lerp(Vector3.up, raycastHitRightFoot.normal, footIKWeight);
		rightFootTargetNormal = Vector3.Lerp(lastRightFootTargetNormal, rightFootTargetNormal, footNormalLerp * deltaTime);
		lastRightFootTargetNormal = rightFootTargetNormal;

		leftFootTargetPos = raycastHitLeftFoot.point;
		leftFootTargetPos = Vector3.Lerp(lastLeftFootTargetPos, leftFootTargetPos, footTargetLerp * deltaTime);
		lastLeftFootTargetPos = leftFootTargetPos;
		leftFootTargetPos = Vector3.Lerp(leftFoot.position, leftFootTargetPos + leftFootTargetNormal * footHeight + leftFootElevationInAnim * Vector3.up, footIKWeight);

		rightFootTargetPos = raycastHitRightFoot.point;
		rightFootTargetPos = Vector3.Lerp(lastRightFootTargetPos, rightFootTargetPos, footTargetLerp * deltaTime);
		lastRightFootTargetPos = rightFootTargetPos;
		rightFootTargetPos = Vector3.Lerp(rightFoot.position, rightFootTargetPos + rightFootTargetNormal * footHeight + rightFootElevationInAnim * Vector3.up, footIKWeight);


		leftLegTargetLength = Mathf.Min((leftFootTargetPos - leftThigh.position).magnitude, calfLength + thighLength - .01f);
		leftLegTargetLength = Mathf.Max(leftLegTargetLength, .2f);
		leftKneeAngle = Mathf.Acos((Mathf.Pow(leftLegTargetLength, 2f) - calfLengthSquared - thighLengthSquared) * reciDenominator);
		leftKneeAngle *= Mathf.Rad2Deg;
		float currKneeAngle;
		Vector3 currKneeAxis;
		Quaternion currKneeRotation = Quaternion.FromToRotation(leftCalf.position - leftThigh.position, leftFoot.position - leftCalf.position);
		currKneeRotation.ToAngleAxis(out currKneeAngle, out currKneeAxis);
		if (currKneeAngle > 180f)
		{
			currKneeAngle = 360f - currKneeAngle;
			currKneeAxis *= -1f;
		}
		leftCalf.Rotate(currKneeAxis, 180f - leftKneeAngle - currKneeAngle, Space.World);
		leftThigh.rotation = Quaternion.FromToRotation(leftFoot.position - leftThigh.position, leftFootTargetPos - leftThigh.position) * leftThigh.rotation;

		rightLegTargetLength = Mathf.Min((rightFootTargetPos - rightThigh.position).magnitude, calfLength + thighLength - .01f);
		rightLegTargetLength = Mathf.Max(rightLegTargetLength, .2f);
		rightKneeAngle = Mathf.Acos((Mathf.Pow(rightLegTargetLength, 2f) - calfLengthSquared - thighLengthSquared) * reciDenominator);
		rightKneeAngle *= Mathf.Rad2Deg;
		currKneeRotation = Quaternion.FromToRotation(rightCalf.position - rightThigh.position, rightFoot.position - rightCalf.position);
		currKneeRotation.ToAngleAxis(out currKneeAngle, out currKneeAxis);
		if (currKneeAngle > 180f)
		{
			currKneeAngle = 360f - currKneeAngle;
			currKneeAxis *= -1f;
		}
		rightCalf.Rotate(currKneeAxis, 180f - rightKneeAngle - currKneeAngle, Space.World);
		rightThigh.rotation = Quaternion.FromToRotation(rightFoot.position - rightThigh.position, rightFootTargetPos - rightThigh.position) * rightThigh.rotation;

		leftFootPosition = leftFoot.position; // - leftFootTargetNormal * footHeight;
		rightFootPosition = rightFoot.position; // - rightFootTargetNormal * footHeight;

		leftFoot.rotation = Quaternion.FromToRotation(transform.up, leftFootTargetNormal) * leftFootRotation;
		rightFoot.rotation = Quaternion.FromToRotation(transform.up, rightFootTargetNormal) * rightFootRotation;
	}
	

	
}

