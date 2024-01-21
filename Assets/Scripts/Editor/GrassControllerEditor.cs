using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GrassController))]
public class GrassControllerEditor : Editor
{
    private GrassController _grassController;
    private void OnEnable()
    {
        _grassController = target as GrassController;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        if (GUILayout.Button("GetInfo"))
        {
            _grassController.TestInEditor();
        }
    }
}
