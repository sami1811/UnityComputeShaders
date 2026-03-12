using UnityEngine;

public class MyNightVision : MyBasePP
{
    [SerializeField]
    private Transform _targetTransform = null;
    
    [SerializeField]
    private Color tintColor =  Color.dodgerBlue;
    
    [SerializeField,Range(0.0f, 1.0f)]
    private float tintStrength = 1.0f;
    
    [SerializeField, Range(1.0f, 10.0f)]
    private float contrast = 1.0f;
    
    [SerializeField, Range(0.0f, 100.0f)]
    private float radius = 1.0f;
    
    [SerializeField,Range(0.0f, 100.0f)]
    private float softenEdge = 1.0f;

    [SerializeField, Range(5, 500)]
    private int lines = 50;
    
    [SerializeField, Range(0.0f, 1.0f)]
    private float lineContrast =  1.0f;

    [SerializeField] private float lenseOffset = 1.0f;

    protected override void Init()
    {
        _kernelName = "MyNightVision";
        base.Init();
    }
    protected override void SetProperties()
    {
        float rad = (radius / 100.0f) * _textureSize.y / resolutionDivisor;
        computeShader.SetFloat("_radius", rad);
        computeShader.SetFloat("_edgeWidth", rad * softenEdge / 100.0f);
        computeShader.SetVector("_tintColor", tintColor);
        computeShader.SetFloat("_tintStrength", tintStrength);
        computeShader.SetInt("_lines", lines);
        computeShader.SetInt("_resolutionDivisor", resolutionDivisor);
        computeShader.SetFloat("_contrast", contrast);
        computeShader.SetFloat("_lineContrast",  lineContrast);
        computeShader.SetFloat("_lenseOffset", lenseOffset);
    }

    protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        computeShader.SetFloat("_time", Time.time);
        base.OnRenderImage(source, destination);
    }
}
