using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "GrassConfig", menuName = "NewGrassData")]
public class GrassConfig : ScriptableObject
{
    [Header("Essential Arguments")] 
    public Texture2D GlobalWindMap;
    public TerrainData GrassMask;
    public GameObject GrassPrefab;
    public GrassMaskLayer GrassLayer=GrassMaskLayer.First;
    public GrassMaskChannel grassChannel=GrassMaskChannel.R;
    
    [Header("Preferences")] 
    public PreferenceState  Preference= PreferenceState.High;
    
    [Header("Grass Disorder")] 
    public Color GrassColor = Color.blue;
    public int Density = 4096;
    public float Jitter = 0;
    [Range(0, 1)] public float RotationRandom;
    
    [Header("Clump")]
    public float ClumpDensity =9f;
    public bool EnableClump = true;
    public List<ClumpArgsStruct> ClumpArgs;
    
    
    public enum PreferenceState
    {
        Low=3000,
        Balanced=5000,
        High=10000
    }
    
    
    public enum GrassMaskLayer
    {
        First=0,
        Second=1
    }
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

public enum GrassMaskChannel
{
    R,
    G,
    B,
    A
}

public static class EnumExtension
{
    public static Vector4 RGBAToVector4(this GrassMaskChannel channel)
    {
        switch (channel)
        {
            case GrassMaskChannel.R:
                return new Vector4(1, 0, 0, 0);
            case GrassMaskChannel.G:
                return new Vector4(0, 1, 0, 0);
            case GrassMaskChannel.B:
                return new Vector4(0, 0, 1, 0);
            case GrassMaskChannel.A:
                return new Vector4(0, 0, 0, 1);
            default:
                throw new System.ArgumentOutOfRangeException(nameof(channel), channel, null);
        }
    }
}