using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInitiliazer : MonoBehaviour
{
    static GameInitiliazer Initialization;

    CameraSizeHandler camSizeHandler;

    public float GameSpeed;
    public bool useCameraSizeHandler = false;

    void Awake()
    {
        if (Initialization != null && Initialization != this)
            Destroy(this.gameObject);
        else
            Initialization = this;

        DontDestroyOnLoad(this);

#if UNITY_EDITOR || (!UNITY_IPHONE || !UNITY_ANDROID)
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
#elif UNITY_IPHONE || UNITY_ANDROID && !UNITY_EDITOR
        QualitySettings.vSyncCount = 1;
#endif
        if (GameSpeed == 0)
            GameSpeed = 1;
        TimeEngine.gameSpeed = GameSpeed;

        if(useCameraSizeHandler)
        {
            camSizeHandler = new CameraSizeHandler();
            camSizeHandler.SetCameraFieldOfView(Camera.main);
        }

    }


    
}
