using System;

namespace Project._Scripts.Terrain
{
    using UnityEngine;
    /// <summary>
    /// Generates a terrain composed of multiple chunks using fBm.
    /// Each chunk is an independent mesh to optimize rendering performance.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ProceduralTerrainGenerator : MonoBehaviour
    {
        [Header("Terrain Dimensions")]
        public int Width = 50;
        public int Length = 50;
        public float Height = 10f;

        [Header("Noise Settings")]
        public float Scale = 0.1f;
        public int Octaves = 4;
        public float Lacunarity = 2f;
        public float Persistence = 0.5f;

        [Header("Detail & Chunking")]
        [Range(1, 10)]
        public int Detail = 1;
        public int ChunkSize = 10;
        public Material TerrainMaterial;
        private static readonly int MinHeight = Shader.PropertyToID("_MinHeight");
        private static readonly int MaxHeight = Shader.PropertyToID("_MaxHeight");

        void Start()
        {
            GenerateChunks();
        }

        void GenerateChunks()
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
        
        private void ApplyMaterial(MeshRenderer renderer)
        {
            renderer.sharedMaterial = TerrainMaterial;
            Material mat = renderer.sharedMaterial;
        }



        void GenerateChunk(int chunkX, int chunkZ)
        {
            int startX = chunkX * ChunkSize * Detail;
            int startZ = chunkZ * ChunkSize * Detail;
            int vertexCount = ChunkSize * Detail + 1;

            Vector3[] vertices = new Vector3[vertexCount * vertexCount];
            int[] triangles = new int[ChunkSize * Detail * ChunkSize * Detail * 6];

            for (int z = 0, i = 0; z <= ChunkSize * Detail; z++)
            {
                for (int x = 0; x <= ChunkSize * Detail; x++, i++)
                {
                    float worldX = (startX + x) / (float)(Width * Detail) * Width;
                    float worldZ = (startZ + z) / (float)(Length * Detail) * Length;
                    float y = FractalBrownianMotion(worldX * Scale, worldZ * Scale) * Height;

                    vertices[i] = new Vector3(worldX, y, worldZ);
                }
            }

            int tris = 0;
            int vert = 0;
            for (int z = 0; z < ChunkSize * Detail; z++)
            {
                for (int x = 0; x < ChunkSize * Detail; x++)
                {
                    triangles[tris + 0] = vert + 0;
                    triangles[tris + 1] = vert + vertexCount;
                    triangles[tris + 2] = vert + 1;
                    triangles[tris + 3] = vert + 1;
                    triangles[tris + 4] = vert + vertexCount;
                    triangles[tris + 5] = vert + vertexCount + 1;

                    vert++;
                    tris += 6;
                }
                vert++;
            }

            // Create chunk GameObject
            GameObject chunk = new GameObject($"Chunk_{chunkX}_{chunkZ}");
            chunk.transform.parent = transform;

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            MeshFilter filter = chunk.AddComponent<MeshFilter>();
            filter.mesh = mesh;

            MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
            ApplyMaterial(meshRenderer);
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