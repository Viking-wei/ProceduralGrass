using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ComputeShaderTest : MonoBehaviour
{
    public ComputeShader BoidsComputeShader;
    public int BoidsCount;
    public GameObject BoidsPrefab;
    public float SpawnRadius;
    public float BoidsSpeed = 1f;
    public float NeighbourDistance = 1f;
    public Transform Target;
    
    private ComputeBuffer _boidsBuffer;
    
    private int _kernelIndex;
    private GameObject[] _boids;
    private Boids[] _boidsProperties;
    private int _groupSize;
    private int _boidsCountFull;

    #region Properties ID
    private static readonly int s_boidsBufferId=Shader.PropertyToID("BoidsBuffer");
    private static readonly int s_boidsCountId=Shader.PropertyToID("boidsCount");
    private static readonly int s_flockPositionId=Shader.PropertyToID("flockPosition");
    private static readonly int s_deltaTimeId=Shader.PropertyToID("deltaTime");
    private static readonly int s_neighbourDistanceId=Shader.PropertyToID("neighbourDistance");
    private static readonly int s_boidsSpeedId=Shader.PropertyToID("boidsSpeed");
    #endregion
    
    void Start()
    {
        _kernelIndex=BoidsComputeShader.FindKernel("CSMain");
        BoidsComputeShader.GetKernelThreadGroupSizes(_kernelIndex,out uint x,out _,out _);
        _groupSize = Mathf.CeilToInt(BoidsCount / (float) x);
        _boidsCountFull = _groupSize * (int) x;

        InitialBoids();
        InitialComputeShader();
    }

    private void InitialBoids()
    {
        _boids=new GameObject[_boidsCountFull];
        _boidsProperties = new Boids[_boidsCountFull];

        for (var i = 0; i < _boidsCountFull; ++i)
        {
            Vector3 pos=transform.position+Random.insideUnitSphere*SpawnRadius;
            _boidsProperties[i] = new Boids(pos);
            _boids[i] = Instantiate(BoidsPrefab, pos, Quaternion.identity);
            _boidsProperties[i].Direction=_boids[i].transform.forward;
        }
    }

    private void InitialComputeShader()
    {
        _boidsBuffer = new ComputeBuffer(_boids.Length, 6 * sizeof(float));
        _boidsBuffer.SetData(_boidsProperties);
        BoidsComputeShader.SetBuffer(_kernelIndex, s_boidsBufferId, _boidsBuffer);
        BoidsComputeShader.SetInt(s_boidsCountId, _boidsCountFull);
        BoidsComputeShader.SetVector(s_flockPositionId, Target.position);
        BoidsComputeShader.SetFloat(s_boidsSpeedId, BoidsSpeed);
        BoidsComputeShader.SetFloat(s_neighbourDistanceId, NeighbourDistance);
    }

    private void Update()
    {
        BoidsComputeShader.SetFloat(s_deltaTimeId, Time.deltaTime);
        BoidsComputeShader.SetVector(s_flockPositionId, Target.position);
        BoidsComputeShader.Dispatch(_kernelIndex, _groupSize, 1, 1);
        
        _boidsBuffer.GetData(_boidsProperties);
        
        for (var i = 0; i < _boidsCountFull; ++i)
        {
            _boids[i].transform.localPosition = _boidsProperties[i].Position;
            
            if(!_boidsProperties[i].Direction.Equals(Vector3.zero))
                _boids[i].transform.rotation = Quaternion.LookRotation(_boidsProperties[i].Direction);
        }
    }

    private void OnDestroy()
    {
        _boidsBuffer.Release();
    }
}

struct Boids
{
    public Vector3 Position;
    public Vector3 Direction;

    public Boids(Vector3 pos)
    {
        Position = pos;
        Direction = Vector3.zero;
    }
}
