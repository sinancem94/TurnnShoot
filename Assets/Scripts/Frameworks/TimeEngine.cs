//#define DEBUG_TIME

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeEngine : MonoBehaviour
{
    public static int frameCounter;
    public static int fixedFrameCounter;
    [SerializeField]
    public static float reciprocalFixedDeltaTime; // 1f / fixedDeltaTime

    public static float gameSpeed;

    void Start()
    {
        DefaultSpeed();

        reciprocalFixedDeltaTime = 1f / Time.fixedDeltaTime; // Cache the reciprocal
        frameCounter = 0;
        fixedFrameCounter = 0;
    }

    void Update()
    {
        frameCounter++;

#if DEBUG_TIME
        Debug.Log("Update frame : " + frameCounter);
#endif
    }

    private void FixedUpdate()
    {
        fixedFrameCounter++;

#if DEBUG_TIME
        Debug.Log("Fixed Update frame : " + frameCounter);
#endif
    }

    public static void SpeedDown(float amount)
    {
        if (Time.timeScale - amount >= 0f)
            Time.timeScale -= amount;
        else
            Time.timeScale = 0f;
    }

    public static void SpeedUp(float amount)
    {
        Time.timeScale += amount;
    }

    public static void DefaultSpeed()
    {
        Time.timeScale = gameSpeed;
    }


}
