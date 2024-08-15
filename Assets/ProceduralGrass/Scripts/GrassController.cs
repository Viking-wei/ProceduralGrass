using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Playables;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class GrassController : MonoBehaviour
{
    public GrassConfig GrassConfigs;
    public DrawMotionMask MotionMask;
    public Transform CharacterPosition;
    
    private Material _grassMaterial;
    private Material _clumpMaterial;
    private Transform _terrainPosition;
    private Mesh _grassMeshLod0;
    private Mesh GrassMeshLod1;
    private Mesh GrassMeshLod2;
    private ComputeShader _grassComputeShader;
    private TerrainData _grassMask;
    private Transform _cameraPosition;
    private Camera _camera;
    private List<ClumpArgsStruct> _clumpArgs;
    
    private float _cullDistancePower=10000;
    private readonly bool _enableDistanceCull=true;
    
    [Header("Random")]
    public float JitterStrength=0;
    public int GrassResolution;
    [Range(0,1)] public float RotationRandom;
    
    [Header("Wind")]
    public Texture2D GlobalWindMap;
    public float GlobalWindStrength=1;
    public float GlobalWindSpeed=1;
    
    private Bounds _grassInstanceBounds;
    private Texture2D _grassHeightMap;

    private ComputeBuffer _grassClumpArgsBuffer;
    private ComputeBuffer _grassPropertiesBuffer;
    private ComputeBuffer _argsBuffer;
    private ComputeBuffer _trianglesBuffer;
    private ComputeBuffer _colorsBuffer;
    private ComputeBuffer _uvsBuffer;

    private int _kernelIndex;

    #region Properties ID
    internal static readonly int GrassMaskChannelId=Shader.PropertyToID("_GrassMaskChannel");
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
        // var obj = FindObjectOfType<TerrainCollider>();
        // Debug.Log(obj.terrainData);
    }

    private void EnableKeyWord()
    {
        if(_enableDistanceCull)
            _grassComputeShader.EnableKeyword("DISTANCE_CULL_ENABLED");
        else
            _grassComputeShader.DisableKeyword("DISTANCE_CULL_ENABLED");
    }

    private void PreLoadAssets()
    {
        _camera=Camera.main;
        _cameraPosition=_camera.transform;
        _terrainPosition = FindObjectOfType<Terrain>().transform;
        _grassMask = FindObjectOfType<TerrainCollider>().terrainData;
        _grassComputeShader=Resources.Load<ComputeShader>("GrassCoreCompute");
        _grassMeshLod0=Resources.Load<GameObject>("GrassBladePacked").GetComponent<MeshFilter>().sharedMesh;
        _grassMaterial=Resources.Load<Material>("GPUGrassMat");
        _clumpMaterial=Resources.Load<Material>("VoronoiMat");
    }
    
    private void GetDataFromConfig()
    {
        GrassResolution = GrassConfigs.Density;
        RotationRandom = GrassConfigs.RotationRandom;
        JitterStrength = GrassConfigs.Jitter;
        _clumpArgs = GrassConfigs.ClumpArgs;
        GlobalWindMap = GrassConfigs.GlobalWindMap;
        _cullDistancePower = (float)GrassConfigs.Preference;
    }

    private void SetGrassDistributeMask()
    {
        var grassDistributeMask=_grassMask.GetAlphamapTexture((int)GrassConfigs.GrassLayer);
        _grassComputeShader.SetTexture(0,GrassMapId,grassDistributeMask);
        _grassComputeShader.SetVector(GrassMaskChannelId,GrassConfigs.grassChannel.RGBAToVector4());
    }

    private void SetMeshData()
    {
        var triangles = _grassMeshLod0.triangles;
        _trianglesBuffer=new ComputeBuffer(triangles.Length,sizeof(int));
        _trianglesBuffer.SetData(triangles);
        var colors = _grassMeshLod0.colors;
        Debug.Log(colors.Length);
        _colorsBuffer=new ComputeBuffer(colors.Length,4*sizeof(float));
        _colorsBuffer.SetData(colors);
        var uvs=_grassMeshLod0.uv;
        _uvsBuffer=new ComputeBuffer(uvs.Length,2*sizeof(float));
        _uvsBuffer.SetData(uvs);
        
        _grassMaterial.SetBuffer(TrianglesBufferId,_trianglesBuffer);
        _grassMaterial.SetBuffer(ColorsBufferId,_colorsBuffer);
        _grassMaterial.SetBuffer(UvsBufferId,_uvsBuffer);
        
        _argsBuffer=new ComputeBuffer(1,4*sizeof(int),ComputeBufferType.IndirectArguments);
        _argsBuffer.SetData(new int[]{triangles.Length,0,0,0});
    }
    
    private void Awake()
    {
        _grassPropertiesBuffer=new ComputeBuffer(GrassResolution*GrassResolution,15*sizeof(float),ComputeBufferType.Append);
        _grassPropertiesBuffer.SetCounterValue(0);
    }

    private void Start()
    {
        PreLoadAssets();
        GetDataFromConfig();
        EnableKeyWord();
        SetGrassDistributeMask();
        
        _grassComputeShader.SetTexture(0,GlobalWindMapId,GlobalWindMap);
        
        var heightMap = _grassMask.heightmapTexture;
        _grassHeightMap=new Texture2D(heightMap.width,heightMap.height,TextureFormat.RFloat,false,true);
        _grassHeightMap.filterMode=FilterMode.Bilinear;
        RenderTexture.active=heightMap;
        _grassHeightMap.ReadPixels(new Rect(0,0,heightMap.width,heightMap.height),0,0);
        _grassHeightMap.Apply();
        RenderTexture.active=null;
        _grassComputeShader.SetTexture(0,HeightMapId,_grassHeightMap);
        _grassComputeShader.SetVector(TerrainSizeId,
            new Vector4(_grassMask.size.x,
                _grassMask.size.y,
                _grassMask.size.z,
                _grassMask.heightmapResolution));
        
        var terrainPos=_terrainPosition.position;
        _grassComputeShader.SetVector(TerrainOriginId,
            new Vector4(terrainPos.x,terrainPos.y,terrainPos.z, _grassMask.alphamapResolution));

        SetMeshData();
        _grassComputeShader.SetBuffer(0,GrassPropertiesBufferId,_grassPropertiesBuffer);
        _grassInstanceBounds=new Bounds(new Vector3(500,200,500),new Vector3(1000,40,1000));
        
        GenerateVoronoiClumpMap();
        SetClumpArgs();
    }

    private void Update()
    {
        MotionMask.SetToGlobal(_grassComputeShader);
        UpdateComputeArgs();
        
        Graphics.DrawProceduralIndirect(_grassMaterial,_grassInstanceBounds,MeshTopology.Triangles,_argsBuffer,
            0,null,null,ShadowCastingMode.Off,true,gameObject.layer);
    }

    private void GenerateVoronoiClumpMap()
    {
        _clumpMaterial.SetFloat(ClumpNumId,_clumpArgs.Count());
        Texture2D voronoi = new Texture2D(512, 512, TextureFormat.RGBAFloat, false, true);
        RenderTexture voronoiRT = RenderTexture.GetTemporary(512, 512, 0, RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
        Graphics.Blit(voronoi,voronoiRT,_clumpMaterial);
        RenderTexture.active = voronoiRT;
        voronoi.filterMode = FilterMode.Point;
        voronoi.ReadPixels(new Rect(0,0,512,512),0,0,true);
        voronoi.Apply();
        RenderTexture.active = null;
        _grassComputeShader.SetTexture(0,ClumpTexId,voronoi);
        RenderTexture.ReleaseTemporary(voronoiRT);
    }
    
    private void UpdateComputeArgs()
    {
        _grassComputeShader.SetVector(CameraPositionId,_cameraPosition.position);
        _grassComputeShader.SetVector(CharacterPositionId,CharacterPosition.position);
        _grassComputeShader.SetFloat(RotationRandomId,RotationRandom);
        _grassComputeShader.SetFloat(GlobalWindSpeedId,GlobalWindSpeed);
        _grassComputeShader.SetVector(TimeCsID,Shader.GetGlobalVector(TimeID)); 
        _grassComputeShader.SetFloat(GlobalWindStrengthId,GlobalWindStrength);
        _grassComputeShader.SetFloat(JitterStrengthId,JitterStrength);
        _grassComputeShader.SetFloat(PerGrassTileSizeId,_grassMask.size.x/GrassResolution);
        _grassPropertiesBuffer.SetCounterValue(0);
        _grassComputeShader.SetInt(GrassResolutionId,GrassResolution);
        _grassComputeShader.SetFloat(CullDistancePowerId,_cullDistancePower);
        
        Matrix4x4 projMat = GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false);
        Matrix4x4 VPMatrix = projMat * _camera.worldToCameraMatrix;
        _grassComputeShader.SetMatrix(WorldToHClipMatrixId,VPMatrix);
        
        var groupSize = Mathf.CeilToInt(GrassResolution / 8f);
        _grassComputeShader.Dispatch(0,groupSize,groupSize,1);
        ComputeBuffer.CopyCount(_grassPropertiesBuffer,_argsBuffer,sizeof(int));
        
        _grassMaterial.SetBuffer(GrassPropertiesBufferId,_grassPropertiesBuffer);
    }

    private void SetClumpArgs()
    {
        var count=_clumpArgs.Count;
        _grassClumpArgsBuffer = new ComputeBuffer(count, 10 * sizeof(float));
        _grassClumpArgsBuffer.SetData(_clumpArgs);
        _grassComputeShader.SetBuffer(0,ClumpArgsId,_grassClumpArgsBuffer);
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
}



