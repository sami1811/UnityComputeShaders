using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DualKawaseBlur_Feature : MyBaseURP_Feature
{
    [Header("Target")]
    public Transform targetObject;

    [Header("Blur")]
    [Range(1, 25)]      public int passes      = 4;
    [Range(0.1f, 5.0f)] public float offset    = 1.0f;

    [Header("Highlight")]
    [Range(1.0f, 100.0f)] public float radius     = 1.0f;
    [Range(1.0f, 50.0f)]  public float softenEdge = 1.0f;
    [Range(0.01f, 1.0f)]  public float shade      = 1.0f;

    protected override MyBaseURP_Pass CreatePass()
    {
        return new DualKawaseBlur_Pass(computeShader, resolutionDivisor, this);
    }
}
