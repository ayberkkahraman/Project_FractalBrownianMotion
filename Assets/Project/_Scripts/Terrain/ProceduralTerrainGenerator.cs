using UnityEngine;

namespace Project._Scripts.Terrain
{
    using UnityEngine;

/// <summary>
/// Generates a procedural terrain mesh using Fractal Brownian Motion (fBm).
/// Adjustable detail level allows control over terrain resolution.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralTerrainGenerator : MonoBehaviour
{
    [Header("Terrain Dimensions")]
    public int Width = 50;      // Size along X axis (in world units)
    public int Length = 50;     // Size along Z axis (in world units)
    public float Height = 10f;  // Vertical scale

    [Header("Noise Settings")]
    public float Scale = 0.1f;
    public int Octaves = 4;
    public float Lacunarity = 2f;
    public float Persistence = 0.5f;

    [Header("Detail Settings")]
    [Range(1, 3)]
    public int Detail = 1; // Controls terrain resolution (1 = default, 3 = high poly)

    private Mesh _mesh;
    private Vector3[] _vertices;
    private int[] _triangles;

    void Start()
    {
        GenerateTerrain();
    }

    void GenerateTerrain()
    {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;

        // Effective resolution with detail factor applied
        int resX = Width * Detail;
        int resZ = Length * Detail;

        _vertices = new Vector3[(resX + 1) * (resZ + 1)];
        for (int z = 0, i = 0; z <= resZ; z++)
        {
            for (int x = 0; x <= resX; x++, i++)
            {
                float xCoord = (float)x / resX * Width;
                float zCoord = (float)z / resZ * Length;
                float y = FractalBrownianMotion(xCoord * Scale, zCoord * Scale) * Height;

                _vertices[i] = new Vector3(xCoord, y, zCoord);
            }
        }

        _triangles = new int[resX * resZ * 6];
        int vert = 0;
        int tris = 0;
        for (int z = 0; z < resZ; z++)
        {
            for (int x = 0; x < resX; x++)
            {
                _triangles[tris + 0] = vert + 0;
                _triangles[tris + 1] = vert + resX + 1;
                _triangles[tris + 2] = vert + 1;
                _triangles[tris + 3] = vert + 1;
                _triangles[tris + 4] = vert + resX + 1;
                _triangles[tris + 5] = vert + resX + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }

        _mesh.Clear();
        _mesh.vertices = _vertices;
        _mesh.triangles = _triangles;
        _mesh.RecalculateNormals();
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

    void OnDrawGizmos()
    {
        if (_vertices == null) return;

        Gizmos.color = Color.black;
        foreach (var v in _vertices)
        {
            Gizmos.DrawSphere(transform.position + v, 0.05f);
        }
    }
}

}