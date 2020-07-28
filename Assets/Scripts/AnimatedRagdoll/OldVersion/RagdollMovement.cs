using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollMovement : MonoBehaviour
{
    public GameObject master;
    public Rigidbody rootBone;

    PlayerInput inputs;
    FollowAnimation masterFollower;

    public List<Rigidbody> ragdollRigidbodies;
    ForceProfiles myProfiles;

    struct ForceProfiles
    {
        public List<float> WalkingProfile;
        public List<float> OnAirProfile;
    };
    
    public enum WalkingProf
    {
        Dundee
    }

    public enum OnAirProf
    {
        NormalDundee,
        FlyingDundee
    }

    public WalkingProf WalkStyle;
    public OnAirProf OnAirMovement;

    //Vector3 APPLIED_FORCE;
    Vector3 PlayerAppliedForce;
    float LookAtForce; //Head will look at upwards or downwards according to this parameter

    public float movementSpeed = 2f;
    float jumpForce = 25f;

    public string[] ignoreCollidersWithTag = { "IgnoreMe" }; // Colliders with these tag will not affect the ragdolls strength
    [HideInInspector] public int numberOfCollisions;	// Number of colliders currently in contact with the ragdoll
    [HideInInspector] public float collisionSpeed;		// The relative speed of the colliding collider

    public bool LookAtMouse = false;

    void Start()
    {
        if (!master)
            Debug.LogError("Assign master in RagdollMovement at " + this.name);

        inputs = master.GetComponent<PlayerInput>();
        if (!inputs)
            Debug.LogError("Assign PlayerInput scrpit to master");

        if (!rootBone)
            rootBone = GetComponentInChildren<Rigidbody>();

        masterFollower = GetComponent<FollowAnimation>();
        if(!masterFollower)
            Debug.LogError("There is no FollowAnimation scrpit in ragdoll..");


        ragdollRigidbodies = new List<Rigidbody>(GetComponentsInChildren<Rigidbody>());

        foreach (Rigidbody limb in ragdollRigidbodies)
            limb.gameObject.AddComponent<LimbCollisionReporter>();

        SetProfiles();
    }

    private void Update()
    {
        SetForces();
    }

    private void FixedUpdate()
    {
        Jumped();
        ApplyForce();
    }


   public void Jumped()
    {
      /*  if(inputs.pressedJump)
        {
            inputs.pressedJump = false;

            Vector3 jumpPower = new Vector3(0f, jumpForce, 0f);
            foreach (Rigidbody rb in ragdollRigidbodies)
            {
                if (rb.name.ToLower().Contains("thigh") || rb.name.ToLower().Contains("calf"))
                    rb.AddForce(jumpPower, ForceMode.Impulse);
            }
        }*/
    }

    public void ApplyForce()
    {
        for(int i = 0; i < ragdollRigidbodies.Count; i ++)
        {
            //Add input keyboard buttons force
            ragdollRigidbodies[i].AddForce(PlayerAppliedForce * myProfiles.WalkingProfile[i], ForceMode.Impulse);

            //Add lookAtForce to head and spine the head is connected to.
            if ((i == 7 || i == 2) && LookAtMouse)
            {
                ragdollRigidbodies[i].AddForce( new Vector3(0f , LookAtForce * 100f , 0f) , ForceMode.Force);
                LookAtForce = (LookAtForce < 0f) ? 0f : LookAtForce;
                masterFollower.myProfile.PForceProfile[i] = Mathf.Lerp(masterFollower.myProfile.PForceProfile[i], LookAtForce, Time.fixedDeltaTime);
                masterFollower.myProfile.PTorqueProfile[i] = Mathf.Lerp(masterFollower.myProfile.PTorqueProfile[i], LookAtForce, Time.fixedDeltaTime);
                masterFollower.myProfile.maxJointTorqueProfile[i] = Mathf.Lerp(masterFollower.myProfile.maxJointTorqueProfile[i], LookAtForce, Time.fixedDeltaTime);
            }

        }
    }

    //Get all real-time forces that is not applied Unity Physics Engine
    void SetForces()
    {
        PlayerAppliedForce = inputs.InputForce * movementSpeed;
        //LookAtForce = inputs.lookForce;
    }

    void ControllRagdollState()
    {
        //Check number of collisions
        if(numberOfCollisions == ragdollRigidbodies.Count)
        {

        }
    }

    //Set walking and flying profiles
    void SetProfiles()
    {
        myProfiles = new ForceProfiles();

        myProfiles.WalkingProfile = new List<float>(ragdollRigidbodies.Count);
        myProfiles.OnAirProfile = new List<float>(ragdollRigidbodies.Count);

        if (ragdollRigidbodies.Count != 12)
        {
            Debug.LogError("There are not 12 rigidbodies in ragdoll. You must assign Profiles manually..\n" +
                "Current rigidbody count is : " + ragdollRigidbodies.Count);
            return;
        }

        // Stickman rigidbody configuration
        //
        // 0 : pelvis
        // 1 : spine 2
        // 2 : spine 3 
        // 3 : UpperArm Left
        // 4 : LowerArm Left
        // 5 : UpperArm Right
        // 6 : LowerArm Right
        // 7 : Head
        // 8 : Thigh Left
        // 9 : Calf Left
        // 10 : Thigh Right
        // 11 : Calf Right
        //                   

        switch (WalkStyle)
        {
            case WalkingProf.Dundee:
                //                                               0     1      2      3      4      5       6      7      8      9      10     11
                //myProfiles.WalkingProfile.AddRange(new float[] { 1f, 0.2f , 0.2f , 0.05f , 0.1f , 0.05f , 0.1f , 0.1f , 0.3f , 0.3f , 0.3f , 0.3f });
                //myProfiles.WalkingProfile.AddRange(new float[] { 1f, 0.2f, 0.2f, 0f, 0f, 0f, 0f, 0.1f, 0.3f, 0.3f, 0.3f, 0.3f });
                //myProfiles.WalkingProfile.AddRange(new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f });
                myProfiles.WalkingProfile.AddRange(new float[] { 1f, 0.8f, 0.8f, 0.5f, 0.2f, 0.5f, 0.2f, 0.2f, 0.8f, 0.8f, 0.8f, 0.8f });

                break;
            default:
                Debug.LogError("Somethings wrong with Walking profiles");
                break;
        }

        switch(OnAirMovement)
        {
            case OnAirProf.NormalDundee:
                //                                              0     1     2     3     4     5     6     7     8     9     10    11
                myProfiles.OnAirProfile.AddRange(new float[] { 0.2f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f });

                break;
            case OnAirProf.FlyingDundee:
                //                                              0     1     2     3     4     5     6     7     8     9     10    11
                myProfiles.OnAirProfile.AddRange(new float[] { 0.5f, 0.3f, 0.4f, 0.5f, 0.7f, 0.5f, 0.7f, 0.5f, 0.5f, 0.3f, 0.5f, 0.3f });

                break;
            default:
                Debug.LogError("Somethings wrong with OnAir profiles");
                break;
        }

    }
}
