using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierRouteScript : MonoBehaviour
{
    [SerializeField]
    private Transform[] controlPoints;

    private Vector3 gizmosPosition;

    public Vector3 startingPosition;
    private void OnEnable()
    {
        controlPoints = new Transform[4];
        int count = 0;
        foreach(Transform t in GetComponentsInChildren<Transform>())
        {
            if(count != 0)
                controlPoints[count - 1] = t;
            count++;
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 curveStartPosDiff = startingPosition - controlPoints[0].position;

        for (float t = 0; t<= 1; t+= 0.05f)
        {
            gizmosPosition = Mathf.Pow(1 - t, 3) * controlPoints[0].position +
                3 * Mathf.Pow(1 - t, 2) * t * controlPoints[1].position +
                3 * (1 - t) * Mathf.Pow(t, 2) * controlPoints[2].position +
                Mathf.Pow(t, 3) * controlPoints[3].position;

            Gizmos.DrawSphere(gizmosPosition + curveStartPosDiff, 0.5f);
            
        }

        Gizmos.DrawLine(controlPoints[0].position, controlPoints[1].position);
        Gizmos.DrawLine(controlPoints[2].position, controlPoints[3].position);
    }
}
