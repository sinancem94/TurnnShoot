using UnityEngine;
using System.Collections;

public class FABRIKEffector : MonoBehaviour
{
    public float weight = 1.0F;

    [HideInInspector]
    public Vector3 upAxisConstraint = Vector3.up;

    [HideInInspector]
    public Vector3 forwardAxisConstraint = Vector3.forward;

    [HideInInspector]
    public float swingConstraint = float.NaN;

    [HideInInspector]
    public float twistConstraint = float.NaN;
    
    private FABRIKEffector parent = null;

    private Vector3 position;
    private Quaternion rotation;

    public float Weight
    {
        get
        {
            return weight;
        }
    }

    public Vector3 Position
    {
        get
        {
            return parent != null ? position : transform.position;
        }

        set
        {
            position = value;
        }
    }

    public Quaternion Rotation
    {
        get
        {
            return parent != null ? rotation : transform.rotation;
        }

        set
        {
            rotation = value;
        }
    }

    public float Length
    {
        get;
        set;
    }

    public float SwingConstraint
    {
        get
        {
            return swingConstraint * 0.5F * Mathf.Deg2Rad;
        }
    }

    public float TwistConstraint
    {
        get
        {
            return twistConstraint * 0.5F * Mathf.Deg2Rad;
        }
    }

    public bool SwingConstrained
    {
        get
        {
            return !float.IsNaN(swingConstraint);
        }
    }

    public bool TwistConstrained
    {
        get
        {
            return !float.IsNaN(twistConstraint);
        }
    }

    public void ApplyConstraints(Vector3 direction)
    {
        if (parent)
        {
            // Neither axis is constrained; set to LookRotation
            if (!SwingConstrained && !TwistConstrained)
            {
                Rotation = Quaternion.LookRotation(direction, parent.Rotation * Vector3.up);
            }
            else
            {
                // Take our world-space direction and world-space up vector of the constraining rotation
                // Multiply this by the inverse of the constraining rotation to derive a local rotation
                Quaternion rotation_global = Quaternion.LookRotation(parent.Rotation * forwardAxisConstraint, parent.Rotation * upAxisConstraint);
                Quaternion rotation_local = Quaternion.Inverse(rotation_global) * Quaternion.LookRotation(direction);

                Quaternion swing, twist;

                // Decompose our local rotation to swing-twist about the forward vector of the constraining rotation
                rotation_local.Decompose(Vector3.forward, out swing, out twist);
                
                // Constrain the swing and twist quaternions
                if (SwingConstrained)
                {
                    swing = swing.Constrain(SwingConstraint);
                }

                if (TwistConstrained)
                {
                    twist = twist.Constrain(TwistConstraint);
                }

                // Multiply the constrained swing-twist by our constraining rotation to get a world-space rotation
                Rotation = rotation_global * swing * twist;
            }
        }
        else
        {
            Rotation = Quaternion.LookRotation(direction);
        }
    }

    void Awake()
    {
        parent = transform.parent != null ? transform.parent.gameObject.GetComponent<FABRIKEffector>() : null;

        Position = transform.position;
        Rotation = transform.rotation;
    }

    public void UpdateTransform()
    {
        Quaternion X90 = new Quaternion(Mathf.Sqrt(0.5F), 0.0F, 0.0F, Mathf.Sqrt(0.5F));

        transform.rotation = Rotation * X90;
        transform.position = Position;

        DebugDrawBounds();
    }

    private void DebugDrawBounds()
    {
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            return;
        }

        Bounds bounds = meshFilter.mesh.bounds;

        Vector3[] vertices = new Vector3[8];

        vertices[0] = transform.TransformPoint(new Vector3(bounds.max.x, bounds.max.y, bounds.max.z));
        vertices[1] = transform.TransformPoint(new Vector3(-bounds.max.x, bounds.max.y, bounds.max.z));
        vertices[2] = transform.TransformPoint(new Vector3(-bounds.max.x, bounds.max.y, -bounds.max.z));
        vertices[3] = transform.TransformPoint(new Vector3(bounds.max.x, bounds.max.y, -bounds.max.z));
        vertices[4] = transform.TransformPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.min.z));
        vertices[5] = transform.TransformPoint(new Vector3(-bounds.min.x, bounds.min.y, bounds.min.z));
        vertices[6] = transform.TransformPoint(new Vector3(-bounds.min.x, bounds.min.y, -bounds.min.z));
        vertices[7] = transform.TransformPoint(new Vector3(bounds.min.x, bounds.min.y, -bounds.min.z));

        Debug.DrawLine(vertices[0], vertices[1], Color.red, 0.0F, false);
        Debug.DrawLine(vertices[1], vertices[2], Color.red, 0.0F, false);
        Debug.DrawLine(vertices[2], vertices[3], Color.red, 0.0F, false);
        Debug.DrawLine(vertices[3], vertices[0], Color.red, 0.0F, false);

        Debug.DrawLine(vertices[4], vertices[5], Color.red, 0.0F, false);
        Debug.DrawLine(vertices[5], vertices[6], Color.red, 0.0F, false);
        Debug.DrawLine(vertices[6], vertices[7], Color.red, 0.0F, false);
        Debug.DrawLine(vertices[7], vertices[4], Color.red, 0.0F, false);

        Debug.DrawLine(vertices[0], vertices[6], Color.red, 0.0F, false);
        Debug.DrawLine(vertices[1], vertices[7], Color.red, 0.0F, false);
        Debug.DrawLine(vertices[2], vertices[4], Color.red, 0.0F, false);
        Debug.DrawLine(vertices[3], vertices[5], Color.red, 0.0F, false);
    }
}
