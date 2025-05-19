using System.Buffers;
using System.Collections.Generic;
using Project._Scripts.Terrain.Multithreading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Rendering;

namespace Project._Scripts.Terrain
{
    using UnityEngine;
    /// <summary>
    /// Generates a terrain composed of multiple chunks using fBm.
    /// Each chunk is an independent mesh to optimize rendering performance.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ProceduralTerrainGenerator : MonoBehaviour, ICamOwner
    {
        public ICamOwner CamOwner { get; set; }
        
        [Header("Terrain Dimensions")]
        [Range(5,25)]public int Width = 25;
        [Range(5,25)]public int Length = 25;
        [Range(1,10)]public float Height = 10f;

        [Header("Noise Settings")]
        [Range(0.001f,0.5f)]public float Scale = 0.1f;
        [Range(1,5)]public int Octaves = 4;
        [Range(0,3)]public float Lacunarity = 2f;
        [Range(0f,1f)]public float Persistence = 0.5f;

        [Header("Detail & Chunking")]
        [Range(1, 10)]
        public int Detail = 1;
        [Range(1,20)]public int ChunkSize = 10;
        public Material TerrainMaterial;
        public Material BoundaryMaterial;

        private Vector3 _minPos;
        private Vector3 _maxPos;
        private Vector3 _camTarget;
        private Vector3 _center;
        private int _initialXPosition;
        private int _initialZPosition;
        private int _vertexCount;
        private int _totalVertexCount;
        private int _vertArrayLength;
        private int _trisArrayLength;
        private Vector3[] _vertices;
        private Vector3[] _boundaryVerts = new Vector3[8];
        private int[] _triangles;
        private NativeArray<Vector3> _verticesArray;
        private NativeArray<int> _trianglesArray;
        private bool _drawBoundary;

        private void Awake()
        {
            _drawBoundary = true;
            CamOwner = this;
        }
        void Start()
        {
            _center = GetTerrainCenter();
            
            GenerateTerrain();
            UpdateSize();
            
        }

        public void SetBoundary(bool value)
        {
            _drawBoundary = value;
        }
        
        void OnRenderObject()
        {
            if(!_drawBoundary) return;
            
            DrawTerrainWireBox();
        }
        
        public void DrawBoundary()
        {
            if(!_drawBoundary) return;
            
            BoundaryMaterial.SetPass(0);

            _minPos = Vector3.zero;
            _minPos.y = -Height;
            _maxPos = new Vector3(Width, Height, Length);
            
            _boundaryVerts[0] = new Vector3(_minPos.x, _minPos.y, _minPos.z);
            _boundaryVerts[1] = new Vector3(_maxPos.x, _minPos.y, _minPos.z);
            _boundaryVerts[2] = new Vector3(_maxPos.x, _minPos.y, _maxPos.z);
            _boundaryVerts[3] = new Vector3(_minPos.x, _minPos.y, _maxPos.z);
            _boundaryVerts[4] = new Vector3(_minPos.x, _maxPos.y, _minPos.z);
            _boundaryVerts[5] = new Vector3(_maxPos.x, _maxPos.y, _minPos.z);
            _boundaryVerts[6] = new Vector3(_maxPos.x, _maxPos.y, _maxPos.z);
            _boundaryVerts[7] = new Vector3(_minPos.x, _maxPos.y, _maxPos.z);
        }


        public void UpdateSize()
        {
            CamOwner.UpdateCam(_center);
            CamOwner.CameraManager.UpdateDistance(((Width+Length)/2) + 15f);
            DrawBoundary();
        }

        public void GenerateTerrain()
        {
            GenerateChunks();
        }

        void ClearTerrain()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        public void RegenerateTerrain()
        {
            ClearTerrain();
            GenerateTerrain();
        }
        
        void Draw(Vector3 a, Vector3 b) { GL.Vertex(a); GL.Vertex(b); }
        
        public void DrawTerrainWireBox()
        {
            BoundaryMaterial.SetPass(0);
            GL.Begin(GL.LINES);
            GL.Color(Color.red);

            Draw(_boundaryVerts[0], _boundaryVerts[1]); Draw(_boundaryVerts[1], _boundaryVerts[2]); Draw(_boundaryVerts[2], _boundaryVerts[3]); Draw(_boundaryVerts[3], _boundaryVerts[0]);
            Draw(_boundaryVerts[4], _boundaryVerts[5]); Draw(_boundaryVerts[5], _boundaryVerts[6]); Draw(_boundaryVerts[6], _boundaryVerts[7]); Draw(_boundaryVerts[7], _boundaryVerts[4]);
            Draw(_boundaryVerts[0], _boundaryVerts[4]); Draw(_boundaryVerts[1], _boundaryVerts[5]); Draw(_boundaryVerts[2], _boundaryVerts[6]); Draw(_boundaryVerts[3], _boundaryVerts[7]);

            GL.End();
        }

        public Vector3 GetTerrainCenter()
        {
            _minPos = Vector3.zero;
            _minPos.y = -Height;
            _maxPos = new Vector3(Width, Height, Length);
            
            var centerPos = (_minPos + _maxPos) * 0.5f;

            return centerPos;
        }

        private Bounds _combinedBounds;
        
        //----------MARKED
        public void GenerateChunks()
        {
            int chunksX = Mathf.CeilToInt((float)Width / ChunkSize);
            int chunksZ = Mathf.CeilToInt((float)Length / ChunkSize);

            for (int cx = 0; cx < chunksX; cx++)
            {
                for (int cz = 0; cz < chunksZ; cz++)
                {
                    GenerateChunk(cx, cz);

                }
            }

            _center = GetTerrainCenter();
        }
        
        private void ApplyMaterial(MeshRenderer meshRenderer)
        {
            meshRenderer.sharedMaterial = TerrainMaterial;
        }


        //----------MARKED
        void GenerateChunk(int chunkX, int chunkZ)
        {
            _initialXPosition = chunkX * ChunkSize * Detail;
            _initialZPosition = chunkZ * ChunkSize * Detail;
            _vertexCount = ChunkSize * Detail + 1;
            _totalVertexCount = _vertexCount * _vertexCount;
            _trisArrayLength = ChunkSize * Detail * ChunkSize * Detail * 6;

            // NativeArray'ler ile GC-free bellek
            _verticesArray = new NativeArray<Vector3>(_totalVertexCount, Allocator.TempJob);
            _trianglesArray = new NativeArray<int>(_trisArrayLength, Allocator.TempJob);

            // FBMJob (yükseklik)
            var fbmJob = new FBMJob
            {
                Vertices = _verticesArray,
                VertexCount = _vertexCount,
                HeightMultiplier = Height,
                Detail = Detail,
                Scale = Scale,
                Octaves = Octaves,
                Persistence = Persistence,
                Lacunarity = Lacunarity,
                StartX = _initialXPosition,
                StartZ = _initialZPosition,
                Width = Width * Detail,
                Length = Length * Detail
            };
            
            JobHandle fbmHandle = fbmJob.Schedule(_totalVertexCount, 64);
            

            // TriangleJob (üçgenler)
            var triangleJob = new TriangleGenerationJob
            {
                Triangles = _trianglesArray,
                VertexCount = _vertexCount,
                ChunkSize = ChunkSize,
                Detail = Detail
            };


            JobHandle triangleHandle = triangleJob.Schedule(fbmHandle);
            triangleHandle.Complete();

            // === GC-FREE MESH VERİSİ YAZIMI ===
            Mesh mesh = new Mesh();

            var meshDataArray = Mesh.AllocateWritableMeshData(mesh);
            var meshData = meshDataArray[0];

            // Vertex buffer ayarı
            meshData.SetVertexBufferParams(_totalVertexCount,
                new VertexAttributeDescriptor(VertexAttribute.Position));

            var vertexBuffer = meshData.GetVertexData<Vector3>();
            vertexBuffer.CopyFrom(_verticesArray);

            // Index buffer ayarı
            meshData.SetIndexBufferParams(_trisArrayLength, IndexFormat.UInt32);
            var indexBuffer = meshData.GetIndexData<int>();
            indexBuffer.CopyFrom(_trianglesArray);

            // Submesh ayarı
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, _trisArrayLength));

            // GC-free mesh verisini uygula ve dispose et
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);

            // Normalleri yeniden hesaplama, bu işlem pahalıdır.
            // Eğer mümkünse normal verisini job veya hesaplamayla önceden oluştur
            mesh.RecalculateNormals();

            
            // === CHUNK NESNESİ OLUŞTURULUYOR ===
            GameObject chunk = new GameObject($"Chunk_{chunkX}_{chunkZ}")
            {
                transform = { parent = transform }
            };

            MeshFilter filter = chunk.AddComponent<MeshFilter>();
            filter.mesh = mesh;

            MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
            ApplyMaterial(meshRenderer);

            // NativeArray bellek temizliği
            _verticesArray.Dispose();
            _trianglesArray.Dispose();
        }
    }

    
}