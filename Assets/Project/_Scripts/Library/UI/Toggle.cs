using Project._Scripts.Terrain;
using UnityEngine;

namespace Project._Scripts.Library.UI
{
  public class Toggle : MonoBehaviour
  {
    [SerializeField]private ProceduralTerrainGenerator ProceduralTerrainGenerator;

    private UnityEngine.UI.Toggle _toggle;

    private void Start()
    {
      _toggle = GetComponent<UnityEngine.UI.Toggle>();
      SetBoundary();
    }

    public void SetBoundary()
    {
      ProceduralTerrainGenerator.SetBoundary(_toggle.isOn);
      if(_toggle.isOn)ProceduralTerrainGenerator.RegenerateTerrain();
    }
  }
}
