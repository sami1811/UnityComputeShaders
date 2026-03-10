using UnityEngine;
using UnityEngine.Rendering.Universal;

public abstract class MyBaseURP_Feature : ScriptableRendererFeature
{
    public bool enableEffect = true;
    public ComputeShader computeShader = null;
    
    [Range(1, 4)]
    public int resolutionDivisor = 1;
    
    protected MyBaseURP_Pass _pass;

    public override void Create()
    {
        if (!computeShader)
        {
            return;
        }

        _pass = CreatePass();
        _pass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }
    
    protected abstract MyBaseURP_Pass CreatePass();

    protected bool IsSupported => enableEffect && computeShader && _pass != null;
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!IsSupported)
        {
            return;
        }
        
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        _pass?.Dispose();
    }
}
