using System.Buffers;
using System.Collections.Generic;
using Project._Scripts.Terrain.Multithreading;
using Unity.Collections;
using Unity.Jobs;

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
        private List<Transform> _chunks;

        private void Awake()
        {
            _chunks = new List<Transform>();
            CamOwner = this;
        }
        void Start()
        {
            _center = GetTerrainCenter();
            
            GenerateTerrain();
            UpdateSize();
            
        }
        
        void OnRenderObject()
        {
            DrawTerrainWireBox();
        }
        
        public void DrawBoundary()
        {
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
            _chunks.Clear();
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
            _combinedBounds = new Bounds();
            
            int chunksX = Mathf.CeilToInt((float)Width / ChunkSize);
            int chunksZ = Mathf.CeilToInt((float)Length / ChunkSize);

            for (int cx = 0; cx < chunksX; cx++)
            {
                for (int cz = 0; cz < chunksZ; cz++)
                {
                    var chunk = GenerateChunk(cx, cz);
                    _combinedBounds.Encapsulate(chunk.GetComponent<MeshFilter>().sharedMesh.bounds);
                    // GenerateChunk(cx, cz);
                }
            }

            _center = _combinedBounds.center;
        }
        
        private void ApplyMaterial(MeshRenderer meshRenderer)
        {
            meshRenderer.sharedMaterial = TerrainMaterial;
        }


        //----------MARKED
        GameObject GenerateChunk(int chunkX, int chunkZ)
        {
            _initialXPosition = chunkX * ChunkSize * Detail;
            _initialZPosition = chunkZ * ChunkSize * Detail;
            _vertexCount = ChunkSize * Detail + 1;
            _totalVertexCount = _vertexCount * _vertexCount;
            _trisArrayLength = ChunkSize * Detail * ChunkSize * Detail * 6;

            _vertices = ArrayPool<Vector3>.Shared.Rent(_totalVertexCount);
            _triangles = ArrayPool<int>.Shared.Rent(_trisArrayLength);
            
            var heights = new NativeArray<float>(_totalVertexCount, Allocator.TempJob);
            
            var fbmJob = new FBMJob
            {
                Heights = heights,
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

            JobHandle verticesHandle = fbmJob.Schedule(_totalVertexCount, 64); // 64 batch size
            verticesHandle.Complete();

            for (int z = 0, i = 0; z < _vertexCount; z++)
            {
                for (int x = 0; x < _vertexCount; x++, i++)
                {
                    float worldX = (_initialXPosition + x) / (float)(Width * Detail) * Width;
                    float worldZ = (_initialZPosition + z) / (float)(Length * Detail) * Length;
                    float y = heights[i];

                    _vertices[i] = new Vector3(worldX, y, worldZ);
                }
            }

            heights.Dispose();

            int tris = 0;
            int vert = 0;
            for (int z = 0; z < ChunkSize * Detail; z++)
            {
                for (int x = 0; x < ChunkSize * Detail; x++)
                {
                    _triangles[tris + 0] = vert + 0;
                    _triangles[tris + 1] = vert + _vertexCount;
                    _triangles[tris + 2] = vert + 1;
                    _triangles[tris + 3] = vert + 1;
                    _triangles[tris + 4] = vert + _vertexCount;
                    _triangles[tris + 5] = vert + _vertexCount + 1;

                    vert++;
                    tris += 6;
                }
                vert++;
            }

            GameObject chunk = new GameObject($"Chunk_{chunkX}_{chunkZ}")
            {
                transform =
                {
                    parent = transform
                }
            };
            
            _chunks.Add(chunk.transform);

            Mesh mesh = new Mesh
            {
                vertices = _vertices[..(_vertexCount * _vertexCount)],
                triangles = _triangles[..tris]
            };
            mesh.RecalculateNormals();

            MeshFilter filter = chunk.AddComponent<MeshFilter>();
            filter.mesh = mesh;

            MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
            ApplyMaterial(meshRenderer);

            ArrayPool<Vector3>.Shared.Return(_vertices, clearArray: true);
            ArrayPool<int>.Shared.Return(_triangles, clearArray: true);

            return chunk;
        }
    }

    
}