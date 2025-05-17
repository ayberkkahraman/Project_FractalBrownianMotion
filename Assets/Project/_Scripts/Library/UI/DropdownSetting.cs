using Project._Scripts.Global.Manager.Core;
using Project._Scripts.Terrain;
using TMPro;
using UnityEngine;

namespace Project._Scripts.Library.UI
{
  public class DropdownSetting : MonoBehaviour
  {
    private ProceduralTerrainGenerator _terrainGenerator;
    private TMP_Dropdown _dropdown;

    public Material LitMaterial;
    public Material UnlitMaterial;

    public GameObject LitTab;
    public GameObject UnlitTab;
    
    public Material CurrentMaterial { get; set; }

    private void Awake()
    {
      CurrentMaterial = UnlitMaterial;
    }
    private void Start()
    {
      _terrainGenerator = ManagerCore.Instance.GetInstance<ProceduralTerrainGenerator>();
      _dropdown = GetComponent<TMP_Dropdown>();
      
      _dropdown.onValueChanged.AddListener(delegate{ChangeMaterial();});
    }

    private void OnDestroy()
    {
      _dropdown.onValueChanged.RemoveListener(delegate{ChangeMaterial();});
    }

    public void ChangeMaterial()
    {
      switch ( _dropdown.value )
      {
        case 0:
          CurrentMaterial = UnlitMaterial;
          LitTab.SetActive(false);
          UnlitTab.SetActive(true);
          break;
        case 1:
          CurrentMaterial = LitMaterial;
          UnlitTab.SetActive(false);
          LitTab.SetActive(true);
          break;
        default:
          CurrentMaterial = CurrentMaterial;
          break;
      }
      _terrainGenerator.TerrainMaterial = CurrentMaterial;
      _terrainGenerator.RegenerateTerrain();
    }
  }
}
