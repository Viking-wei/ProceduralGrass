using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

//[ExecuteAlways]
public class GrassController : MonoBehaviour
{
    
    [Header("Essential Arguments")]
    public ComputeShader GrassComputeShader;
    public Material GrassMaterial;
    public Material ClumpMaterial;
    public Mesh GrassMeshLod0;
    public Mesh GrassMeshLod1;
    public TerrainData GrassMask;
    public Transform TerrainPosition;
    public Texture2D GlobalWindMap;
    public Transform CharacterPosition;
    public Transform CameraPosition;
    public Camera Cam;
    public List<ClumpArgsStruct> ClumpArgs;
    
    [Header("Cull")]
    public float CullDistancePower=10000;
    public bool EnableDistanceCull=true;
    
    [Header("Grass Properties")]
    public float JitterStrength=0;
    public int GrassResolution;
    [Range(0,1)] public float RotationRandom;
    
    [Header("WindControl")]
    [Range(0,2)] public float WindDirection=1;
    public float GlobalWindStrength=1;
    public float GlobalWindSpeed=1;
    
    public Vector2 Test;
    
    private Bounds _grassInstanceBounds;
    private Texture2D _grassTexture;
    private Texture2D _grassHeightMap;

    private ComputeBuffer _grassClumpArgsBuffer;
    private ComputeBuffer _grassPropertiesBuffer;
    private ComputeBuffer _argsBuffer;
    private ComputeBuffer _trianglesBuffer;
    private ComputeBuffer _colorsBuffer;
    private ComputeBuffer _uvsBuffer;

    private int _kernelIndex;

    #region Properties ID
    internal static readonly int HeightMapId=Shader.PropertyToID("_heightMap");
    internal static readonly int ClumpTexId=Shader.PropertyToID("_clumpTex");
    internal static readonly int GrassMapId=Shader.PropertyToID("_grassMap");
    internal static readonly int TerrainSizeId=Shader.PropertyToID("_terrainSize");
    internal static readonly int TerrainOriginId=Shader.PropertyToID("_terrainOrigin");
    internal static readonly int GrassResolutionId=Shader.PropertyToID("_grassResolution");
    internal static readonly int TrianglesBufferId=Shader.PropertyToID("Triangles");
    internal static readonly int ColorsBufferId=Shader.PropertyToID("Colors");
    internal static readonly int UvsBufferId=Shader.PropertyToID("Uvs");
    internal static readonly int ClumpArgsId=Shader.PropertyToID("ClumpArgs");
    internal static readonly int ClumpNumId = Shader.PropertyToID("_ClumpNum");
    internal static readonly int GrassPropertiesBufferId=Shader.PropertyToID("GrassProperties");
    internal static readonly int PerGrassTileSizeId=Shader.PropertyToID("_perGrassTileSize");
    internal static readonly int JitterStrengthId=Shader.PropertyToID("_jitterStrength");
    internal static readonly int GlobalWindMapId=Shader.PropertyToID("_globalWindMap");
    internal static readonly int GlobalWindStrengthId=Shader.PropertyToID("_globalWindStrength");
    internal static readonly int GlobalWindDirectionId=Shader.PropertyToID("_globalWindDirection");
    internal static readonly int TimeCsID=Shader.PropertyToID("_timeCs");
    internal static readonly int TimeID=Shader.PropertyToID("_Time");
    internal static readonly int GlobalWindSpeedId=Shader.PropertyToID("_globalWindSpeed");
    internal static readonly int RotationRandomId=Shader.PropertyToID("_rotationRandom");
    internal static readonly int CharacterPositionId=Shader.PropertyToID("_characterPosition");
    internal static readonly int CameraPositionId=Shader.PropertyToID("_cameraPosition");
    internal static readonly int WorldToHClipMatrixId=Shader.PropertyToID("_WorldToHClipMatrix");
    internal static readonly int CullDistancePowerId=Shader.PropertyToID("_CullDistancePower");
    #endregion

