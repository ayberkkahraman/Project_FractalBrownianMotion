using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Project._Scripts.Terrain.Multithreading
{
  [BurstCompile]
  public struct TriangleGenerationJob : IJob
  {
    [WriteOnly] public NativeArray<int> Triangles;
    
    public int VertexCount;
    public int ChunkSize;
    public int Detail;

    public void Execute()
    {
      int vertexPerLine = VertexCount;
      int tris = 0;
      int vert = 0;

      for (int z = 0; z < ChunkSize * Detail; z++)
      {
        for (int x = 0; x < ChunkSize * Detail; x++)
        {
          Triangles[tris + 0] = vert + 0;
          Triangles[tris + 1] = vert + vertexPerLine;
          Triangles[tris + 2] = vert + 1;
          Triangles[tris + 3] = vert + 1;
          Triangles[tris + 4] = vert + vertexPerLine;
          Triangles[tris + 5] = vert + vertexPerLine + 1;

          vert++;
          tris += 6;
        }
        vert++;
      }
    }
  }

}
