using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Limb : MonoBehaviour
{
    public int No { get; set; }
    protected Skeleton mySkeleton;

    protected Rigidbody limbRb;
    protected ConfigurableJoint limbJoint;

    protected JointDrive jointDrive = new JointDrive();

    public float JointSpring;

    public float FollowForce;
    public float FollowTorque;
    public float FollowRate;

    public float AppliedForce;

    public struct LimbProfile
    {
        public float pAnimationRate;

        // per limb profiles //////////////
        public float pJointSpring;

      /*  public float pFollowForce;
        public float pFollowTorque;
        public float pFollowRate;*/

        public float pAppliedForce;
        ////////////////////
    }

    public LimbProfile limbProfile;

    protected Vector3 rigidbodyPosToCOM; // assigned in awake. inverse of slave rigidbody rotation according to its world center of mass.
    protected Vector3 lastRigidbodyPosition;

    bool isColliding;
    protected float collisionSpeed;


    //-------  CACHED VALUES
    Quaternion startLocalRotation;
    Quaternion localToJointSpace;

    float torqueAngle;
    Vector3 torqueAxis;
    Vector3 torqueError;
    Vector3 torqueSignal;
    Vector3 torqueLastError;

    protected Vector3 forceSignal;
    protected Vector3 forceError;
    protected Vector3 forceLastError;

    void Start()
    {
        mySkeleton = GetComponentInParent<Skeleton>();

        limbRb = GetComponent<Rigidbody>();
        lastRigidbodyPosition = limbRb.position;

        limbJoint = GetComponent<ConfigurableJoint>();

        if (limbJoint)
        {
            Vector3 forward = Vector3.Cross(limbJoint.axis, limbJoint.secondaryAxis);
            Vector3 up = limbJoint.secondaryAxis;
            localToJointSpace = Quaternion.LookRotation(forward, up);
            startLocalRotation = transform.localRotation * localToJointSpace;
            jointDrive = limbJoint.slerpDrive;
        }

        //if (transform.localPosition.x < 0f)
            rigidbodyPosToCOM = Quaternion.Inverse(transform.rotation) * (limbRb.worldCenterOfMass - transform.position);
       // else
         //   rigidbodyPosToCOM = Quaternion.Inverse(transform.rotation) /* Quaternion.AngleAxis(180f,Vector3.left)*/  * (limbRb.worldCenterOfMass - transform.position);

        if (limbJoint)
            JointLimited(true);

        limbRb.collisionDetectionMode = mySkeleton.collisionDetectionMode;
        limbRb.useGravity = mySkeleton.useGravity;
        limbRb.angularDrag = mySkeleton.angularDrag;
        limbRb.drag = mySkeleton.drag;

        limbRb.interpolation = RigidbodyInterpolation.None;

        limbProfile = SetLimbProfile();
        SetLimb();
        //Debug.Log(this.name + " is setted");
    }

    private void FixedUpdate()
    {
        
        FollowAnimatedLimb(mySkeleton.animatedTransforms[No]);
        ControlLimb();

        //lastRigidbodyPosition += (limbRb.position - lastRigidbodyPosition);

        //   Debug.Log("lastRigibbodyPosition : " + lastRigibbodyPosition + " limbRb.position : " + limbRb.position);
    }


    void OnCollisionEnter(Collision collision)
    {
        if (!(collision.gameObject.CompareTag("Floor")) && collision.transform.root != this.transform.root)
        {
            CollEnter(collision);
        }
        
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!(collision.gameObject.CompareTag("Floor")) && collision.transform.root != this.transform.root)
        {
            CollStay(collision);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (!(collision.gameObject.CompareTag("Floor")) && collision.transform.root != this.transform.root)
        {
            CollExit();
        }
    }

    protected void OnJointBreak(float breakForce)
    {
         Debug.Log($"Joint breaked on {this.name}");

         foreach (var limb in this.transform.GetChild(0).GetComponentsInChildren<Limb>())
         {
           //  if(this.transform.GetChild(0).GetComponentInChildren<Limb>())
             //    this.transform.GetChild(0).GetComponentInChildren<Limb>().OnJointBreak(breakForce);
             limb.OnJointBreak(breakForce);
         }
        
       // if(limbJoint)
         //   Destroy(limbJoint);
         mySkeleton.breakedPartCount++;
         mySkeleton.animationRate *= 0.75f;
         
         this.transform.parent = null;
         Destroy(this);
    }

    #region Protected

    protected abstract LimbProfile SetLimbProfile();

    protected virtual void CollEnter(Collision collision)
    {
        if (!isColliding)
        {
            collisionSpeed = collision.relativeVelocity.magnitude;
            mySkeleton.LimbCollided(collisionSpeed);
            isColliding = true;
        }
    }

    protected virtual void CollStay(Collision collision)
    {
        collisionSpeed = collision.relativeVelocity.magnitude;
        mySkeleton.LimbCollideStay(collisionSpeed);
    }


    protected virtual void CollExit()
    {
        if (isColliding)
        {
            isColliding = false;
            mySkeleton.LimbCollisionExit();
        } 
    }

    //update limbs parameters each fixedUpdate. skeleton animationRate and maxJointSpring is dynamic and changes in skeleton script
    protected virtual void SetLimb()
    {
        //if (FollowRate != limbProfile.pFollowRate)
        {
            FollowRate = mySkeleton.animationRate * limbProfile.pAnimationRate;
            FollowForce = mySkeleton.PForce * mySkeleton.animationRate * limbProfile.pAnimationRate;
            FollowTorque = mySkeleton.Torque * mySkeleton.animationRate * limbProfile.pAnimationRate;
            JointSpring = limbProfile.pJointSpring * mySkeleton.maxJointSpring;// * limbProfile.pAnimationRate;

            AppliedForce = limbProfile.pAppliedForce * mySkeleton.animationRate;

            SetJointTorque();

            limbRb.angularDrag = mySkeleton.angularDrag * mySkeleton.animationRate; // Set rigidbody drag and angular drag in real-time
            limbRb.drag = mySkeleton.drag * mySkeleton.animationRate;

        }
    }


    #endregion

    /// <summary>
    /// Control limbs for world effects. If on air go to full ragdoll etc.
    /// </summary>
    void ControlLimb()
    {
        SetLimb();
    }

    /// <summary>
    /// Calculate torque and force to apply in order to stand like animated ragdoll
    /// Set joints target rotation in order to do same movements like animated ragdoll
    /// </summary>
    /// <param name="followedLimb"></param>
    void FollowAnimatedLimb(Transform followedLimb)
    {
        if (mySkeleton.torque) // Calculate and apply world torque
        {
            AddFollowTorque(followedLimb);
        }


        if (mySkeleton.force) // Calculate and apply world force
        {
            AddFollowForce(followedLimb);
        }

        //Sets joint target rotation to animated limbs
        if (limbJoint && mySkeleton.follow /*&& TimeEngine.fixedFrameCounter % FollowRate == 0*/)
        {
            Quaternion targetRot = Quaternion.Inverse(localToJointSpace) * Quaternion.Inverse(followedLimb.localRotation) * startLocalRotation;
            limbJoint.targetRotation = Quaternion.Slerp(limbJoint.targetRotation, targetRot, FollowRate);
        }

        lastRigidbodyPosition = (transform.position);
    }


    /// <summary>
    /// Calculate force to apply in order to stand like animated ragdoll
    /// </summary>
    /// <param name="followedLimb"></param>
    protected virtual void AddFollowForce(Transform followedLimb)
    {
        //if character is full animated follow that completely. Otherwise add force according to last position of rigidbody (limb will be much looser)
        bool zeroY = false;
        Vector3 toGoPosition;
        if (mySkeleton.getState() != Skeleton.RagdollState.Animated)
        {
            toGoPosition = followedLimb.position;
            zeroY = true;
        }
        else
            toGoPosition = followedLimb.position;

        //forward kinematics
        Vector3 lastRigidTransformsWCOM = toGoPosition + followedLimb.rotation * rigidbodyPosToCOM;
        //forceError is how far is limb from desired position
        forceError = lastRigidTransformsWCOM - limbRb.worldCenterOfMass; // Doesn't work if collider is trigger

        PDControl(FollowForce, mySkeleton.DForce, out forceSignal, forceError, ref forceLastError, TimeEngine.reciprocalFixedDeltaTime);
        
        if(zeroY)
            forceSignal.y = 0f;

        limbRb.AddForce(forceSignal, ForceMode.VelocityChange);
    }

    /// <summary>
    /// Calculate torque to apply in order to rotate like animated ragdoll
    /// </summary>
    /// <param name="followedLimb"></param>
    protected virtual void AddFollowTorque(Transform followedLimb)
    {
        Quaternion targetRotation;
        targetRotation = followedLimb.rotation * Quaternion.Inverse(limbRb.rotation);
        targetRotation.ToAngleAxis(out torqueAngle, out torqueAxis);
        torqueError = FixEuler(torqueAngle) * torqueAxis;

        PDControl(FollowTorque, 0.05f /*mySkeleton.DTorque*/, out torqueSignal, torqueError, ref torqueLastError, TimeEngine.reciprocalFixedDeltaTime);

        limbRb.AddTorque(torqueSignal, ForceMode.Impulse); // Add torque to the limbs
        
        //limbRb.MoveRotation(Quaternion.Slerp(limbRb.rotation, followedLimb.rotation, FollowTorque));
    }


    //Simple PD controller
    public void PDControl(float P, float D, out Vector3 signal, Vector3 error, ref Vector3 lastError, float reciDeltaTime) // A PD controller
    {
        // theSignal = P * (theError + D * theDerivative) This is the implemented algorithm.
        signal = P * (error + D * (error - lastError) * reciDeltaTime);
        lastError = error;
    }

    /// <summary>
    /// This prop will only be called when character is not in animated state. Applied input will directly add force to limbs and anımated wıll follow ragdoll as a result
    /// 
    /// Apply the force which given by player, inputs etc.
    /// Called from skeleton
    /// </summary>
    public virtual void ApplyPlayerForce(Vector3 PlayerAppliedForce)
    {
        limbRb.AddForce(PlayerAppliedForce * AppliedForce, ForceMode.Impulse);
    }
    

    float FixEuler(float angle) // For the angle in angleAxis, to make the error a scalar
    {
        if (angle > 180f)
            return angle - 360f;
        else
            return angle;
    }

    void SetJointTorque()
    {

        if (!limbJoint)
            return;

        jointDrive.positionSpring = JointSpring;
        limbJoint.slerpDrive = jointDrive;
        limbJoint.breakForce = mySkeleton.BreakForce;
    }

    void JointLimited(bool limited)
    {
        if (limited)
        {
            limbJoint.angularXMotion = ConfigurableJointMotion.Limited;
            limbJoint.angularYMotion = ConfigurableJointMotion.Limited;
            limbJoint.angularZMotion = ConfigurableJointMotion.Limited;
        }
        else
        {
            limbJoint.angularXMotion = ConfigurableJointMotion.Free;
            limbJoint.angularYMotion = ConfigurableJointMotion.Free;
            limbJoint.angularZMotion = ConfigurableJointMotion.Free;
        }
    }
    
}
