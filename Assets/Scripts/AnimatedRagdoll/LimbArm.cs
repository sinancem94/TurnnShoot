using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimbArm : Limb
{
    /* protected override LimbProfile SetLimbProfile()
     {
         LimbProfile prof = new LimbProfile();

         prof.pFollowForce = 0.8f;
         prof.pFollowTorque = 0.25f;
         prof.pJointSpring = 0.5f;

         //apply less force to arms
         prof.pAppliedForce = 0.5f;

         prof.pFollowRate = 0.5f;

         return prof;
     }*/

    protected override LimbProfile SetLimbProfile()
    {
        LimbProfile prof = new LimbProfile();

        prof.pAnimationRate = 1f;
        prof.pJointSpring = 1f;
        prof.pAppliedForce = 0.75f;

        return prof;
    }

}
