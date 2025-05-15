namespace Project._Scripts.Terrain
{
  using UnityEngine;

  public class TerrainBoundary : MonoBehaviour
  {
    public Vector3 size = new Vector3(5, 5, 5);
    private LineRenderer lineRenderer;

    void Start()
    {
      lineRenderer = gameObject.AddComponent<LineRenderer>();
      lineRenderer.positionCount = 16;
      lineRenderer.loop = false;
      lineRenderer.widthMultiplier = 0.05f;
      lineRenderer.useWorldSpace = false;

      Vector3[] points = new Vector3[]
      {
        // Bottom square
        new Vector3(-size.x, -size.y, -size.z) * 0.5f,
        new Vector3(size.x, -size.y, -size.z) * 0.5f,
        new Vector3(size.x, -size.y, size.z) * 0.5f,
        new Vector3(-size.x, -size.y, size.z) * 0.5f,
        new Vector3(-size.x, -size.y, -size.z) * 0.5f,

        // Vertical lines
        new Vector3(-size.x, size.y, -size.z) * 0.5f,
        new Vector3(size.x, size.y, -size.z) * 0.5f,
        new Vector3(size.x, -size.y, -size.z) * 0.5f,
        new Vector3(size.x, size.y, -size.z) * 0.5f,
        new Vector3(size.x, size.y, size.z) * 0.5f,
        new Vector3(size.x, -size.y, size.z) * 0.5f,
        new Vector3(size.x, size.y, size.z) * 0.5f,
        new Vector3(-size.x, size.y, size.z) * 0.5f,
        new Vector3(-size.x, -size.y, size.z) * 0.5f,
        new Vector3(-size.x, size.y, size.z) * 0.5f,
        new Vector3(-size.x, size.y, -size.z) * 0.5f,
      };

      lineRenderer.SetPositions(points);
    }
  }

}