    public void TestInEditor()
    {
        Debug.Log(GrassMask.bounds.center+" "+GrassMask.bounds.size);
        Debug.Log(GrassMask.heightmapTexture.width);
        Debug.Log(GrassMask.heightmapTexture.height);
    }
    
    private void Awake()
    {
        _grassPropertiesBuffer=new ComputeBuffer(GrassResolution*GrassResolution,14*sizeof(float),ComputeBufferType.Append);
        _grassPropertiesBuffer.SetCounterValue(0);
    }

    private void Start()
    { 
        if(EnableDistanceCull)
            GrassComputeShader.EnableKeyword("DISTANCE_CULL_ENABLED");
        else
            GrassComputeShader.DisableKeyword("DISTANCE_CULL_ENABLED");
        
        _grassTexture=GrassMask.GetAlphamapTexture(1);
        GrassComputeShader.SetTexture(0,GrassMapId,_grassTexture);
        GrassComputeShader.SetTexture(0,GlobalWindMapId,GlobalWindMap);
        
        var heightMap = GrassMask.heightmapTexture;
        _grassHeightMap=new Texture2D(heightMap.width,heightMap.height,TextureFormat.RFloat,false,true);
        _grassHeightMap.filterMode=FilterMode.Bilinear;
        RenderTexture.active=heightMap;
        _grassHeightMap.ReadPixels(new Rect(0,0,heightMap.width,heightMap.height),0,0);
        _grassHeightMap.Apply();
        RenderTexture.active=null;
        GrassComputeShader.SetTexture(0,HeightMapId,_grassHeightMap);
        
        GrassComputeShader.SetVector(TerrainSizeId,new Vector4(GrassMask.size.x,GrassMask.size.y,GrassMask.size.z,GrassMask.heightmapResolution));
        var terrainPos=TerrainPosition.position;
        GrassComputeShader.SetVector(TerrainOriginId,new Vector4(terrainPos.x,terrainPos.y,terrainPos.z,GrassMask.alphamapResolution));
        
        
        var triangles = GrassMeshLod0.triangles;
        _trianglesBuffer=new ComputeBuffer(triangles.Length,sizeof(int));
        _trianglesBuffer.SetData(triangles);
        
        var colors = GrassMeshLod0.colors;
        _colorsBuffer=new ComputeBuffer(colors.Length,4*sizeof(float));
        _colorsBuffer.SetData(colors);
        
        var uvs=GrassMeshLod0.uv;
        _uvsBuffer=new ComputeBuffer(uvs.Length,2*sizeof(float));
        _uvsBuffer.SetData(uvs);
        
        
        GrassMaterial.SetBuffer(TrianglesBufferId,_trianglesBuffer);
        GrassMaterial.SetBuffer(ColorsBufferId,_colorsBuffer);
        GrassMaterial.SetBuffer(UvsBufferId,_uvsBuffer);
        GrassComputeShader.SetBuffer(0,GrassPropertiesBufferId,_grassPropertiesBuffer);
        
        _argsBuffer=new ComputeBuffer(1,4*sizeof(int),ComputeBufferType.IndirectArguments);
        _argsBuffer.SetData(new int[]{triangles.Length,0,0,0});
        
        _grassInstanceBounds=new Bounds(new Vector3(500,200,500),new Vector3(1000,40,1000));


        GenerateVoronoiClumpMap();
        
        SetClumpArgs();
        UpdateComputeArgs();
        
    }

    private void Update()
    {
        UpdateComputeArgs();
        
        Graphics.DrawProceduralIndirect(GrassMaterial,_grassInstanceBounds,MeshTopology.Triangles,_argsBuffer,
            0,null,null,ShadowCastingMode.Off,true,gameObject.layer);
    }

