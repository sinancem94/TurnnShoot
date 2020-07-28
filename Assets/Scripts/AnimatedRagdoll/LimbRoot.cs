using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimbRoot : LimbDefault
{
    RaycastHit raycastHit;

    public LayerMask layerMask;
    float initialDistToGround;

    protected override LimbProfile SetLimbProfile()
    {
        LimbProfile prof = new LimbProfile();

        prof.pAnimationRate = 1f;
        prof.pJointSpring = 1f;
        prof.pAppliedForce = 0.75f;

        return prof;
    }

    private void OnEnable()
    {
        layerMask = layerMask | (1 << gameObject.layer); // Use to avoid raycasts to hit colliders on the character (ragdoll must be on an ignored layer)
        layerMask = ~layerMask;

        Physics.Raycast(transform.position, Vector3.up * -1f, out raycastHit, 20f, layerMask);
        initialDistToGround = raycastHit.distance;
    }

    private void Update()
    {
        Debug.DrawRay(transform.position, Vector3.up * -10f, Color.green);
        bool didHit = Physics.Raycast(transform.position, Vector3.up * -1f, out raycastHit, 10f, layerMask);

        if (didHit)
        {
            if(mySkeleton.groundObject != raycastHit.collider.gameObject)
                mySkeleton.groundObject = raycastHit.collider.gameObject;

            mySkeleton.rootDistanceToGround = raycastHit.distance - initialDistToGround;
            mySkeleton.groundPosition = raycastHit.point;

            if (mySkeleton.rootDistanceToGround > 1f)
            {
                //Debug.LogError(mySkeleton.rootDistanceToGround);
                //mySkeleton.Fall();
            }
            //Debug.LogError(mySkeleton.distanceToGround);
        }
        else
        {

            if (mySkeleton.groundObject != null)
                mySkeleton.groundObject = null;

            mySkeleton.groundPosition = this.transform.position;
            //character exceed maximum fall threshold set as -10
            mySkeleton.rootDistanceToGround = 20f;// raycastHit.distance - initialDistToGround;
            //Debug.LogError(mySkeleton.rootDistanceToGround);
        }
    }

}
