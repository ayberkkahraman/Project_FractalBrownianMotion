using System.Buffers;
using System.Collections;

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
        [Range(5,200)]public int Width = 50;
        [Range(5,200)]public int Length = 50;
        [Range(1,50)]public float Height = 10f;

        [Header("Noise Settings")]
        [Range(0.001f,0.5f)]public float Scale = 0.1f;
        [Range(1,6)]public int Octaves = 4;
        [Range(0,4)]public float Lacunarity = 2f;
        [Range(0f,2f)]public float Persistence = 0.5f;

        [Header("Detail & Chunking")]
        [Range(1, 10)]
        public int Detail = 1;
        [Range(1,20)]public int ChunkSize = 10;
        public Material TerrainMaterial;
        public Material BoundaryMaterial;

        private Vector3 _camTarget;
        private Vector3 _previousCenter;
        private Vector3 _center;
        private int _initialXPosition;
        private int _initialZPosition;
        private int _vertexCount;
        private int _vertArrayLength;
        private int _trisArrayLength;
        private Vector3[] _vertices;
        private int[] _triangles;

        private void Awake()
        {
            CamOwner = this;
        }
        void Start()
        {
            _center = GetTerrainCenter();
            _previousCenter = _center;
            
            GenerateTerrain();
            UpdateSize();
        }

        public void UpdateSize()
        {
            _center = GetTerrainCenter();
            if(_previousCenter == _center) return;
            
            _previousCenter = _center;
            CamOwner.UpdateCam(_center);
            CamOwner.CameraManager.UpdateDistance(((Width+Length)/2) + 15f);
            DrawTerrainWireBox();
        }

        public void GenerateTerrain()
        {
            GenerateChunks();
            // DrawTerrainWireBox();
            
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

        private void DrawTerrainWireBox()
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (Transform chunk in transform)
            {
                MeshFilter filter = chunk.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;

                Vector3[] verts = filter.sharedMesh.vertices;
                foreach (var v in verts)
                {
                    Vector3 worldV = chunk.transform.TransformPoint(v);
                    min = Vector3.Min(min, worldV);
                    max = Vector3.Max(max, worldV);
                }
            }

            GameObject wireObj = new GameObject("TerrainWireBounds");
            wireObj.transform.SetParent(transform);
            LineRenderer lineRenderer = wireObj.AddComponent<LineRenderer>();

            lineRenderer.loop = false;
            lineRenderer.widthMultiplier = 0.2f;
            lineRenderer.material = BoundaryMaterial;
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;
            lineRenderer.useWorldSpace = true;
            lineRenderer.numCapVertices = 5;
            lineRenderer.numCornerVertices = 15;

            Vector3[] corners = new Vector3[8];
            corners[0] = new Vector3(min.x, min.y, min.z);
            corners[1] = new Vector3(max.x, min.y, min.z);
            corners[2] = new Vector3(max.x, min.y, max.z);
            corners[3] = new Vector3(min.x, min.y, max.z);

            corners[4] = new Vector3(min.x, max.y, min.z);
            corners[5] = new Vector3(max.x, max.y, min.z);
            corners[6] = new Vector3(max.x, max.y, max.z);
            corners[7] = new Vector3(min.x, max.y, max.z);

            Vector3[] lines = {
                corners[0], corners[1],
                corners[1], corners[2],
                corners[2], corners[3],
                corners[3], corners[0],

                corners[0], corners[4],
                corners[1], corners[5],
                corners[2], corners[6],
                corners[3], corners[7],

                corners[4], corners[5],
                corners[5], corners[6],
                corners[6], corners[7],
                corners[7], corners[4]
            };

            lineRenderer.positionCount = lines.Length;
            lineRenderer.SetPositions(lines);
        }

        public Vector3 GetTerrainCenter()
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (Transform chunk in transform)
            {
                MeshFilter filter = chunk.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;

                Vector3[] verts = filter.sharedMesh.vertices;
                foreach (var v in verts)
                {
                    Vector3 worldV = chunk.transform.TransformPoint(v);
                    min = Vector3.Min(min, worldV);
                    max = Vector3.Max(max, worldV);
                }
            }

            return (min + max) * 0.5f;
        }


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
        }
        
        public IEnumerator GenerateChunksCoroutine()
        {
            int chunksX = Mathf.CeilToInt((float)Width / ChunkSize);
            int chunksZ = Mathf.CeilToInt((float)Length / ChunkSize);

            for (int cx = 0; cx < chunksX; cx++)
            {
                for (int cz = 0; cz < chunksZ; cz++)
                {
                    GenerateChunk(cx, cz);

                    // Belirli aralıklarla frame başına çalış
                    if ((cx * chunksZ + cz) % 2 == 0)
                        yield return null;
                }
            }
        }

        
        private void ApplyMaterial(MeshRenderer renderer)
        {
            renderer.sharedMaterial = TerrainMaterial;
            Material mat = renderer.sharedMaterial;
        }



        void GenerateChunk(int chunkX, int chunkZ)
        {
            _initialXPosition = chunkX * ChunkSize * Detail;
            _initialZPosition = chunkZ * ChunkSize * Detail;
            _vertexCount = ChunkSize * Detail + 1;

            _vertArrayLength = _vertexCount * _vertexCount;
            _trisArrayLength = ChunkSize * Detail * ChunkSize * Detail * 6;

            _vertices = ArrayPool<Vector3>.Shared.Rent(_vertArrayLength);
            _triangles = ArrayPool<int>.Shared.Rent(_trisArrayLength);

            for (int z = 0, i = 0; z <= ChunkSize * Detail; z++)
            {
                for (int x = 0; x <= ChunkSize * Detail; x++, i++)
                {
                    float worldX = (_initialXPosition + x) / (float)(Width * Detail) * Width;
                    float worldZ = (_initialZPosition + z) / (float)(Length * Detail) * Length;
                    float y = FractalBrownianMotion(worldX * Scale, worldZ * Scale) * Height;

                    _vertices[i] = new Vector3(worldX, y, worldZ);
                }
            }

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

            ArrayPool<Vector3>.Shared.Return(_vertices, clearArray: false);
            ArrayPool<int>.Shared.Return(_triangles, clearArray: false);
        }
        
        float FractalBrownianMotion(float x, float z)
        {
            float total = 0f;
            float frequency = 1f;
            float amplitude = 1f;
            float maxAmplitude = 0f;

            for (int i = 0; i < Octaves; i++)
            {
                total += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
                maxAmplitude += amplitude;
                amplitude *= Persistence;
                frequency *= Lacunarity;
            }

            return total / maxAmplitude;
        }
        
        #if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            foreach (Transform chunk in transform)
            {
                MeshFilter filter = chunk.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;

                Vector3[] verts = filter.sharedMesh.vertices;

                Gizmos.color = Color.black;
                foreach (var v in verts)
                {
                    Gizmos.DrawSphere(chunk.transform.position + v, 0.05f);
                }
            }
        }
        #endif
        
    }

    
}