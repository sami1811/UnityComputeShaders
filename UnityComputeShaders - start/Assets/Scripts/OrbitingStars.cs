using UnityEngine;

public class OrbitingStars : MonoBehaviour
{
    public int starCount = 17;
    public ComputeShader shader;

    public GameObject prefab;

    int kernelHandle;
    uint threadGroupSizeX;
    int groupSizeX;
    
    Transform[] stars;
    private ComputeBuffer _resultBuffer;
    private Vector3[] _output;
    
    void Start()
    {
        kernelHandle = shader.FindKernel("OrbitingStars");
        shader.GetKernelThreadGroupSizes(kernelHandle, out threadGroupSizeX, out _, out _);
        groupSizeX = (int)((starCount + threadGroupSizeX - 1) / threadGroupSizeX);
        
        _resultBuffer = new ComputeBuffer(starCount, sizeof(float) * 3);
        shader.SetBuffer(kernelHandle, "result_buffer", _resultBuffer);
        _output = new Vector3[starCount];
        
        stars = new Transform[starCount];
        for (int i = 0; i < starCount; i++)
        {
            stars[i] = Instantiate(prefab, transform).transform;
        }
    }
    
    void Update()
    {
        shader.SetFloat("time", Time.time);
        shader.Dispatch(kernelHandle, groupSizeX, 1, 1);
        _resultBuffer.GetData(_output);
        
        for (int i = 0; i < starCount; i++)
        {
            stars[i].localPosition = _output[i];
        }
    }

    private void OnDestroy()
    {
        _resultBuffer.Dispose();
        _resultBuffer = null;
    }
}
