using UnityEngine;

public class MyPassData : MonoBehaviour
{
    [SerializeField] ComputeShader computeShader;
    [SerializeField] int texResolution = 1024;
    [SerializeField] Color clearColor, circleColor;
    
    Renderer _rend;
    RenderTexture _outputTexture;
    
    int clearHandle;
    int circleHandle;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _outputTexture = new RenderTexture(texResolution, texResolution, 0);
        _outputTexture.enableRandomWrite = true;
        _outputTexture.Create();
        
        _rend = GetComponent<Renderer>();
        _rend.enabled = true;
        
        InitShader();
    }

    void InitShader()
    {
        clearHandle = computeShader.FindKernel("Clear");
        circleHandle = computeShader.FindKernel("Circles");
        
        computeShader.SetInt("_texResolution", texResolution);
        computeShader.SetVector("_clearColor", clearColor);
        computeShader.SetVector("_circleColor", circleColor);
        
        computeShader.SetTexture(clearHandle, "Result", _outputTexture);
        computeShader.SetTexture(circleHandle, "Result", _outputTexture);
        _rend.material.SetTexture("_MainTex", _outputTexture);
    }

    void DispatchKernels(int count)
    {
        computeShader.SetFloat("_time",  Time.time);
        computeShader.Dispatch(clearHandle, texResolution/8, texResolution/8, 1);
        computeShader.Dispatch(circleHandle, count, 1, 1);
    }
    
    // Update is called once per frame
    void Update()
    {
        DispatchKernels(10);
    }
}
