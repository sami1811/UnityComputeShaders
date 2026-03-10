using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class DualKawaseBlur_Pass : MyBaseURP_Pass
{
    private class PassData
    {
        public ComputeShader computeShader;
        public DualKawaseBlur_Feature feature;

        public RTHandle renderedSource;
        public RTHandle renderedOutput;
        public RTHandle tempBuffer1;
        public RTHandle tempBuffer2;

        public TextureHandle cameraColor;

        public int highlightKernel;
        public int kawaseDownKernel;
        public int kawaseUpKernel;

        public Vector2Int threadGroupSize;

        public Camera camera;
    }
    
    private DualKawaseBlur_Feature _feature;
    
    private int _highlightKernel  = -1;
    private int _kawaseDownKernel = -1;
    private int _kawaseUpKernel   = -1;

    private RTHandle _tempBuffer1;
    private RTHandle _tempBuffer2;
    
    public DualKawaseBlur_Pass(ComputeShader computeShader, int resolutionDivisor, DualKawaseBlur_Feature feature)
        : base(computeShader, resolutionDivisor)
    {
        _feature = feature;
        
        _highlightKernel = computeShader.FindKernel("HighLight");
        _kawaseDownKernel = computeShader.FindKernel("KawaseDown");
        _kawaseUpKernel = computeShader.FindKernel("KawaseUp");

        _init = true;
    }
    
    protected override void Render(RenderGraph renderGraph, UniversalResourceData resourceData, UniversalCameraData cameraData)
    {
        // allocate textures
        var desc = cameraData.cameraTargetDescriptor;
        desc.enableRandomWrite = true;
        desc.depthBufferBits   = 0;
        desc.width             = _textureSize.x;
        desc.height            = _textureSize.y;

        RenderingUtils.ReAllocateHandleIfNeeded(ref _renderedSource, desc, name: "_RenderedSource");
        RenderingUtils.ReAllocateHandleIfNeeded(ref _renderedOutput, desc, name: "_RenderedOutput");
        RenderingUtils.ReAllocateHandleIfNeeded(ref _tempBuffer1,    desc, name: "_TempBuffer1");
        RenderingUtils.ReAllocateHandleIfNeeded(ref _tempBuffer2,    desc, name: "_TempBuffer2");

        _threadGroupSize = GetThreadGroupSize(_kawaseDownKernel, _textureSize);
        
        using (var builder = renderGraph.AddUnsafePass<PassData>("DualKawaseBlur", out var passData))
        {
            passData.camera = cameraData.camera;
            passData.computeShader   = _computeShader;
            passData.feature         = _feature;
            passData.renderedSource  = _renderedSource;
            passData.renderedOutput  = _renderedOutput;
            passData.tempBuffer1     = _tempBuffer1;
            passData.tempBuffer2     = _tempBuffer2;
            passData.cameraColor     = resourceData.activeColorTexture;
            passData.highlightKernel  = _highlightKernel;
            passData.kawaseDownKernel = _kawaseDownKernel;
            passData.kawaseUpKernel   = _kawaseUpKernel;
            passData.threadGroupSize  = _threadGroupSize;

            builder.UseTexture(passData.cameraColor);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) => ExecutePass(data, ctx));
        }
    }

    private static void ExecutePass(PassData data, UnsafeGraphContext ctx)
    {
        CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
    
        if (data.feature.targetObject && data.camera)
        {
            Vector3 pos = data.camera.WorldToScreenPoint(data.feature.targetObject.position);
            cmd.SetComputeVectorParam(data.computeShader, "_centre",
                new Vector4(
                    pos.x / data.feature.resolutionDivisor,
                    pos.y / data.feature.resolutionDivisor,
                    0, 0));
        }

        float r = (data.feature.radius / 100.0f) * data.feature.resolutionDivisor;
        cmd.SetComputeFloatParam(data.computeShader, "_radius",    r / data.feature.resolutionDivisor);
        cmd.SetComputeFloatParam(data.computeShader, "_edgeWidth", r * data.feature.softenEdge / 100.0f / data.feature.resolutionDivisor);
        cmd.SetComputeFloatParam(data.computeShader, "_shade",     data.feature.shade);
        cmd.SetComputeIntParam  (data.computeShader, "width",      data.renderedSource.rt.width);
        cmd.SetComputeIntParam  (data.computeShader, "height",     data.renderedSource.rt.height);
        
        // blit camera → renderedSource
        cmd.Blit(data.cameraColor, data.renderedSource.rt);
        
        // ping-pong blur
        RenderTexture current = data.renderedSource.rt;

        for (int i = 0; i < data.feature.passes; i++)
        {
            RenderTexture target = (i % 2 == 0) ? data.tempBuffer1.rt : data.tempBuffer2.rt;
            DispatchPass(cmd, data.computeShader, data.kawaseDownKernel, current, target, data.feature.offset, data.threadGroupSize);
            current = target;
        }

        for (int i = 0; i < data.feature.passes - 1; i++)
        {
            RenderTexture target = (current == data.tempBuffer1.rt) ? data.tempBuffer2.rt : data.tempBuffer1.rt;
            DispatchPass(cmd, data.computeShader, data.kawaseUpKernel, current, target, data.feature.offset, data.threadGroupSize);
            current = target;
        }

        RenderTexture finalBlur = (current == data.tempBuffer1.rt) ? data.tempBuffer2.rt : data.tempBuffer1.rt;
        DispatchPass(cmd, data.computeShader, data.kawaseUpKernel, current, finalBlur, data.feature.offset, data.threadGroupSize);
        
        cmd.SetComputeTextureParam(data.computeShader, data.highlightKernel, "rendered_source",  finalBlur);
        cmd.SetComputeTextureParam(data.computeShader, data.highlightKernel, "original_source",  data.renderedSource.rt);
        cmd.SetComputeTextureParam(data.computeShader, data.highlightKernel, "rendered_output",  data.renderedOutput.rt);
        cmd.DispatchCompute(data.computeShader, data.highlightKernel, data.threadGroupSize.x, data.threadGroupSize.y, 1);

        // blit result → camera
        cmd.Blit(data.renderedOutput.rt, data.cameraColor);
    }
    
    private static void DispatchPass(CommandBuffer cmd, ComputeShader shader, int kernel,
        RenderTexture source, RenderTexture destination, float offset, Vector2Int threadGroupSize)
    {
        cmd.SetComputeTextureParam(shader, kernel, "rendered_source", source);
        cmd.SetComputeTextureParam(shader, kernel, "rendered_output", destination);
        cmd.SetComputeFloatParam  (shader, "offset", offset);
        cmd.DispatchCompute       (shader, kernel, threadGroupSize.x, threadGroupSize.y, 1);
    }
    
    public override void Dispose()
    {
        base.Dispose();
        _tempBuffer1?.Release();
        _tempBuffer2?.Release();
    }
}

