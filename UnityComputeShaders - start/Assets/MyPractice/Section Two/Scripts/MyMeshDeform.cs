using System;
using UnityEngine;

public struct MyVertex
{
    public Vector3 position;
    public Vector3 normal;

    public MyVertex(Vector3 pos, Vector3 norm)
    {
        position.x =  pos.x;
        position.y =  pos.y;
        position.z =  pos.z;
        normal.x =  norm.x;
        normal.y =  norm.y;
        normal.z =  norm.z;
    }
}

public class MyMeshDeform : MonoBehaviour
{
    [SerializeField] private ComputeShader computeShader;
    [SerializeField] private float speed = 1.0f;
    [SerializeField, Range(0.5f, 2.0f)] private float radius = 1.0f;
    
    private ComputeBuffer _vertexBuffer;
    private ComputeBuffer _initialBuffer;
    
    private Vertex[]  _vertexArray;
    private Vertex[] _initialArray;

    private int _kernelHandle;
    
    private Mesh _mesh;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (InitData())
        {
            InitShaderData();
        }
    }
    
    private bool InitData()
    {
        _kernelHandle = computeShader.FindKernel("CSMain");
        
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            Debug.LogWarning("MeshFilter not found");
            return false;
        }

        InitVertexData(meshFilter.mesh);
        InitDataToGPU();
        
        _mesh = meshFilter.mesh;
        
        return true;
    }

    private void InitVertexData(Mesh mesh)
    {
        _vertexArray = new Vertex[mesh.vertices.Length];
        _initialArray = new Vertex[mesh.vertices.Length];

        for (var i = 0; i < _vertexArray.Length; i++)
        {
            var v1 = new Vertex(mesh.vertices[i], mesh.normals[i]);
            var v2 = new Vertex(mesh.vertices[i], mesh.normals[i]);
            
            _initialArray[i] = v1;
            _vertexArray[i] = v2;
        }
    }

    private void InitDataToGPU()
    {
        _initialBuffer = new ComputeBuffer(_initialArray.Length, sizeof(float) * 6);
        _vertexBuffer = new ComputeBuffer(_vertexArray.Length, sizeof(float) * 6);
        
        _initialBuffer.SetData(_initialArray);
        _vertexBuffer.SetData(_vertexArray);
        
        computeShader.SetBuffer(_kernelHandle, "initial_buffer", _initialBuffer);
        computeShader.SetBuffer(_kernelHandle, "vertex_buffer", _vertexBuffer);
    }
    
    // Update is called once per frame
    void Update()
    {
        if (computeShader)
        {
            InitShaderData();
            DispatchShader();
            GetVerticesFromGPU();
        }
    }
    
    private void InitShaderData()
    {
        computeShader.SetFloat("_radius", radius);
        computeShader.SetFloat("_delta", GetDelta());
    }

    private float GetDelta()
    {
        return ((MathF.Sin(Time.time * speed) + 1.0f) / 2.0f);
    }

    private void DispatchShader()
    {
        computeShader.Dispatch(_kernelHandle, _vertexArray.Length, 1, 1);
    }
    
    private void GetVerticesFromGPU()
    {
        _vertexBuffer.GetData(_vertexArray);
        
        var vertices = new Vector3[_vertexArray.Length];
        var normals = new Vector3[_vertexArray.Length];
        
        for (var i = 0; i < _vertexArray.Length; i++)
        {
            vertices[i] = _vertexArray[i].position;
            normals[i] = _vertexArray[i].normals;
        }

        _mesh.vertices = vertices;
        _mesh.normals = normals;
    }
    
    private void OnDisable()
    {
        _initialBuffer.Release();
        _vertexBuffer.Release();
    }
}
