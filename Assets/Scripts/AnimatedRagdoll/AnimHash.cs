﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimHash : MonoBehaviour
{
	// Add this script to the master
	public readonly int version = 7; // The version of this script

	// Here we store the hash tags for various strings used in our animators.
	public int dyingState;
	public int locomotionState;
	public int deadBool;
	public int speedFloat;
	public int sneakingBool;
	public int fallingBool;

	public int jumpingBool;

	public int frontTrigger;
	public int backTrigger;
	public int frontMirrorTrigger;
	public int backMirrorTrigger;

	public int idle;

	public int getupFront;
	public int getupBack;
	public int getupFrontMirror;
	public int getupBackMirror;

	public int anyStateToGetupFront;
	public int anyStateToGetupBack;
	public int anyStateToGetupFrontMirror;
	public int anyStateToGetupBackMirror;

	void Awake()
	{
		dyingState = Animator.StringToHash("Base Layer.Dying");
		locomotionState = Animator.StringToHash("Base Layer.Locomotion");
		deadBool = Animator.StringToHash("Dead");
		sneakingBool = Animator.StringToHash("Sneaking");

		fallingBool = Animator.StringToHash("Falling");
		jumpingBool = Animator.StringToHash("Jumping");
		idle = Animator.StringToHash("Base Layer.Idle");

		// These are used by the RagdollControll script and must exist exactly as below
		speedFloat = Animator.StringToHash("Speed");

		frontTrigger = Animator.StringToHash("FrontTrigger");
		backTrigger = Animator.StringToHash("BackTrigger");
		frontMirrorTrigger = Animator.StringToHash("FrontMirrorTrigger");
		backMirrorTrigger = Animator.StringToHash("BackMirrorTrigger");

		getupFront = Animator.StringToHash("Base Layer.GetupFront");
		getupBack = Animator.StringToHash("Base Layer.GetupBack");
		getupFrontMirror = Animator.StringToHash("Base Layer.GetupFronMirror");
		getupBackMirror = Animator.StringToHash("Base Layer.GetupBackMirror");

		anyStateToGetupFront = Animator.StringToHash("Entry -> Base Layer.GetupFront");
		anyStateToGetupBack = Animator.StringToHash("Entry -> Base Layer.GetupBack");
		anyStateToGetupFrontMirror = Animator.StringToHash("Entry -> Base Layer.GetupFrontMirror");
		anyStateToGetupBackMirror = Animator.StringToHash("Entry -> Base Layer.GetupBackMirror");
	}
}
