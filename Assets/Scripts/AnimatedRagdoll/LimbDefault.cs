using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimbDefault : Limb
{
    protected override LimbProfile SetLimbProfile()
    {
        LimbProfile prof = new LimbProfile();

        prof.pAnimationRate = 1f;
        prof.pJointSpring = 1f;
        prof.pAppliedForce = 0.75f;

        return prof;
    }

}
