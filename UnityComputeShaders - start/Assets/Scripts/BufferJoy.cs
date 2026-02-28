using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class BufferJoy : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)] struct CircleData
    {
        public Vector2 origin;
        public Vector2 velocity;
        public float radius;
    }
    
    private CircleData[] _circleData;
    private ComputeBuffer _circleBuffer;
    
    public ComputeShader shader;
    public int texResolution = 1024;
    public float speed = 1.0f;

    Renderer rend;
    RenderTexture outputTexture;

    int circlesHandle;
    int clearHandle;

    public Color clearColor = new Color();
    public Color circleColor = new Color();

    int count = 10;

    // Use this for initialization
    void Start()
    {
        outputTexture = new RenderTexture(texResolution, texResolution, 0);
        outputTexture.enableRandomWrite = true;
        outputTexture.Create();

        rend = GetComponent<Renderer>();
        rend.enabled = true;

        InitData();

        InitShader();
    }

    private void InitData()
    {
        circlesHandle = shader.FindKernel("Circles");

        shader.GetKernelThreadGroupSizes(circlesHandle, out var threadGroupSizeX, out _, out _);
        
        var actualSpeed = speed;
        var halfSpeed = actualSpeed * 0.5f;
        var minRadius = 10;
        var maxRadius = 30;
        var radiusRange = maxRadius - minRadius;
        var total = (int)threadGroupSizeX * count;
        
        _circleData = new CircleData[total];

        for (int i = 0; i < total; i++)
        {
            var circleData = _circleData[i];
            circleData.origin.x = Random.value * texResolution;
            circleData.origin.y = Random.value * texResolution;
            circleData.velocity.x = (Random.value * actualSpeed) - halfSpeed;
            circleData.velocity.y = (Random.value * actualSpeed) - halfSpeed;
            circleData.radius = Random.value * radiusRange + minRadius;
            _circleData[i] = circleData;
        }
    }

    private void InitShader()
    {
    	clearHandle = shader.FindKernel("Clear");
    	
        shader.SetVector( "clearColor", clearColor );
        shader.SetVector( "circleColor", circleColor );
        shader.SetInt( "texResolution", texResolution );
		
		shader.SetTexture( clearHandle, "Result", outputTexture );
        shader.SetTexture( circlesHandle, "Result", outputTexture );

        int stride = Marshal.SizeOf<CircleData>();
        _circleBuffer = new ComputeBuffer(_circleData.Length, stride);
        _circleBuffer.SetData(_circleData);
        shader.SetBuffer( circlesHandle, "CirclesBuffer", _circleBuffer);
        
        rend.material.SetTexture("_MainTex", outputTexture);
    }
 
    private void DispatchKernels(int count)
    {
    	shader.Dispatch(clearHandle, texResolution/8, texResolution/8, 1);
        shader.SetFloat("time", Time.time);
        shader.Dispatch(circlesHandle, count, 1, 1);
    }

    void Update()
    {
        DispatchKernels(count);
    }
}

