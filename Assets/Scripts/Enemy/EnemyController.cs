using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
	public Skeleton pChar; //player character
	private Vector3 targetPosition;
	
	public CharacterState charState = CharacterState.idle;

	float onAirTimer = 0f;
	float inputMagnitude;
	Vector3 PlayerAppliedForce;


	float currSpeed;
	[SerializeField] float defaultSpeed = 1f;
	[SerializeField] float runSpeed = 3f;

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
		pChar = this.transform.GetComponentInChildren<Skeleton>();
		currSpeed = defaultSpeed;
		targetPosition = FindObjectOfType<ShooterController>().transform.position;
	}

	private void Update()
	{
		//Character skeleton will use playerForce in order to move
		pChar.playerForce = transform.rotation * (transform.position - targetPosition) * currSpeed;
		
		//Vector3 pos = new Vector3(pChar.rootBone.transform.position.x, /*rootBone.transform.position.y - rootAnimatedDiff*/ this.transform.position.y, pChar.rootBone.transform.position.z);
		//this.transform.position = pos;
		
	}


}
