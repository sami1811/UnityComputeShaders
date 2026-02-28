using UnityEngine;
using System.Runtime.InteropServices;

public class MyBufferJoy : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)] struct CircleData
    {
        public Vector2 origin;
        public Vector2 velocity;
        public float radius;
    }
    
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private int texResolution = 1024;
    [SerializeField] private Color clearColor, circleColor;
    [SerializeField] private int kernelCount = 10;
    [SerializeField] private float speed = 1.0f;
    [SerializeField,Tooltip("For Bresenham Lines Only")] private Vector2 startPoint;
    [SerializeField,Tooltip("For Bresenham Lines Only")] private Vector2 endPoint;
    
    private Renderer _rend;
    private RenderTexture _outputTexture;
    
    private int _clearHandle;
    private int _circleHandle;
    private int _bresenhamHandle;
    
    CircleData[] _circleData;
    ComputeBuffer _circlesBuffer;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _outputTexture = new RenderTexture(texResolution, texResolution, 0);
        _outputTexture.enableRandomWrite = true;
        _outputTexture.Create();
        
        _rend = GetComponent<Renderer>();
        _rend.enabled = true;

        InitData();
        InitShader();
    }

    private void InitData()
    {
        _circleHandle = computeShader.FindKernel("Circles");
        
        computeShader.GetKernelThreadGroupSizes(_circleHandle, out var threadGroupSizeX, out _, out _);

        float halfSpeed = speed * 0.5f;
        float radiusMin = 10.0f;
        float radiusMax = 30.0f;
        float radiusRange = radiusMax - radiusMin;
        int total = (int)threadGroupSizeX * kernelCount;
        
        _circleData = new CircleData[total];

        for (int i = 0; i < total; i++)
        {
            CircleData circleData = _circleData[i];
            
            circleData.origin.x = Random.value * texResolution;
            circleData.origin.y = Random.value * texResolution;
            circleData.velocity.x = (Random.value * speed) - halfSpeed;
            circleData.velocity.y = (Random.value * speed) - halfSpeed;
            circleData.radius = (Random.value * radiusRange) +  radiusMin;
            
            _circleData[i] = circleData;
        }
    }
    
    void InitShader()
    {
        _clearHandle = computeShader.FindKernel("Clear");
        _bresenhamHandle = computeShader.FindKernel("Bresenham");
        
        computeShader.SetInt("_texResolution", texResolution);
        computeShader.SetVector("_clearColor", clearColor);
        computeShader.SetVector("_circleColor", circleColor);
        computeShader.SetVector("_startPoint", startPoint);
        computeShader.SetVector("_endPoint", endPoint);
        
        computeShader.SetTexture(_clearHandle, "Result", _outputTexture);
        computeShader.SetTexture(_circleHandle, "Result", _outputTexture);
        computeShader.SetTexture(_bresenhamHandle, "Result", _outputTexture);
        
        int stride = Marshal.SizeOf<CircleData>();
        _circlesBuffer = new ComputeBuffer(_circleData.Length, stride);
        _circlesBuffer.SetData(_circleData);
        computeShader.SetBuffer(_circleHandle, "_circlesBuffer", _circlesBuffer);
        
        _rend.material.SetTexture("_MainTex", _outputTexture);
    }

    void DispatchKernels(int count)
    {
        computeShader.SetFloat("_time",  Time.time);
        computeShader.Dispatch(_clearHandle, texResolution/8, texResolution/8, 1);
        computeShader.Dispatch(_circleHandle, count, 1, 1);
        //computeShader.Dispatch(bresenhamHandle, texResolution, texResolution, 1);
    }
    
    // Update is called once per frame
    void Update()
    {
        DispatchKernels(kernelCount);
    }
}
