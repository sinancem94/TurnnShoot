using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FABRIKChain
{
    private FABRIKChain parent = null;

    private List<FABRIKChain> children = new List<FABRIKChain>();

    private List<FABRIKEffector> effectors;

    private int layer;

    private float summed_weight;

    public float sqrThreshold = 0.01F;

    public FABRIKChain(FABRIKChain parent, List<FABRIKEffector> effectors, int layer)
    {
        for (int i = 1; i < effectors.Count; i++)
        {
            effectors[i - 1].Length = Vector3.Distance(effectors[i].transform.position, effectors[i - 1].transform.position);
        }

        this.effectors = new List<FABRIKEffector>(effectors);

        this.layer = layer;

        // Add this chain to the parent chain's children
        if (parent != null)
        {
            this.parent = parent;

            parent.children.Add(this);
        }
        
        //Debug.Log(effectors.Count);
    }

    public void CalculateSummedWeight()
    {
        summed_weight = 0.0F;

        foreach (FABRIKChain child in children)
        {
            summed_weight += child.EndEffector.Weight;
        }
    }
    
    public void Backward()
    {
        // Store the original position to be reset below
        Vector3 origin = BaseEffector.Position;

        // Sub-base, average for centroid
        if (children.Count > 1)
        {
            Target /= summed_weight;
        }

        if ((EndEffector.Position - Target).sqrMagnitude > sqrThreshold)
        {
            // Set the end effector Position to Target to calculate the Backward iteration
            EndEffector.Position = Target;

            for (int i = effectors.Count - 2; i >= 0; i--)
            {
                Vector3 direction = Vector3.Normalize(effectors[i].Position - effectors[i + 1].Position);

                effectors[i].Position = effectors[i + 1].Position + direction * effectors[i].Length;
            }
        }

        // Increment parent sub-base's target, to be averaged as above
        if (parent != null)
        {
            parent.Target += BaseEffector.Position * EndEffector.Weight;
        }

        // Reset initial effector to origin
        BaseEffector.Position = origin;
    }

    public void Forward()
    {
        //Debug.Log(EndEffector);
        if(effectors.Count > 1)
            effectors[1].Position = BaseEffector.Position + BaseEffector.Rotation * Vector3.forward * BaseEffector.Length;

        for (int i = 2; i < effectors.Count; i++)
        {
            Vector3 direction = Vector3.Normalize(effectors[i].Position - effectors[i - 1].Position);
                        
            effectors[i - 1].ApplyConstraints(direction);

            effectors[i].Position = effectors[i - 1].Position + effectors[i - 1].Rotation * Vector3.forward * effectors[i - 1].Length;
        }

        // This is a sub-base, reset Target to zero to be recalculated in Backward
        if (children.Count != 0)
        {
            Target = Vector3.zero;

            // In order to constrain a sub-base end effector, we must average the directions of its children
            Vector3 direction = Vector3.zero;

            foreach(FABRIKChain child in children)
            {
                direction += Vector3.Normalize(child.effectors[1].Position - EndEffector.Position);
            }

            direction /= (float)children.Count;

            EndEffector.ApplyConstraints(direction);
        }
    }

    public void ForwardMulti()
    {
        Forward();

        for (int i = 1; i < effectors.Count; i++)
        {
            effectors[i].UpdateTransform();
        }

        foreach (FABRIKChain child in children)
        {
            child.ForwardMulti();
        }
    }

    public bool IsEndChain
    {
        get
        {
            if(effectors.Count > 0)
                return EndEffector.transform.childCount == 0;
            else
                return false;
        }
    }

    public int Layer
    {
        get
        {
            return layer;
        }
    }

    public Vector3 Target
    {
        get;
        set;
    }

    public FABRIKEffector BaseEffector
    {
        get
        {
            return effectors[0];
        }
    }

    public FABRIKEffector EndEffector
    {
        get
        {
            return effectors[effectors.Count - 1];
        }
    }
}
