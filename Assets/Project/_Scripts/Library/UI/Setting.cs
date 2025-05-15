using System;
using System.Linq;
using System.Reflection;
using Project._Scripts.Global.Manager.Core;
using Project._Scripts.Terrain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class Setting : MonoBehaviour
{
  public ProceduralTerrainGenerator terrainGenerator;
  
  private Slider _slider; 
  private TMP_Text _valueText;
  private TMP_Text _headerText;
  public float MinValue;
  public float MaxValue;
  
  public enum Type{Integer, Float}
  public Type ValueType;
  
  [SerializeField]private float debounceDelay = 0.3f; // saniye cinsinden bekleme süresi
  private float debounceTimer = 0f;
  private bool pendingUpdate = false;

  private void Start()
  {
    terrainGenerator = ManagerCore.Instance.GetInstance<ProceduralTerrainGenerator>();
    _slider = GetComponentInChildren<Slider>();
    _valueText = GetComponentsInChildren<TMP_Text>().FirstOrDefault(x => x.name == "ValueText");
    _headerText = GetComponentsInChildren<TMP_Text>().FirstOrDefault(x => x.name == "HeaderText");

    _headerText.text = gameObject.name;
    
    _slider.onValueChanged.AddListener(delegate { SetSlider();});
  }

  void Update()
  {
    if (pendingUpdate)
    {
      debounceTimer -= Time.deltaTime;
      if (debounceTimer <= 0f)
      {
        terrainGenerator.RegenerateTerrain();
        pendingUpdate = false;
      }
    }
  }
  
  public void SetSlider()
  {
    float sliderValue = 0;
    switch ( ValueType )
    {
      case Type.Integer:
        sliderValue = Mathf.Clamp(Mathf.CeilToInt(_slider.value * MaxValue), MinValue, MaxValue);
        _valueText.text = $"{sliderValue}";
        break;
      case Type.Float:
        sliderValue = Mathf.Clamp(_slider.value * MaxValue, MinValue, MaxValue);
        _valueText.text = $"{sliderValue:F1}";
        break;
    }
    
    SetTerrainVariable(gameObject.name, sliderValue);

    debounceTimer = debounceDelay;
    pendingUpdate = true;
  }
    
  void SetTerrainVariable(string variableName, object value)
  {
    if (terrainGenerator == null)
    {
      Debug.LogWarning("TerrainGenerator referansı atanmadı.");
      return;
    }

    FieldInfo field = typeof(ProceduralTerrainGenerator).GetField(variableName);
    if (field != null)
    {
      try
      {
        var convertedValue = Convert.ChangeType(value, field.FieldType);
        field.SetValue(terrainGenerator, convertedValue);
      }
      catch (Exception e)
      {
        Debug.LogWarning($"'{variableName}' için değer atanamadı: {e.Message}");
      }
    }
    else
    {
      Debug.LogWarning($"'{variableName}' adında bir field bulunamadı.");
    }
  }
}
