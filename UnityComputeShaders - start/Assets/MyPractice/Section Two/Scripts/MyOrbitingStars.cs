using UnityEngine;

public class MyOrbitingStars : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private int starCount = 10;
    [SerializeField] private float orbitRadius = 1.0f;

    private ComputeBuffer _resultBuffer;
    private Transform[] _stars;
    private Vector3[] _starPositions;
    private int _kernelHandle;
    private int _groupSizeX;
    private uint _threadGroupSizeX;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        InitShader();
    }

    private void InitShader()
    {
        _kernelHandle = computeShader.FindKernel("OrbitingStars");
        computeShader.GetKernelThreadGroupSizes(_kernelHandle, out _threadGroupSizeX, out _, out _);
        
        computeShader.SetFloat("_orbitRadius", orbitRadius);
        _groupSizeX = (int)((starCount + _threadGroupSizeX - 1) / _threadGroupSizeX);
        _resultBuffer = new ComputeBuffer(starCount, sizeof(float) * 3);
        
        computeShader.SetBuffer(_kernelHandle, "result_buffer", _resultBuffer);
        
        _starPositions = new Vector3[starCount];
        _stars = new Transform[starCount];

        for (var i = 0; i < starCount; i++)
        {
            _stars[i] = Instantiate(starPrefab, transform).transform;
        }
    }
    
    // Update is called once per frame
    private void Update()
    {
        DispatchShader();
    }

    private void DispatchShader()
    {
        computeShader.SetFloat("_time", Time.time);
        computeShader.Dispatch(_kernelHandle, _groupSizeX, 1, 1);
        _resultBuffer.GetData(_starPositions);

        for (var i = 0; i < starCount; i++)
        {
            _stars[i].position = _starPositions[i];
        }
    }
    
    private void OnDisable()
    {
        _resultBuffer.Dispose();
        _resultBuffer = null;
    }
}
