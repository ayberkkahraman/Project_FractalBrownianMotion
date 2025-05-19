using System;
using Project._Scripts.Terrain;
using UnityEngine;
using UnityEngine.UI;

namespace Project._Scripts.Library.UI
{
  public class Toggle : MonoBehaviour
  {
    [SerializeField]private ProceduralTerrainGenerator _proceduralTerrainGenerator;

    private UnityEngine.UI.Toggle _toggle;

    private void Start()
    {
      _toggle = GetComponent<UnityEngine.UI.Toggle>();
      SetBoundary();
    }

    public void SetBoundary()
    {
      _proceduralTerrainGenerator.SetBoundary(_toggle.isOn);
    }
  }
}
