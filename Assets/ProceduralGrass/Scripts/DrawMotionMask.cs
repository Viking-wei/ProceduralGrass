using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DrawMotionMask : MonoBehaviour
{
    public ComputeShader motionMaskCompute;
    public Transform target;
    public float pixelPerMeter = 15f;
    public float softEdge = 0.1f;
    public float fadeSpeed = 0.5f;
    [Header("Debug")]
    public Material DebugMat;
    private RenderTexture _currentMotion;
    private RenderTexture _previousMotion;
    private Vector3 _lastPosition;
    [SerializeField]private Vector3 Offset=Vector3.zero;
    private readonly Vector3Int k_Resolution = new Vector3Int(256, 256, 1);
    
    
    private void Start()
    {
        _lastPosition = target.position;
        CreateMotionMask();
    }

    private void FixedUpdate()
    {
        CalculateOffset();
        DrawMotion();
        DebugFunc();
    }

    private void CreateMotionMask()
    {
        _currentMotion = new RenderTexture(k_Resolution.x, k_Resolution.y, 0, RenderTextureFormat.RG16);
        _currentMotion.enableRandomWrite = true;
        _currentMotion.Create();
        
        _previousMotion = new RenderTexture(k_Resolution.x, k_Resolution.y, 0, RenderTextureFormat.RG16);
        _previousMotion.enableRandomWrite = true;
        _previousMotion.Create();
    }
    
    private void ReleaseMotionMask()
    {
        _currentMotion.Release();
        _previousMotion.Release();
    }

    private void CalculateOffset()
    {
        Offset = target.position-_lastPosition;
        Offset *= pixelPerMeter;
        _lastPosition = target.position;
    }

    private void DrawMotion()
    {
        (_previousMotion, _currentMotion) = (_currentMotion, _previousMotion);
        motionMaskCompute.SetVector("Offset", Offset);
        motionMaskCompute.SetInts("Resolution", k_Resolution);
        motionMaskCompute.SetFloat("FadeSpeed", fadeSpeed);
        motionMaskCompute.SetFloat("SoftEdge", softEdge);
        motionMaskCompute.SetTexture(0, "CurResult", _currentMotion);
        motionMaskCompute.SetTexture(0, "PreResult", _previousMotion);
        motionMaskCompute.DispatchThreads(0, k_Resolution);
    }

    private void DebugFunc()
    {
        DebugMat.SetTexture("_MainTex", _currentMotion);
    }

    public void SetToGlobal(ComputeShader GrassCompute)
    {
        GrassCompute.SetTexture(0,"_MotionMask", _currentMotion);
        GrassCompute.SetVector("_PixelPerMeter", new Vector4(k_Resolution.x, k_Resolution.y, pixelPerMeter, pixelPerMeter));
    }

    private void OnDestroy()
    {
        ReleaseMotionMask();
    }
}
