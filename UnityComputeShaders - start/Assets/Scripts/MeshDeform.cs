using UnityEngine;

public struct Vertex
{
    public Vector3 position;
    public Vector3 normals;
    
    public Vertex(Vector3 p, Vector3 n)
    {
        position.x = p.x;
        position.y = p.y;
        position.z = p.z;
        normals.x = n.x;
        normals.y = n.y;
        normals.z = n.z;
    }
}

public class MeshDeform : MonoBehaviour
{
    public ComputeShader shader;
    [Range(0.5f, 2.0f)]
	public float radius;
	
    int kernelHandle;
    Mesh mesh;

    private Vertex[] _vertexArray;
    private Vertex[] _initialArray;
    private ComputeBuffer _vertexBuffer;
    private ComputeBuffer _initialBuffer;
    
    // Use this for initialization
    void Start()
    {
    
        if (InitData())
        {
            InitShader();
        }
    }

    private bool InitData()
    {
        kernelHandle = shader.FindKernel("CSMain");

        MeshFilter mf = GetComponent<MeshFilter>();

        if (mf == null)
        {
            Debug.Log("No MeshFilter found");
            return false;
        }

        InitVertexArrays(mf.mesh);
        InitGPUBuffers();

        mesh = mf.mesh;

        return true;
    }

    private void InitShader()
    {
        shader.SetFloat("radius", radius);
    }
    
    private void InitVertexArrays(Mesh mesh)
    {
        _vertexArray = new Vertex[mesh.vertices.Length];
        _initialArray = new Vertex[mesh.vertices.Length];

        for (var i = 0; i < _vertexArray.Length; i++)
        {
            Vertex v1 = new Vertex(mesh.vertices[i], mesh.normals[i]);
            Vertex v2 = new Vertex(mesh.vertices[i], mesh.normals[i]);

            _vertexArray[i] = v1;
            _initialArray[i] = v2;
        }
    }

    private void InitGPUBuffers()
    {
        _vertexBuffer = new ComputeBuffer(_vertexArray.Length, sizeof(float) * 6);
        _initialBuffer = new ComputeBuffer(_initialArray.Length, sizeof(float) * 6);
        
        _vertexBuffer.SetData(_vertexArray);
        _initialBuffer.SetData(_initialArray);
        
        shader.SetBuffer(kernelHandle, "vertex_buffer", _vertexBuffer);
        shader.SetBuffer(kernelHandle, "initial_buffer", _initialBuffer);
    }
    
    void GetVerticesFromGPU()
    {
        _vertexBuffer.GetData(_vertexArray);
        
        Vector3[] vertices = new Vector3[_vertexArray.Length];
        Vector3[] normals = new Vector3[_vertexArray.Length];

        for (var i = 0; i < _vertexArray.Length; i++)
        {
            vertices[i] = _vertexArray[i].position;
            normals[i] = _vertexArray[i].normals;
        }
        
        mesh.vertices = vertices;
        mesh.normals = normals;
    }

    void Update(){
        if (shader)
        {
        	shader.SetFloat("radius", radius);
            float delta = (Mathf.Sin(Time.time) + 1)/ 2;
            shader.SetFloat("delta", delta);
            shader.Dispatch(kernelHandle, _vertexArray.Length, 1, 1);
            
            GetVerticesFromGPU();
        }
    }

    void OnDestroy()
    {
        _vertexBuffer.Release();
        _initialBuffer.Release();
    }
}

