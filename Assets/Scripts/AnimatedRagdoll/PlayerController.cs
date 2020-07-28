using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	public Skeleton pChar; //player character
	PlayerAnimation pAnimation; //player animation
	PlayerInput pInputs; //player input
	PlayerFootIK pFoot;

	public CharacterState charState = CharacterState.idle;

	float onAirTimer = 0f;
	float inputMagnitude;
	Vector3 PlayerAppliedForce;


	float currSpeed;
	[SerializeField] float defaultSpeed = 5f;
	[SerializeField] float runSpeed = 15f;

	public enum CharacterState
	{
		idle,
		running,
		jumping,
		falling,
		hitted,
		gettingUp
	}

	private void Start()
	{
		pChar = this.transform.parent.GetComponentInChildren<Skeleton>();

		pInputs = GetComponent<PlayerInput>();
		if (!pInputs)
			Debug.LogError("Assign PlayerInput scrpit to master");

		pAnimation = GetComponent<PlayerAnimation>();
		if (!pAnimation)
		{
			Debug.LogError("Assign PlayerAnimation scrpit to master");
		}

		pFoot = GetComponent<PlayerFootIK>();
		if (!pFoot)
		{
			Debug.LogError("Assign PlayerFootIK scrpit to master");
		}

		currSpeed = defaultSpeed;
	}

	private void Update()
	{
		
		//override current speed to default if character is jumping or falling
		if(charState == CharacterState.jumping || charState == CharacterState.falling)
		{
			currSpeed = defaultSpeed;
		}

		//Character skeleton will use playerForce in order to move
		pChar.playerForce = transform.rotation * pInputs.InputForce * currSpeed;
		inputMagnitude = pInputs.InputForce.z;

		//Set state
		if(inputMagnitude > 0.1f && charState == CharacterState.idle)
		{
			charState = CharacterState.running;
		}
		else if(inputMagnitude < 0.1f && charState == CharacterState.running)
		{
			charState = CharacterState.idle;
		}

		//set animation and speed
		pAnimation.inputMagnitude = inputMagnitude;
		if (pInputs.pressingShift && (pAnimation.running == false || currSpeed != runSpeed))
		{
			currSpeed = runSpeed;
			pAnimation.running = true;
		}
		else if (pInputs.pressingShift == false && (pAnimation.running || currSpeed != defaultSpeed))
		{
			currSpeed = defaultSpeed;
			pAnimation.running = false;
		}
	}

	private void FixedUpdate()
	{
		pFoot.skeletonGroundHeight = pChar.groundPosition.y;

		if (Collided() && charState != CharacterState.hitted)
		{
			charState = CharacterState.hitted;
			pChar.changeState(Skeleton.RagdollState.Ragdoll);
		}

		switch (charState)
		{
			case CharacterState.idle:

				if (Time.time > 2f && Fall())
				{
					charState = CharacterState.falling;
				}

				if (inputMagnitude > 0.1f)
				{
					charState = CharacterState.running;
				}

				//Check if jumped
				if (didJump())
				{
					Debug.Log("Jumping");
					charState = CharacterState.jumping;
					StartCoroutine(WaitForJumpAnim());
				}


				break;
			case CharacterState.running:

				if (Fall())
				{
					charState = CharacterState.falling;
				}

				if (inputMagnitude < 0.1f && charState == CharacterState.running)
				{
					charState = CharacterState.idle;
				}

				//Check if jumped
				if(didJump())
				{
					Debug.Log("Jumping");
					charState = CharacterState.jumping;
					StartCoroutine(WaitForJumpAnim());

					//Jump();
				}

				break;
			case CharacterState.jumping:
			case CharacterState.falling:

				onAirTimer += Time.fixedDeltaTime;
				//Debug.Log(onAirTimer + "  " + groundCollidingFoot);
				if (Grounded())
				{
					charState = CharacterState.idle;
				}


				break;
			
		/*		onAirTimer += Time.fixedDeltaTime;
				//Debug.Log(onAirTimer + "  " + groundCollidingFoot);
				if (Grounded())
				{
					charState = CharacterState.idle;
				}

				break;*/
			case CharacterState.hitted:

				if (pChar.collisionCount > 0)
				{
					if (pChar.animationRate > 0.1f)
					{
						pChar.animationRate = Mathf.Lerp(pChar.animationRate, pChar.animationRange.min, pChar.totalCollisionSpeed / 1000f);
						pChar.maxJointSpring = Mathf.Lerp(pChar.maxJointSpring, pChar.jointSpringRange.min, pChar.totalCollisionSpeed / 1000f);
					}
					else
					{
						charState = CharacterState.gettingUp;
						pChar.FullRagdoll();
					}
				}
				else
				{
					if (pChar.animationRate < pChar.animationRange.max - 0.1f)
					{
						pChar.animationRate = Mathf.Lerp(pChar.animationRate, pChar.animationRange.max, 0.05f);
						pChar.maxJointSpring = Mathf.Lerp(pChar.maxJointSpring, pChar.jointSpringRange.max, 0.05f);
					}
					else
					{
						charState = CharacterState.idle;
						pChar.FullAnimated();
					}
				}


				break;

			case CharacterState.gettingUp:

				if(didGetUp())
				{
					pChar.changeState(Skeleton.RagdollState.Animated);
					charState = CharacterState.idle;
					pChar.FullAnimated();

					onAirTimer = 0f;
					pAnimation.falling = false;
					pAnimation.jumping = false;
					pAnimation.getUp(pChar.rootBone.transform);
				}

			/*	Vector3 rootBoneForward = pChar.rootBone.rotation * rootboneToForward * Vector3.forward;
				if (Vector3.Dot(rootBoneForward, Vector3.down) >= 0f) // Check if ragdoll is lying on its back or front, then transition to getup animation
				{
					if (!animator.GetCurrentAnimatorStateInfo(0).fullPathHash.Equals(hash.getupFront))
						pAnimation.SetBool(hash.frontTrigger, true);
					else // if (!anim.GetCurrentAnimatorStateInfo(0).IsName("GetupFrontMirror"))
						animator.SetBool(hash.frontMirrorTrigger, true);
				}
				else
				{
					if (!animator.GetCurrentAnimatorStateInfo(0).fullPathHash.Equals(hash.getupBack))
						animator.SetBool(hash.backTrigger, true);
					else // if (!anim.GetCurrentAnimatorStateInfo(0).IsName("GetupFrontMirror"))
						animator.SetBool(hash.backMirrorTrigger, true);
				}
				*/
				break;
			default:
				break;
		}

		
	}

	bool didJump()
	{
		if(pInputs.jump && pChar.groundCollidingFoot != 0 && pChar.getState() == Skeleton.RagdollState.Animated)
		{
			pInputs.jump = false;
			pAnimation.jumping = true;

			return true;
		}

		return false;
	}

	IEnumerator WaitForJumpAnim()
	{
		while (pAnimation.jumpGroundEnded == false)
		{
			yield return null;
		}
		//set enden to false again
		pAnimation.jumpGroundEnded = false;
		//Jump
		Jump();

		StopCoroutine(WaitForJumpAnim());
	}

	void Jump()
	{
		pChar.changeState(Skeleton.RagdollState.Ragdoll);

		Vector3 jumpForce = new Vector3(0f, pInputs.jumpForce, 0f);

		pChar.animationRate = pChar.animationRange.max / 10f;//animationRange.min;
		pChar.maxJointSpring = pChar.jointSpringRange.min;

		//pChar.force = false;
		//pChar.follow = false;

		pChar.ApplyForcesToLimbs(jumpForce);
	}

	bool Fall()
	{

		if (pChar.rootDistanceToGround > 1f)
		{
			pChar.changeState(Skeleton.RagdollState.Ragdoll);

			pAnimation.falling = true;

			pChar.animationRate = pChar.animationRange.max / 100f;//animationRange.min;
			pChar.maxJointSpring = pChar.jointSpringRange.min;

			return true;
		}

		return false;
	}

	bool Grounded()
	{
		if (pChar.groundCollidingFoot > 0 && onAirTimer > 1f)
		{
			pChar.changeState(Skeleton.RagdollState.Animated);

			onAirTimer = 0f;
			pAnimation.falling = false;
			pAnimation.jumping = false;

			pChar.animationRate = pChar.animationRange.max;
			pChar.maxJointSpring = pChar.jointSpringRange.max;

			return true;
		}
		return false;
	}

	bool didGetUp()
	{
		if(pChar.rootBone.velocity.magnitude < 0.2f)
		{
			return true;
		}

		return false;
	}

	bool Collided()
	{
		if(pChar.collisionCount > 0)
		{
			return true;
		}

		return false;
	}



}
