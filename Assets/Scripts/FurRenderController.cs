using System.Security.Claims;
using UnityEngine;

public class FurRenderController : MonoBehaviour
{
    [Header("Basic Settings")]
    public GameObject TargetModel;
    public Shader ShellShader;
    public int LayerCount = 10;

    [Header("Fur Settings")]
    public Texture FurNoise;
    public Color FurRootColor;
    public Color FurSurfaceColor;
    public float FurLength = 0.5f;
    public Vector4 FurForce;
    [Range(0.5f,5f)] 
    public float FurTenacity = 1;
    
    [Header("Lighting Settings")]
    public Color RimColor = Color.white;
    public float RimPower = 5;
    
    // public Vector3 WindDirection = Vector3.right;
    // public float WindSpeed = 1;
    
    
    private GameObject[] _layers;
    
    public static readonly int FurNoiseTexId=Shader.PropertyToID("_FurNoiseTex");
    public static readonly int FurRootColorId=Shader.PropertyToID("_FurRootColor");
    public static readonly int FurSurfaceColorId=Shader.PropertyToID("_FurSurfaceColor");
    public static readonly int FurLengthId=Shader.PropertyToID("_FurLength");
    public static readonly int FurOffsetId=Shader.PropertyToID("_FurOffset");
    public static readonly int LayerOffsetId=Shader.PropertyToID("_LayerOffset");
    public static readonly int RimPowerId=Shader.PropertyToID("_RimPower");
    public static readonly int RimColorId=Shader.PropertyToID("_RimColor");

    void CreatShell()
    {
        _layers= new GameObject[LayerCount];
        float layerOffset = 1.0f/ LayerCount;

        for (int i = 0; i < LayerCount; ++i)
        {
            GameObject layer = Instantiate(TargetModel,TargetModel.transform.position, TargetModel.transform.rotation);
            //layer.hideFlags= HideFlags.HideInHierarchy;
            layer.name= "FurLayer" + i;
            layer.tag = "FurShell";
            
            var furRender = layer.GetComponent<Renderer>();
            furRender.sharedMaterial = new Material(ShellShader);
            furRender.sharedMaterial.SetTexture(FurNoiseTexId, FurNoise);
            furRender.sharedMaterial.SetColor(FurRootColorId, FurRootColor);
            furRender.sharedMaterial.SetColor(FurSurfaceColorId, FurSurfaceColor);
            furRender.sharedMaterial.SetFloat(FurLengthId, FurLength);
            furRender.sharedMaterial.SetFloat(LayerOffsetId, i * layerOffset);
            furRender.sharedMaterial.SetVector(FurOffsetId, FurForce* Mathf.Pow(i * layerOffset, FurTenacity));
            furRender.sharedMaterial.SetFloat(RimPowerId, RimPower);
            furRender.sharedMaterial.SetColor(RimColorId, RimColor);
            furRender.sharedMaterial.renderQueue = 3000 + i;

            _layers[i] = layer;
        }
    }

    public void ClearFurShell()
    {
        GameObject[] shells = GameObject.FindGameObjectsWithTag("FurShell");
        Debug.Log(shells.Length);
        foreach (var shell in shells)
            DestroyImmediate(shell);
        _layers = null;
    }

    public void UpdateShellData()
    {
        if (_layers == null||_layers.Length == 0 )
        {
            CreatShell();
            return;
        }
        
        float layerOffset = 1.0f/ LayerCount;
        for (int i = 0; i < LayerCount; ++i)
        {
            var furRender = _layers[i].GetComponent<Renderer>();
            furRender.sharedMaterial.SetTexture(FurNoiseTexId, FurNoise);
            furRender.sharedMaterial.SetColor(FurRootColorId, FurRootColor);
            furRender.sharedMaterial.SetColor(FurSurfaceColorId, FurSurfaceColor);
            furRender.sharedMaterial.SetFloat(FurLengthId, FurLength);
            furRender.sharedMaterial.SetFloat(LayerOffsetId, i * layerOffset);
            furRender.sharedMaterial.SetVector(FurOffsetId, FurForce* Mathf.Pow(i * layerOffset, FurTenacity));
            furRender.sharedMaterial.SetFloat(RimPowerId, RimPower);
            furRender.sharedMaterial.SetColor(RimColorId, RimColor);
        }
    }

    void FurFollowing()
    {
        int layerCount = _layers.Length;
        float divide = 1.0f / layerCount;
        // Vector3 normalizedWindDir= WindDirection.normalized;
        // Vector3 verticalDir = Vector3.Cross(normalizedWindDir, Vector3.up);
        // Vector3 horizontalDir = Vector3.Cross(normalizedWindDir, verticalDir);
        // Vector3 cicleDir = verticalDir* Mathf.Sin(Time.time) + horizontalDir* Mathf.Cos(Time.time);
        // Vector3 furSwingDir = normalizedWindDir + cicleDir.normalized* WindSpeed;
        
        if (layerCount== 0)
            return;

        for(int i=0;i<layerCount;++i)
        {
            float lerpSpeed = (layerCount-i) * divide * FurTenacity;
            
            _layers[i].transform.position = Vector3.Lerp(_layers[i].transform.position, TargetModel.transform.position, lerpSpeed);
            _layers[i].transform.rotation = Quaternion.Lerp(_layers[i].transform.rotation, TargetModel.transform.rotation, lerpSpeed);
        }
    }
    
    void Start()
    {
        UpdateShellData();
    }
    
    void Update()
    {
        FurFollowing();
    }
}
