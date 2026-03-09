using UnityEngine;

public class DualKawaseBlur : MyBasePP
{
    [SerializeField]
    private Transform targetObject =  null;
    
    [SerializeField][Range(1, 4)]
    private int passes = 4;
    
    [SerializeField][Range(0.1f, 5.0f)]
    private float offset = 1.0f;
    
    [SerializeField][Range(1.0f, 100.0f)]
    private float radius = 1.0f;
    
    [SerializeField][Range(1.0f, 50.0f)]
    private float softenEdge = 1.0f;
    
    [SerializeField][Range(0.01f, 1.0f)]
    private float shade = 1.0f;

    private int _highlightKernel = -1;
    private int _kawaseDownKernel = -1;
    private int _kawaseUpKernel   = -1;

    // Ping-pong buffers — we alternate between these across passes
    private RenderTexture _tempBuffer1 = null;
    private RenderTexture _tempBuffer2 = null;
    
    private Vector3 _centre = Vector3.zero;

    protected override void Init()
    {
        if (!computeShader) return;
        
        if (!targetObject)
        {
            Debug.LogError($"Target Object is not assigned!");
            return;
        }
        
        _kernelName = "KawaseDown"; // base validates this kernel exists
        
        _highlightKernel = computeShader.FindKernel("Highlight");
        _kawaseDownKernel = computeShader.FindKernel("KawaseDown");
        _kawaseUpKernel   = computeShader.FindKernel("KawaseUp");
        
        base.Init();
    }

    protected override void CreateTextures()
    {
        base.CreateTextures();
        
        CreateTexture(ref _tempBuffer1, resolutionDivisor);
        CreateTexture(ref _tempBuffer2,  resolutionDivisor);
        
        computeShader.SetTexture(_highlightKernel, "original_source", _renderedSource);
        computeShader.SetTexture(_highlightKernel, "rendered_output", _renderedOutput);
    }
    
    protected override void SetProperties()
    {
        float r = (radius / 100.0F) * _textureSize.y;
        float edgeWidth = r * softenEdge /  100.0f;
        
        computeShader.SetFloat("_radius", r / resolutionDivisor);
        computeShader.SetFloat("_edgeWidth", edgeWidth / resolutionDivisor);
        computeShader.SetFloat("_shade", shade);
    }
    
    protected override void ClearTextures()
    {
        base.ClearTextures();
        
        ClearTexture(ref _tempBuffer1);
        ClearTexture(ref _tempBuffer2);
    }

    private void SetSize()
    {
        computeShader.SetInt("width",  _textureSize.x / resolutionDivisor);
        computeShader.SetInt("height", _textureSize.y / resolutionDivisor);
    }
    
    private void CalculateCentre(Transform objectToTarget)
    {
        if (!objectToTarget)
        {
            return;
        }
        
        Vector3 objectPosition = _thisCamera.WorldToScreenPoint(objectToTarget.position);
        _centre.x = objectPosition.x / resolutionDivisor;
        _centre.y = objectPosition.y / resolutionDivisor;
        
        computeShader.SetVector("_centre", _centre);
    }

    private void DispatchPass(int kernelHandle, RenderTexture source, RenderTexture destination)
    {
        computeShader.SetTexture(kernelHandle, "rendered_source", source);
        computeShader.SetTexture(kernelHandle, "rendered_output", destination);
        computeShader.SetFloat("offset", offset);
        computeShader.Dispatch(kernelHandle, _threadGroupSize.x, _threadGroupSize.y, 1);
    }

    protected override void DispatchCompute(ref RenderTexture source, ref RenderTexture destination)
    {
        if (!_init) return;

        _dispatchMarker.Begin();
        Graphics.Blit(source, _renderedSource);

        SetSize();

        RenderTexture current = _renderedSource;

        // Downsample passes
        for (int i = 0; i < passes; i++)
        {
            RenderTexture target = (i % 2 == 0) ? _tempBuffer1 : _tempBuffer2;
            DispatchPass(_kawaseDownKernel, current, target);
            current = target;
        }

        // Upsample passes
        for (int i = 0; i < passes - 1; i++) // ✅ stop one early
        {
            RenderTexture target = (current == _tempBuffer1) ? _tempBuffer2 : _tempBuffer1;
            DispatchPass(_kawaseUpKernel, current, target);
            current = target;
        }

        // ✅ Final upsample into _tempBuffer1 or _tempBuffer2 (whichever is free)
        RenderTexture finalBlur = (current == _tempBuffer1) ? _tempBuffer2 : _tempBuffer1;
        DispatchPass(_kawaseUpKernel, current, finalBlur);

        // ✅ Highlight reads finalBlur, writes to _renderedOutput — no collision
        computeShader.SetTexture(_highlightKernel, "rendered_source", finalBlur);
        computeShader.Dispatch(_highlightKernel, _threadGroupSize.x, _threadGroupSize.y, 1);

        Graphics.Blit(_renderedOutput, destination);
        _dispatchMarker.End();
    }
    
    protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!IsSupported)
        {
            Graphics.Blit(source, destination);
            return;
        }
        
        CalculateCentre(targetObject);
        CheckResolution(out bool resChange);

        if (resChange)
        {
            SetProperties();
        }
        
        DispatchCompute(ref source, ref destination);
    }
}