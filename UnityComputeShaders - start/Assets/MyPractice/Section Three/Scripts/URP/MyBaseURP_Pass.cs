using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public abstract class MyBaseURP_Pass : ScriptableRenderPass, System.IDisposable
{
    protected ComputeShader _computeShader;
    protected int _resolutionDivisor;
    
    protected Vector2Int _textureSize;
    protected Vector2Int _threadGroupSize;

    protected RTHandle _renderedSource;
    protected RTHandle _renderedOutput;
    
    protected bool _init =  false;

    public MyBaseURP_Pass(ComputeShader computeShader, int resolutionDivisor)
    {
        _computeShader = computeShader;
        _resolutionDivisor = resolutionDivisor;
    }

    protected Vector2Int GetThreadGroupSize(int kernelHandle, Vector2Int size)
    {
        _computeShader.GetKernelThreadGroupSizes(kernelHandle, out var x, out var y, out _);
        
        return new Vector2Int(
            Mathf.CeilToInt((float)size.x / x),
            Mathf.CeilToInt((float)size.y / y));
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (!_init)
        {
            return;
        }
        
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        
        _textureSize = new Vector2Int(
            cameraData.cameraTargetDescriptor.width / _resolutionDivisor,
            cameraData.cameraTargetDescriptor.height / _resolutionDivisor);

        Render(renderGraph, resourceData, cameraData);
    }
    
    protected abstract void Render(RenderGraph renderGraph, UniversalResourceData resourceData, UniversalCameraData cameraData);

    public virtual void Dispose()
    {
        _renderedSource?.Release();
        _renderedOutput?.Release();
    }
}
