using UnityEngine;
using Unity.Profiling;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class MyBasePP : MonoBehaviour
{
    public bool enableEffect = true;
    public ComputeShader computeShader = null;
    
    [Range(1, 4)]
    public int resolutionDivisor = 1;
    
    protected string _kernelName = "";
    protected int _kernelHandle = -1;
    
    protected Vector2Int _textureSize = new Vector2Int(0, 0);
    protected Vector2Int _threadGroupSize = new Vector2Int(0, 0);
    
    protected Camera _thisCamera = null;

    protected RenderTexture _renderedSource = null;
    protected RenderTexture _renderedOutput = null;
    protected RenderTextureFormat _textureFormat = RenderTextureFormat.Default;
    
    protected bool _init = false;
    
    protected static readonly ProfilerMarker _dispatchMarker = new ProfilerMarker("MyBasePP.DispatchCompute");

    protected virtual void OnEnable()
    {
        Init();
    }
    
    protected virtual void OnValidate()
    {
        if (!_init)
        {
            Init();
        }

        if (_init)
        {
            SetProperties();
        }
    }

    protected virtual void OnDisable()
    {
        ClearTextures();
        _init = false;
    }

    protected virtual void OnDestroy()
    {
        ClearTextures();
        _init = false;
    }

    protected virtual void Init()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError("Compute Shaders not supported on this platform!");
            return;
        }

        if (!SystemInfo.SupportsRenderTextureFormat(_textureFormat))
        {
            Debug.LogError("RenderTexture format not supported on this platform!");
            return;
        }

        if (!computeShader)
        {
            Debug.LogError("Compute Shader is not assigned!");
            return;
        }
        
        if (!computeShader.HasKernel(_kernelName))
        {
            Debug.LogError($"Kernel {_kernelName} not found in {computeShader}!");
            return;
        }
        
        _thisCamera = GetComponent<Camera>();

        if (!_thisCamera)
        {
            Debug.LogError("No Camera found!");
            return;
        }
        
        _kernelHandle = computeShader.FindKernel(_kernelName);
        
        CreateTextures();
        
        _init = true;
    }

    protected void CreateTexture(ref RenderTexture textureToCreate, int divide = 1)
    {
        textureToCreate = new RenderTexture(_textureSize.x / divide, _textureSize.y / divide, 0, _textureFormat);
        textureToCreate.enableRandomWrite = true;
        textureToCreate.Create();
    }

    protected Vector2Int GetThreadGroupSize(int kernelHandle, Vector2Int size)
    {
        computeShader.GetKernelThreadGroupSizes(kernelHandle, out var x, out var y, out _);

        return new Vector2Int(
            Mathf.CeilToInt((float)size.x / x), 
            Mathf.CeilToInt((float)size.y / y));
    }
    
    protected virtual void CreateTextures()
    {
        ClearTextures();
        
        _textureSize.x = _thisCamera.pixelWidth;
        _textureSize.y = _thisCamera.pixelHeight;

        Vector2Int actualSize = new Vector2Int(_textureSize.x / resolutionDivisor, _textureSize.y / resolutionDivisor);
        
        _threadGroupSize = GetThreadGroupSize(_kernelHandle, actualSize);
        
        CreateTexture(ref _renderedSource, resolutionDivisor);
        CreateTexture(ref _renderedOutput, resolutionDivisor);
        
        computeShader.SetTexture(_kernelHandle, "rendered_source", _renderedSource);
        computeShader.SetTexture(_kernelHandle, "rendered_output", _renderedOutput);
    }
    
    protected virtual void SetProperties() { }
    
    protected virtual void ClearTexture(ref RenderTexture textureToClear)
    {
        if (textureToClear)
        {
            textureToClear.Release();
            textureToClear = null;
        }
            
    }
    
    protected virtual void ClearTextures()
    {
        ClearTexture(ref _renderedSource);
        ClearTexture(ref _renderedOutput);
    }

    protected void CheckResolution(out bool resolutionChanged)
    {
        resolutionChanged = false;

        if (_textureSize.x != _thisCamera.pixelWidth || _textureSize.y != _thisCamera.pixelHeight)
        {
            resolutionChanged = true;
            CreateTextures();
        }
    }

    protected virtual void DispatchCompute(ref RenderTexture source, ref RenderTexture destination)
    {
        if (!_init)
        {
            return;
        }

        _dispatchMarker.Begin();
        Graphics.Blit(source, _renderedSource);
        
        computeShader.Dispatch(_kernelHandle, _threadGroupSize.x, _threadGroupSize.y, 1);
        
        Graphics.Blit(_renderedOutput, destination);
        _dispatchMarker.End();
    }
    
    protected bool IsSupported => _init && computeShader && _thisCamera && enableEffect;

    protected virtual void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!IsSupported)
        {
            Graphics.Blit(source, destination);
        }
        else
        {
            CheckResolution(out _);
            DispatchCompute(ref source, ref destination);
        }
    }
}
