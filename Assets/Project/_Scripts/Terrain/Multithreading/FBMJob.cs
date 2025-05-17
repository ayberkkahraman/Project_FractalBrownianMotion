using UnityEngine;

namespace Project._Scripts.Terrain.Multithreading
{
  using Unity.Burst;
  using Unity.Collections;
  using Unity.Jobs;
  using Unity.Mathematics;

  [BurstCompile]
  public struct FBMJob : IJobParallelFor
  {
    [WriteOnly] public NativeArray<float> Heights;

    public int Detail;
    public int VertexCount;
    public float HeightMultiplier;
    public float Scale;

    public int Octaves;
    public float Persistence;
    public float Lacunarity;

    public float StartX;
    public float StartZ;
    public float Width;
    public float Length;

    public void Execute(int index)
    {
      int x = index % VertexCount;
      int z = index / VertexCount;

      float worldX = (StartX + x) / (Width * Detail) * Width;
      float worldZ = (StartZ + z) / (Length * Detail) * Length;

      float y = FBM(worldX * Scale, worldZ * Scale);
      Heights[index] = y * HeightMultiplier;
    }

    float FBM(float x, float z)
    {
      float total = 0f;
      float frequency = 1f;
      float amplitude = 1f;
      float maxAmplitude = 0f;

      for (int i = 0; i < Octaves; i++)
      {
        total += noise.snoise(new float2(x * frequency, z * frequency)) * amplitude;
        maxAmplitude += amplitude;
        amplitude *= Persistence;
        frequency *= Lacunarity;
      }

      return total / maxAmplitude;
    }
  }

}
