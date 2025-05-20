using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Project._Scripts.Terrain.Multithreading
{
  /// <summary>
  /// fBm(Fractal Brownian Motion) Equation with seperated job
  /// </summary>
  [BurstCompile]
  public struct FBMJob : IJobParallelFor
  {
    [WriteOnly] public NativeArray<Vector3> Vertices;

    public int VertexCount;
    public float HeightMultiplier;
    public int Detail;
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
      float worldY = FBM(worldX, worldZ) * HeightMultiplier; // <-- DÜZENLENDİ

      Vertices[index] = new Vector3(worldX, worldY, worldZ);
    }

    float FBM(float x, float z)
    {
      float total = 0f;
      float frequency = 1f;
      float amplitude = 1f;
      float maxAmplitude = 0f;

      for (int i = 0; i < Octaves; i++)
      {
        float sampleX = x * Scale * frequency;        // <-- DÜZENLENDİ
        float sampleZ = z * Scale * frequency;        // <-- DÜZENLENDİ

        total += noise.snoise(new float2(sampleX, sampleZ)) * amplitude;
        maxAmplitude += amplitude;
        amplitude *= Persistence;
        frequency *= Lacunarity;
      }

      return total / maxAmplitude;
    }

  }

}
