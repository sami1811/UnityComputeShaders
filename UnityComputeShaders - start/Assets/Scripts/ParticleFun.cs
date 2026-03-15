using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0649

public class ParticleFun : MonoBehaviour
{

    private Vector2 cursorPos;

    // struct
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float life;
    }

    const int SIZE_PARTICLE = 7 * sizeof(float);

    public int particleCount = 1000000;
    public Material material;
    public ComputeShader shader;
    [Range(1, 10)]
    public int pointSize = 2;

    int kernelID;
    ComputeBuffer particleBuffer;

    int groupSizeX;
    private Camera _c;


    // Use this for initialization
    void Start()
    {
        _c = Camera.main;
        Init();
    }

    void Init()
    {
        // initialize the particles
        Particle[] particleArray = new Particle[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            //TO DO: Initialize particle
            Vector3 v = new Vector3();
            
            v.x = Random.value * 2.0f - 1.0f;
            v.y = Random.value * 2.0f - 1.0f;
            v.z = Random.value * 2.0f - 1.0f;
            
            v.Normalize();
            v *= Random.value * 2.5f;
            
            particleArray[i].position.x = v.x;
            particleArray[i].position.y = v.y;
            particleArray[i].position.z = v.z + 0.2f;
            
            particleArray[i].velocity.x = 0.0f;
            particleArray[i].velocity.y = 0.0f;
            particleArray[i].velocity.z = 0.0f;

            particleArray[i].life = Random.value * 5.0f + 3.0f;
        }

        // create compute buffer
        particleBuffer = new ComputeBuffer(particleCount, SIZE_PARTICLE);

        particleBuffer.SetData(particleArray);

        // find the id of the kernel
        kernelID = shader.FindKernel("CSParticle");

        uint threadsX;
        shader.GetKernelThreadGroupSizes(kernelID, out threadsX, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)particleCount / (float)threadsX);

        // bind the compute buffer to the shader and the compute shader
        shader.SetBuffer(kernelID, "particleBuffer", particleBuffer);
        shader.SetFloat("totalTime", Time.time);
        shader.SetFloat("attractorScale", 0.09f);
        material.SetBuffer("particleBuffer", particleBuffer);

        material.SetInt("_PointSize", pointSize);
        material.SetFloat("_Deltatime", Time.deltaTime);
    }

    void OnRenderObject()
    {
        material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, 1, particleCount);
    }

    void OnDestroy()
    {
        if (particleBuffer != null)
            particleBuffer.Release();
    }

    // Update is called once per frame
    void Update()
    {

        float[] mousePosition2D = { cursorPos.x, cursorPos.y };

        // Send datas to the compute shader
        shader.SetFloat("deltaTime", Time.deltaTime);
        shader.SetFloats("mousePosition", mousePosition2D);

        // Update the Particles
        shader.Dispatch(kernelID, groupSizeX, 1, 1);
    }

    void OnGUI()
    {
        Vector3 p = new Vector3();
        Event e = Event.current;
        Vector2 mousePos = new Vector2();

        // Get the mouse position from Event.
        // Note that the y position from Event is inverted.
        mousePos.x = e.mousePosition.x;
        mousePos.y = _c.pixelHeight - e.mousePosition.y;

        p = _c.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, _c.nearClipPlane + 14));// z = 3.

        cursorPos.x = p.x;
        cursorPos.y = p.y;
        
    }
}