    private void GenerateVoronoiClumpMap()
    {
        ClumpMaterial.SetFloat(ClumpNumId,ClumpArgs.Count());
        Texture2D voronoi = new Texture2D(512, 512, TextureFormat.RGBAFloat, false, true);
        RenderTexture voronoiRT = RenderTexture.GetTemporary(512, 512, 0, RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
        Graphics.Blit(voronoi,voronoiRT,ClumpMaterial);
        RenderTexture.active = voronoiRT;
        voronoi.filterMode = FilterMode.Point;
        voronoi.ReadPixels(new Rect(0,0,512,512),0,0,true);
        voronoi.Apply();
        RenderTexture.active = null;
        GrassComputeShader.SetTexture(0,ClumpTexId,voronoi);
        RenderTexture.ReleaseTemporary(voronoiRT);
    }
    
    private void UpdateComputeArgs()
    {
        GrassComputeShader.SetVector(CameraPositionId,CameraPosition.position);
        GrassComputeShader.SetVector(CharacterPositionId,CharacterPosition.position);
        GrassComputeShader.SetFloat(RotationRandomId,RotationRandom);
        GrassComputeShader.SetFloat(GlobalWindSpeedId,GlobalWindSpeed);
        GrassComputeShader.SetVector(TimeCsID,Shader.GetGlobalVector(TimeID)); 
        GrassComputeShader.SetFloat(GlobalWindStrengthId,GlobalWindStrength);
        GrassComputeShader.SetFloat(GlobalWindDirectionId,WindDirection);
        GrassComputeShader.SetFloat(JitterStrengthId,JitterStrength);
        GrassComputeShader.SetFloat(PerGrassTileSizeId,GrassMask.size.x/GrassResolution);
        _grassPropertiesBuffer.SetCounterValue(0);
        GrassComputeShader.SetInt(GrassResolutionId,GrassResolution);
        GrassComputeShader.SetFloat(CullDistancePowerId,CullDistancePower);
        
        Matrix4x4 projMat = GL.GetGPUProjectionMatrix(Cam.projectionMatrix, false);
        Matrix4x4 VPMatrix = projMat * Cam.worldToCameraMatrix;
        GrassComputeShader.SetMatrix(WorldToHClipMatrixId,VPMatrix);
        
        var groupSize = Mathf.CeilToInt(GrassResolution / 8f);
        GrassComputeShader.Dispatch(0,groupSize,groupSize,1);
        //set the instance the GPU should draw
        ComputeBuffer.CopyCount(_grassPropertiesBuffer,_argsBuffer,sizeof(int));
        
        GrassMaterial.SetBuffer(GrassPropertiesBufferId,_grassPropertiesBuffer);
    }

    private void SetClumpArgs()
    {
        var count=ClumpArgs.Count;
        _grassClumpArgsBuffer = new ComputeBuffer(count, 10 * sizeof(float));
        _grassClumpArgsBuffer.SetData(ClumpArgs);
        GrassComputeShader.SetBuffer(0,ClumpArgsId,_grassClumpArgsBuffer);
    }
    

    private void OnDestroy()
    {
        _grassClumpArgsBuffer?.Dispose();
        _grassPropertiesBuffer?.Dispose();
        _argsBuffer?.Dispose();
        _uvsBuffer?.Dispose();
        _colorsBuffer?.Dispose();
        _trianglesBuffer?.Dispose();
    }
    
    [Serializable]
    public struct ClumpArgsStruct {
        [Range(0,1)]
        public float PullToCentre;
        [Range(0,1)]
        public float PointInSameDirection;
        public float BaseHeight;
        [Range(0,0.5f)]
        public float HeightRandom;
        public float BaseWidth;
        [Range(0,0.1f)]
        public float WidthRandom;
        public float BaseTilt;
        [Range(0,0.2f)]
        public float TiltRandom;
        public float BaseBend;
        [Range(0,0.2f)]
        public float BendRandom;
    };
}


