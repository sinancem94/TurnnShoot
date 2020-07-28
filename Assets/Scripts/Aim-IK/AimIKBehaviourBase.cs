using UnityEngine;

namespace AimIK
{
    [DisallowMultipleComponent]
    public abstract class AimIKBehaviourBase : MonoBehaviour
    {
        #region Variables
            [SerializeField]
            private Transform target;
            [SerializeField]
            private Transform headTarget;

            [SerializeField]
            private bool smoothLookAt;

            [SerializeField]
            private float smoothTime;

            private Vector3 smoothTarget;
            private Vector3 smoothHeadTarget;
        #endregion

        #region SetterGetter
            public Transform Target
            {
                get { return target; }
                set { target = value; }
            }

            public Transform HeadTarget
            {
                get => headTarget;
                set => headTarget = value;
            }

            public bool SmoothLookAt
            {
                get { return smoothLookAt; }
                set { smoothLookAt = value; }
            }

            public float SmoothTime
            {
                get { return smoothTime; }
                set { smoothTime = value; }
            }

            protected Vector3 SmoothTarget
            {
                get { return smoothTarget; }
            }

            protected Vector3 SmoothHeadTarget
            {
                get => smoothHeadTarget;
                set => smoothHeadTarget = value;
            }
        #endregion

        void Awake()
        {
            if (target)
                smoothTarget = target.position;
            else
                smoothTarget = Vector3.zero;

            if (headTarget)
                smoothHeadTarget = headTarget.position;
            else
                smoothHeadTarget = Vector3.zero;

        }

        void Update()
        {
            // Smooth move to target
            if(target)
                smoothTarget = Vector3.Lerp(smoothTarget, target.position, smoothTime);

            if (headTarget)
                smoothHeadTarget = Vector3.Lerp(smoothHeadTarget, headTarget.position, smoothTime);
        }
    }
}
