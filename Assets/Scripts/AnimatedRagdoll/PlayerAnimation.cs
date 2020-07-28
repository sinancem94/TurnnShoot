using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    AnimHash hash;
    Animator anim;

    public bool jumping;
    public bool falling;
    public bool running;
    public float inputMagnitude;

    //Sended events. This bools reporting animation status to playerController
    public bool jumpGroundEnded;

    void Awake()
    {
        // Setting up the references.
        if (!(anim = GetComponent<Animator>()))
        {
            Debug.LogWarning("Missing Animator on " + this.name);

        }

        if (!(hash = GetComponent<AnimHash>()))
        {
            Debug.LogWarning("Missing Script: HashIDs on " + this.name);

        }

        if (anim.avatar)
            if (!anim.avatar.isValid)
                Debug.LogWarning("Animator avatar is not valid");
    }

    public void Update()
    {
        SetAnimation();
    }

    public void SetAnimation()
    {
       
        if (Mathf.Abs(inputMagnitude) >= 0.1f)
        {
            if (running)            // ... set the speed parameter to 5.5f.
                anim.SetFloat(hash.speedFloat, 5f, 0f, Time.fixedDeltaTime);
            else
                anim.SetFloat(hash.speedFloat, 2.5f, 0f, Time.fixedDeltaTime);
        }
        else
            // Otherwise set the speed parameter to 0.
            anim.SetFloat(hash.speedFloat, 0, 0f, Time.fixedDeltaTime);


        if (anim.GetBool(hash.jumpingBool) == false && jumping)
        {
            anim.SetBool(hash.jumpingBool, jumping);
        }
        else if (anim.GetBool(hash.jumpingBool) && !jumping)
        {
            anim.SetBool(hash.jumpingBool, jumping);
        }

        if (falling && anim.GetBool(hash.fallingBool) == false)
        {
            anim.SetBool(hash.fallingBool, falling);
        }
        else if(!falling && anim.GetBool(hash.fallingBool))
        {
            anim.SetBool(hash.fallingBool, falling);
        }
    }
    public void JumpFromGroundEnd()
    {
        //Time.timeScale = 0f;
        jumpGroundEnded = true;
        //Debug.Log("JumpEnded: "  + " called at: " + Time.time);
    }

    public void getUp(Transform rootBone)
    {
        RaycastHit raycastHit;

        if(Physics.Raycast(rootBone.position,rootBone.forward,out raycastHit,1f))
        {
            if (!anim.GetCurrentAnimatorStateInfo(0).fullPathHash.Equals(hash.getupFront))// if character is front
            {
                anim.SetBool(hash.frontTrigger, true);
            }
        }
        else if(!anim.GetCurrentAnimatorStateInfo(0).fullPathHash.Equals(hash.getupBack))
            anim.SetBool(hash.backTrigger, true);
   
    }
}
