using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GrassPainter : MonoBehaviour
{
    public bool IsPainting=false;
    [HideInInspector] 
    public Vector3 PainterCenter= Vector3.zero;

    private void Start()
    {
        
    }
}
