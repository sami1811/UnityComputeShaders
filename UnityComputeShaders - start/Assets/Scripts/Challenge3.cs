using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Challenge3 : MyBasePP
{
    [Range(0.0f, 1.0f)]
    public float height = 0.3f;
    [Range(0.0f, 1.0f)]
    public float softenEdge;
    [Range(0.0f, 1.0f)]
    public float shade;
    [Range(0.0f, 1.0f)]
    public float tintStrength;
    public Color tintColor = Color.white;

    Vector4 center;

    private void OnValidate()
    {
        _kernelName = "Challenge3";
        if(!_init)
            Init();
           
        SetProperties();
    }

    protected void SetProperties()
    {
        float tintHeight = height * _textureSize.y;
        computeShader.SetFloat("tintHeight", tintHeight);
        computeShader.SetFloat("edgeWidth", tintHeight * softenEdge / 100.0f);
        computeShader.SetFloat("shade", shade);
        computeShader.SetFloat("tintStrength", tintStrength);
        computeShader.SetVector("tintColor", tintColor);
    }

    protected override void OnRenderImage(RenderTexture source, RenderTexture destination)
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
