using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimbCollisionReporter : MonoBehaviour
{
	// This script is distributed (automatically by RagdollMovement) to all rigidbodies and reports to the RagdollMovement script if any limb is currently colliding.

	RagdollMovement ragdollControl;
	string[] ignoreCollidersWithTag;

	void OnEnable()
	{
		ragdollControl = transform.root.GetComponentInChildren<RagdollMovement>();
		ignoreCollidersWithTag = ragdollControl.ignoreCollidersWithTag;
	}

	void OnCollisionEnter(Collision collision)
	{
		bool ignore = false;
		if (!(collision.transform.name == "Terrain") && collision.transform.root != this.transform.root)
		{
			foreach (string ignoreTag in ignoreCollidersWithTag)
			{
				if (collision.transform.tag == ignoreTag)
				{
					ignore = true;
					break;
				}
			}

			if (!ignore)
			{
				ragdollControl.numberOfCollisions++;
				ragdollControl.collisionSpeed = collision.relativeVelocity.magnitude;
				//					Debug.Log (collision.transform.name + "\nincreasing");
			}
		}
	}

	void OnCollisionExit(Collision collision)
	{
		bool ignore = false;
		if (!(collision.transform.name == "Terrain") && collision.transform.root != this.transform.root)
		{
			foreach (string ignoreTag in ignoreCollidersWithTag)
			{
				if (collision.transform.tag == ignoreTag)
				{
					ignore = true;
					break;
				}
			}

			if (!ignore)
			{
				ragdollControl.numberOfCollisions--;
				//				Debug.Log (collision.transform.name + "\ndecreasing");
			}
		}
	}
}

