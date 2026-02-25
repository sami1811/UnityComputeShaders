using UnityEngine;


public class SolidColorCompute : MonoBehaviour
{
    [SerializeField] ComputeShader computeShader;
    [SerializeField] string kernelName = "SolidRed";
    [SerializeField] int texResolution = 256;
    [SerializeField,Tooltip("Circle radius in Percentage, Only works with Circle kernels")] int circleRadius = 50;
    [SerializeField,Tooltip("Square area in Percentage, Only works with Square kernel")] int rectArea = 50;
    
    Renderer _rend;
    RenderTexture _outputRenderTexture;
    int _kernelHandle;
    
    // Start is calleds once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _outputRenderTexture = new RenderTexture(texResolution, texResolution, 0);
        _outputRenderTexture.enableRandomWrite = true;
        _outputRenderTexture.Create();
        
        _rend = GetComponent<Renderer>();
        _rend.enabled = true;
        
        InitShader();
    }

    private void InitShader()
    {
        _kernelHandle = computeShader.FindKernel(kernelName);
        computeShader.SetInt("_texResolution", texResolution);
        computeShader.SetInt("_radius", circleRadius);
        computeShader.SetInt("_area", rectArea);
        computeShader.SetTexture(_kernelHandle, "Result", _outputRenderTexture);
        _rend.material.SetTexture("_MainTex", _outputRenderTexture);
        
        DispatchShader(texResolution, texResolution);
    }

    private void DispatchShader(int x, int y)
    {
        computeShader.Dispatch(_kernelHandle, x, y, 1);
    }
    
    // Update is called once per frame
    void Update()
    {
        /*if (Input.GetKeyUp(KeyCode.U))
        {
            DispatchShader(texResolution/8, texResolution/8);
        }*/
    }
}
