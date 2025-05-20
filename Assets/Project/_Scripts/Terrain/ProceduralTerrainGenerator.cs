using UnityEngine;
using Project._Scripts.Terrain.Multithreading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Rendering;

namespace Project._Scripts.Terrain
{
    /// <summary>
    /// Generates a terrain composed of multiple chunks using fBm.
    /// Each chunk is an independent mesh to optimize rendering performance.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ProceduralTerrainGenerator : MonoBehaviour, ICamOwner
    {
        #region Components
        public ICamOwner CamOwner { get; set; }
        #endregion

        #region Fields
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
        
        [Header("Materials")]
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
        private readonly Vector3[] _boundaryVerts = new Vector3[8];
        private int[] _triangles;
        private NativeArray<Vector3> _verticesArray;
        private NativeArray<int> _trianglesArray;
        private bool _drawBoundary;
        #endregion

        #region Unity Functions
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
        #endregion

        #region Terrain Operations
        //----------------------------------------------------------------------
        //--------------------------TERRAIN OPERATIONS--------------------------
        //----------------------------------------------------------------------
        /// <summary>
        /// Initial Generation Section of the Terrain
        /// </summary>
        public void GenerateTerrain() => GenerateChunks();
        
        /// <summary>
        /// Sets the boundary
        /// </summary>
        /// <param name="value"></param>
        public void SetBoundary(bool value) => _drawBoundary = value;
        
        /// <summary>
        /// Destroys the last terrain for the new creation
        /// </summary>
        void ClearTerrain()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
        
        /// <summary>
        /// Updates the terrain size based on "Height" & "Width"
        /// </summary>
        public void UpdateSize()
        {
            CamOwner.UpdateCam(_center);
            CamOwner.CameraManager.UpdateDistance(((Width+Length)/2) + 15f);
            DrawBoundary();
        }
        
        /// <summary>
        /// Regeneration of the terrain when the specs have changed
        /// </summary>
        public void RegenerateTerrain()
        {
            ClearTerrain();
            GenerateTerrain();
        }

        /// <summary>
        /// Gets the center position of the terrain
        /// </summary>
        /// <returns></returns>
        public Vector3 GetTerrainCenter()
        {
            _minPos = Vector3.zero;
            _minPos.y = -Height;
            _maxPos = new Vector3(Width, Height, Length);
            
            var centerPos = (_minPos + _maxPos) * 0.5f;

            return centerPos;
        }
        //----------------------------------------------------------------------
        #endregion

        #region OpenGL Operations
        //------------------------------------------------------------------------
        //----------------------DRAWING BOUNDARY WITH OPENGL----------------------
        //------------------------------------------------------------------------
        
        /// <summary>
        /// Built-in Unity function to draw over graphics
        /// </summary>
        void OnRenderObject()
        {
            if(!_drawBoundary) return;
            
            DrawTerrainWireBox();
        }
        
        /// <summary>
        /// Draws the lines
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        void Draw(Vector3 a, Vector3 b) { GL.Vertex(a); GL.Vertex(b); }
        
        /// <summary>
        /// Draws the boundary for the correct signal
        /// </summary>
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
        
        /// <summary>
        /// Draws the 3D space wire box as a boundary
        /// </summary>
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
        //------------------------------------------------------------------------
        #endregion
        
        #region Terrain Generation
        //----------------------------------------------------------------------------
        //-----------------------------TERRAIN GENERATION-----------------------------
        //----------------------------------------------------------------------------
        /// <summary>
        /// Generation of the seperated chunks for the terrain
        /// </summary>
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
        
        /// <summary>
        /// Applies the visual rendering effects based on the vertex height
        /// </summary>
        /// <param name="meshRenderer"></param>
        private void ApplyMaterial(MeshRenderer meshRenderer) => meshRenderer.sharedMaterial = TerrainMaterial;


        /// <summary>
        /// Chunk Generation for the Terrain. -> The Key of the generation
        /// </summary>
        /// <param name="chunkX"></param>
        /// <param name="chunkZ"></param>
        void GenerateChunk(int chunkX, int chunkZ)
        {
            _initialXPosition = chunkX * ChunkSize * Detail;
            _initialZPosition = chunkZ * ChunkSize * Detail;
            _vertexCount = ChunkSize * Detail + 1;
            _totalVertexCount = _vertexCount * _vertexCount;
            _trisArrayLength = ChunkSize * Detail * ChunkSize * Detail * 6;

            //------------------ Preventing GC(Garbage Collection) with the NativeArray collection
            _verticesArray = new NativeArray<Vector3>(_totalVertexCount, Allocator.TempJob);
            _trianglesArray = new NativeArray<int>(_trisArrayLength, Allocator.TempJob);

            //------------------ fBm equation with seperated jobs
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
            

            //------------------ Triangle Generation of the polygon with seperated jobs
            var triangleJob = new TriangleGenerationJob
            {
                Triangles = _trianglesArray,
                VertexCount = _vertexCount,
                ChunkSize = ChunkSize,
                Detail = Detail
            };


            JobHandle triangleHandle = triangleJob.Schedule(fbmHandle);
            triangleHandle.Complete();

            //------------------ The stage of GC(Garbage Collection) free Mesh Generation
            Mesh mesh = new Mesh();

            var meshDataArray = Mesh.AllocateWritableMeshData(mesh);
            var meshData = meshDataArray[0];

            //------------------ Vertex Buffer
            meshData.SetVertexBufferParams(_totalVertexCount,
                new VertexAttributeDescriptor(VertexAttribute.Position));

            var vertexBuffer = meshData.GetVertexData<Vector3>();
            vertexBuffer.CopyFrom(_verticesArray);

            //------------------ Index Buffer
            meshData.SetIndexBufferParams(_trisArrayLength, IndexFormat.UInt32);
            var indexBuffer = meshData.GetIndexData<int>();
            indexBuffer.CopyFrom(_trianglesArray);

            //------------------ Sub-mesh Setting
            meshData.subMeshCount = 1;
            meshData.SetSubMesh(0, new SubMeshDescriptor(0, _trisArrayLength));

            //------------------ Application of the mesh data and dispose
            Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, mesh);
            mesh.RecalculateNormals();

            
            //------------------ Instantiating of the "Chunk" Object
            GameObject chunk = new GameObject($"Chunk_{chunkX}_{chunkZ}")
            {
                transform = { parent = transform }
            };

            MeshFilter filter = chunk.AddComponent<MeshFilter>();
            filter.mesh = mesh;

            MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
            ApplyMaterial(meshRenderer);

            //------------------ The cleaning of the NativeArray sets
            _verticesArray.Dispose();
            _trianglesArray.Dispose();
        }
        //----------------------------------------------------------------------------
        #endregion
        
    }
}