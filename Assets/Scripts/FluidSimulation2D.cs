using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidSimulation2D : MonoBehaviour
{
    public Shader FsShader;
    public Texture2D InitialTexture2D;
    public Material TargetMaterial;
    public Camera TargetCamera;
    public Transform CharaTransform;
    public int Resolution = 512;
    public float Viscosity = 1.0f;
    public float AdvectSpeed = 1.0f;
    public int DiffusionIteration = 20;
    public float Radius = 10;
    public float ForceScale = 1.0f;

    private Material _fsMaterial;
    private RenderTexture _newVelRT,
        _oldVelRT,
        _divergenceRT,
        _newPressureRT,
        _oldPressureRT,
        _newColorRT,
        _oldColorRT;

    private Vector2 _lastPos = Vector2.zero,
                    _currentPos = Vector2.zero;
    
    #region Properties Id
    internal static readonly int VelocityTexNewId = Shader.PropertyToID("_VelocityTexNew");
    internal static readonly int VelocityTexOldId = Shader.PropertyToID("_VelocityTexOld");
    internal static readonly int PressureTexId = Shader.PropertyToID("_PressureTex");
    internal static readonly int DivergenceTexId = Shader.PropertyToID("_DivergenceTex");
    internal static readonly int AdvectSpeedId = Shader.PropertyToID("_AdvectSpeed");
    internal static readonly int InputPosAForceDirId = Shader.PropertyToID("_InputPosAForceDir");
    internal static readonly int RadiusId = Shader.PropertyToID("_Radius");
    internal static readonly int TexelSizeId = Shader.PropertyToID("_TexelSize");
    internal static readonly int ViscosityId = Shader.PropertyToID("_Viscosity");
    internal static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    #endregion
    
    void Start()
    {
        _newVelRT = new RenderTexture(Resolution, Resolution, 0, RenderTextureFormat.RGHalf);
        _oldVelRT = new RenderTexture(Resolution, Resolution, 0, RenderTextureFormat.RGHalf);
        _divergenceRT = new RenderTexture(Resolution, Resolution, 0, RenderTextureFormat.RHalf);
        _newColorRT = new RenderTexture(Resolution, Resolution, 0, RenderTextureFormat.ARGBHalf);
        _oldColorRT = new RenderTexture(Resolution, Resolution, 0, RenderTextureFormat.ARGBHalf);

        Graphics.Blit(InitialTexture2D, _newColorRT);
        TargetMaterial.SetTexture(MainTexId,InitialTexture2D);
        
        if (FsShader is null)
        {
            Debug.LogError("Please Add Fluid Simulation Shader!");
        }
        _fsMaterial = new Material(FsShader);
        Shader.SetGlobalVector(TexelSizeId, new Vector4(1.0f / Resolution, 1.0f / Resolution, 0, 0));
    }
    
    // private void SimulateLoop()
    // {
    //     #region Advect
    //     _fsMaterial.SetTexture(VelocityTexNewId, _newVelRT);
    //     _fsMaterial.SetTexture(VelocityTexOldId, _newVelRT);
    //     _fsMaterial.SetFloat(AdvectSpeedId, AdvectSpeed);
    //     Graphics.Blit(null, _oldVelRT, _fsMaterial, 0);
    //     SwapRT(ref _newVelRT, ref _oldVelRT);
    //     #endregion
    //     
    //     #region Diffusion
    //     Shader.SetGlobalFloat(ViscosityId, Viscosity);
    //     for (int i = 0; i < DiffusionIteration; ++i)
    //     {
    //         _fsMaterial.SetTexture(VelocityTexNewId, _newVelRT);
    //         _fsMaterial.SetTexture(VelocityTexOldId, _newVelRT);
    //         Graphics.Blit(null, _oldVelRT, _fsMaterial, 1);
    //         SwapRT(ref _newVelRT, ref _oldVelRT);
    //     }
    //     #endregion
    //     
    //     #region Force
    //     var xzVelocity=new Vector2(CharaMotor.BaseVelocity.x,CharaMotor.BaseVelocity.z);
    //     
    //     if(xzVelocity.sqrMagnitude==0) 
    //         _lastPos = GetCharaPosition();
    //     _currentPos = GetCharaPosition();
    //
    //     Vector2 forceDir;
    //     if (CharaMotor.BaseVelocity.y == 0)
    //         forceDir = (_currentPos - _lastPos)*ForceScale;
    //     else
    //         forceDir = Vector2.zero;
    //     
    //     _lastPos = _currentPos;
    //     _fsMaterial.SetVector(InputPosAForceDirId, new Vector4(_currentPos.x, _currentPos.y, forceDir.x, forceDir.y));
    //     _fsMaterial.SetFloat(RadiusId, Radius/Resolution);
    //     _fsMaterial.SetTexture(VelocityTexNewId, _newVelRT);
    //     Graphics.Blit(null, _oldVelRT, _fsMaterial, 2);
    //     SwapRT(ref _newVelRT, ref _oldVelRT);
    //     #endregion
    //     
    //     #region Divergence
    //     _fsMaterial.SetTexture(VelocityTexNewId, _newVelRT);
    //     Graphics.Blit(null, _divergenceRT, _fsMaterial, 3);
    //     
    //     _newPressureRT = new RenderTexture(Resolution, Resolution, 0, RenderTextureFormat.RGHalf);
    //     _oldPressureRT = new RenderTexture(Resolution, Resolution, 0, RenderTextureFormat.RGHalf);
    //     
    //     for(int i=0;i<50;++i)
    //     {
    //         _fsMaterial.SetTexture(PressureTexId, _newPressureRT);
    //         _fsMaterial.SetTexture(DivergenceTexId, _divergenceRT);
    //         Graphics.Blit(null, _oldPressureRT, _fsMaterial, 4);
    //         SwapRT(ref _newPressureRT, ref _oldPressureRT);
    //     }
    //     #endregion
    //     
    //     #region Gradient
    //     _fsMaterial.SetTexture(PressureTexId, _newPressureRT);
    //     _fsMaterial.SetTexture(VelocityTexNewId, _newVelRT);
    //     Graphics.Blit(null, _oldVelRT, _fsMaterial, 5);
    //     SwapRT(ref _newVelRT, ref _oldVelRT);
    //     #endregion
    //     
    //     #region Render
    //     _fsMaterial.SetTexture(VelocityTexNewId, _newVelRT);
    //     _fsMaterial.SetTexture(VelocityTexOldId, _newColorRT);
    //     _fsMaterial.SetFloat(AdvectSpeedId, AdvectSpeed);
    //     Graphics.Blit(null, _oldColorRT, _fsMaterial, 0);
    //     TargetMaterial.SetTexture(MainTexId,_oldColorRT);
    //     SwapRT(ref _newColorRT, ref _oldColorRT);
    //     #endregion
    //     
    //     _newPressureRT.Release();
    //     _oldPressureRT.Release();
    // }
    
    private void SwapRT(ref RenderTexture rt1, ref RenderTexture rt2)
    {
        (rt1, rt2) = (rt2, rt1);
    }

    private Vector2 GetPosition()
    {
        Vector2 currentPos = Vector2.zero;
        
        Ray ray = TargetCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000))
        {
            currentPos = hit.textureCoord;
        }
        return currentPos;
    }

    private Vector2 GetCharaPosition()
    {
        Vector2 currentPos = Vector2.zero;
        
        Ray ray=new Ray(CharaTransform.position,Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000))
        {
            currentPos = hit.textureCoord;
        }
        return currentPos;
    }
    void Update()
    {
        //SimulateLoop();
        // if (Input.GetKeyDown(KeyCode.A))
        //     Debug.Log(GetCharaPosition());

    }

    private void OnDestroy()
    {
        _newVelRT.Release();
        _oldVelRT.Release();
        _divergenceRT.Release();
        _newColorRT.Release();
        _oldColorRT.Release();
    }
}
