using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickToBehaviour : MonoBehaviour
{
    public Transform stickTarget = null;
    public Transform stickyPart = null;
    public Vector3 offset = Vector3.zero;
    void Start()
    {
        if (!stickTarget || !stickyPart)
            Debug.LogError("Assign parts");

        if (offset == Vector3.zero)
        {
            //offset.z = stickyPart.lossyScale.z * -3;
        }
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        stickyPart.position = stickTarget.position - offset;
    }
}
