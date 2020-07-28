using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[ExecuteInEditMode]
public class RemoveFromName : MonoBehaviour
{
    private string removed = "mixamorig:";

    void Start()
    {
        RemoveNames();
        AssetDatabase.SaveAssets();
        DestroyImmediate(this);
    }

    private void RemoveNames()
    {
        foreach (Transform child in transform.GetComponentsInChildren<Transform>())
        {
            if (child.name.Contains(removed))
            {
               // Debug.Log(child.name);
                child.name = child.name.Remove(0, removed.Length);
               // Debug.Log(child.name);

            }
        }
    }
}
