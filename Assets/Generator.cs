using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
public class Generator : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject cube;
    public int chunkSide;
    public Vector3Int chunkSize;
    [Range(0,1)]
    public float threshold;

    private float old_threshold;
    public float scale;
    private float old_sclae;
    public int xOffset;

    private int old_xOffset;
    public int yOffset; 
     private int old_yOffset;
    public int zOffset;
     private int old_zOffset;
    public float speed;
    public ComputeShader computeShader;
    private ComputeBuffer buffer;
    private ComputeBuffer triangles;
    private ComputeBuffer triangleCount;


    public Mesh mesh;
    private ComputeBuffer trigTable;
    GameObject[] chunk;
    public bool running = false; 
    public struct Vertex{
        public Vector3 position;
        public float val;
        public Vector4 color;

    }

    void Start()
    {
        running = true;
        mesh = new Mesh();
        old_sclae = scale;
        old_threshold = threshold;
        old_xOffset = xOffset;
        old_yOffset = yOffset;
        old_zOffset = zOffset;
        
        triangles = new ComputeBuffer((int)Mathf.Pow(chunkSide,3)*5, 3*4*4, ComputeBufferType.Append);
        //indices = new ComputeBuffer(chunkSize.x*chunkSize.y*chunkSize.z, );
        buffer = new ComputeBuffer((int)Mathf.Pow(chunkSide,3),8*4);
        triangleCount = new ComputeBuffer(1, 4, ComputeBufferType.Raw);
    
        GetComponent<MeshFilter>().sharedMesh = mesh;


        chunkSize.x = chunkSide;
        chunkSize.y = chunkSide;
        chunkSize.z = chunkSide;
        
       



       
        chunk = new GameObject[(int)Mathf.Pow(chunkSide,3)];
        for (int i = 0; i < chunk.Length; i++)
        {
            chunk[i] = null;
        }
        Debug.Log("C: " + chunk.Length);
        
        CalcNoiseCS();
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct Triangle{
        public Vector4 pos1;
        public Vector4 pos2;
        public Vector4 pos3;
    } 
    public int numTrigs;

    void CalcNoiseCS(){

        //computeShader.SetInt("chunk_size", chunkSide);
        computeShader.SetFloat("Scale", scale);
        computeShader.SetInt("chunk_size", chunkSide);
        computeShader.SetVector("chunk_offset", new Vector4(xOffset,yOffset,zOffset,0));
        computeShader.SetFloat("threshold", threshold);
        computeShader.SetBuffer(0,"_Buffer",buffer);
        computeShader.Dispatch(0, chunkSide / 8,  chunkSide / 8, chunkSide / 8);
        Vertex[] _buffer = new Vertex[buffer.count];
        
        buffer.GetData(_buffer);
        for (int i = 0; i < _buffer.Length; i++)
        {
            //Debug.Log(_buffer[i].val);
            //Debug.Log(_buffer[i].position);

        }

        triangles.SetCounterValue(0);
        computeShader.SetBuffer(1,"_Triangles", triangles);
        computeShader.SetBuffer(1,"_Buffer", buffer);
        
        computeShader.Dispatch(1, chunkSide / 8,  chunkSide / 8, chunkSide / 8);

        
        //calulate the number of triangles appended to the buffer;
        ComputeBuffer.CopyCount (triangles, triangleCount, 0);
        int[] triCountArray = { 0 };
        triangleCount.GetData (triCountArray);
        numTrigs = triCountArray[0];
        
        Debug.Log(numTrigs);
        
        Triangle[] _tris = new Triangle[numTrigs];
        //int[] _indices = new int[chunkSize.x*chunkSize.y*chunkSize.z]; 

        buffer.GetData(_buffer);
        triangles.GetData(_tris, 0, 0, numTrigs);

        Vector3[] _vertices = new Vector3[_tris.Length*3]; 
        Color[] _colors =new Color[_tris.Length*3];
        int[] _triangles = new int[_tris.Length*3]; 

        for(int i = 0; i < numTrigs;i++){
            _vertices[3*i] = new Vector3(_tris[i].pos1.x,_tris[i].pos1.y,_tris[i].pos1.z);
            _colors[3*i] = new Color(_tris[i].pos1.w,_tris[i].pos1.w,_tris[i].pos1.w);
            
            _vertices[3*i+1] = new Vector3(_tris[i].pos2.x,_tris[i].pos2.y,_tris[i].pos2.z);
            _colors[3*i+1] = new Color(_tris[i].pos2.w,_tris[i].pos2.w,_tris[i].pos2.w);

            _vertices[3*i+2] = new Vector3(_tris[i].pos3.x,_tris[i].pos3.y,_tris[i].pos3.z);
            _colors[3*i+2] = new Color(_tris[i].pos3.w,_tris[i].pos3.w,_tris[i].pos3.w);


            _triangles[3*i] = 3*i;
            _triangles[3*i+1] = 3*i+1;
            _triangles[3*i+2] = 3*i+2;

            //Debug.Log(_tris[i].pos1 + ", " +  _tris[i].pos2 +", "+ _tris[i].pos3);
        }
        //Debug.Log("Verts: " + _vertices.Length);
        //Debug.Log("COLORS: " + _colors.Length);

        Debug.Log("Tris: " + _triangles.Length);
        mesh.Clear();
        mesh.vertices = _vertices;
        mesh.colors = _colors;
        mesh.triangles = _triangles;
        mesh.RecalculateNormals();
        // Copy the pixel data to the texture and load it into the GPU.
    }

    private void OnDrawGizmos() {
        if(running){
            int iter = 100;
            if (numTrigs < iter)
                iter = numTrigs*3;
            for (int i = 0; i < iter; i++)
            {
                Gizmos.DrawSphere(mesh.vertices[i], 0.2f);
                Debug.Log(mesh.vertices[i]);
            }
        
        }
    }
    GameObject SpawnCube(Vector3 pos, Vector4 color, string name){
        GameObject spawned = Instantiate(cube, pos, Quaternion.identity);
        spawned.GetComponent<MeshRenderer>().materials[0].color = color;
        spawned.name = name;
        return spawned;
    }
    void Update()
    {
        if(old_sclae != scale || old_threshold!=threshold || old_xOffset != xOffset ||  old_yOffset != yOffset|| old_zOffset != zOffset)         
        {
            CalcNoiseCS();
            old_sclae = scale;
            old_threshold = threshold;
            old_xOffset = xOffset;
            old_yOffset = yOffset;
            old_zOffset = zOffset;
        }
        //zOffset += Time.deltaTime*speed;
        
        
    }

    private void OnDisable()  {
        buffer.Release();    
       
        triangles.Release();

        triangleCount.Release();
    }
}