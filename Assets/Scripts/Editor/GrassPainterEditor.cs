using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GrassPainter))]
public class GrassPainterEditor : Editor
{
    private static Vector2 s_lastMousePos=Vector2.zero;
    private void OnEnable()
    {
        // 注册EditorApplication.update事件
        EditorApplication.update += OnEditorUpdate;
    }
    private void OnDisable()
    {
        // 注册EditorApplication.update事件
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnSceneGUI()
    {
        var painter = target as GrassPainter;
        if(painter == null || !painter.IsPainting)
            return;
        
        var painterCenter= GetPainterPos();
        Handles.color = Color.green;
        Handles.DrawSolidDisc(painterCenter, Vector3.up, 5f);
        //SceneView.RepaintAll();
    }
    
    private void OnEditorUpdate()
    {
        s_lastMousePos= Vector2.zero;
    }
    
    public Vector3 GetPainterPos()
    {
        Vector3 painterCenter = Vector3.zero;
        Event e = Event.current;
        var terrain = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        
        if(Physics.Raycast(terrain, out RaycastHit raycastHit))
        {
            painterCenter= raycastHit.point;
        }
        
        return painterCenter;
    }
}
