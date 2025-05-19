using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Project._Scripts.Global.Manager.Core;
using Project._Scripts.Terrain;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class Setting : MonoBehaviour
{
  private ProceduralTerrainGenerator _terrainGenerator;

  public UnityEvent AdditiveEvent;
  private Slider _slider; 
  private TMP_Text _valueText;
  private TMP_Text _headerText;
  public float MinValue;
  public float MaxValue;
  public float Multiplier = 1f;
  
  public enum Type{Integer, Float}
  public Type ValueType;
  
  [SerializeField]private float debounceDelay = 0.3f; // saniye cinsinden bekleme süresi
  private float debounceTimer = 0f;
  private bool pendingUpdate = false;

  private void Start()
  {
    _terrainGenerator = ManagerCore.Instance.GetInstance<ProceduralTerrainGenerator>();
    _slider = GetComponentInChildren<Slider>();
    _valueText = GetComponentsInChildren<TMP_Text>().FirstOrDefault(x => x.name == "ValueText");
    _headerText = GetComponentsInChildren<TMP_Text>().FirstOrDefault(x => x.name == "HeaderText");

    _headerText!.text = gameObject.name;

    GetTerrainVariable(gameObject.name);
    
    _slider.onValueChanged.AddListener(delegate { SetSlider();});
  }

  private void OnDestroy()
  {
    _slider.onValueChanged.RemoveListener(delegate { SetSlider();});
  }

  void Update()
  {
    if (pendingUpdate)
    {
      debounceTimer -= Time.deltaTime;
      if (debounceTimer <= 0f)
      {
        _terrainGenerator.RegenerateTerrain();
        AdditiveEvent?.Invoke();
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
        _valueText.text = $"{sliderValue * Multiplier}";
        break;
      case Type.Float:
        sliderValue = Mathf.Clamp(_slider.value * MaxValue, MinValue, MaxValue);
        _valueText.text = $"{(sliderValue*Multiplier):F2}";
        break;
    }
    
    SetTerrainVariable(gameObject.name, sliderValue * Multiplier);

    debounceTimer = debounceDelay;
    pendingUpdate = true;
  }
  void GetTerrainVariable(string variableName)
  {
    if (_terrainGenerator == null)
    {
      Debug.LogWarning("TerrainGenerator referansı atanmadı.");
      return;
    }

    FieldInfo field = typeof(ProceduralTerrainGenerator).GetField(variableName);
    if (field != null)
    {
      try
      {
        object value = field.GetValue(_terrainGenerator);
        float sliderValue = 0;

        switch ( value )
        {
          case float floatValue:
            sliderValue = floatValue;
            break;
          case int intValue:
            sliderValue = intValue;
            break;
          case double doubleValue:
            sliderValue = (float)doubleValue;
            break;
          default:
            Debug.LogWarning($"'{variableName}' field tipi desteklenmiyor: {field.FieldType}");
            break;
        }
        
        
        switch ( ValueType )
        {
          case Type.Integer:
            _valueText.text = $"{sliderValue}";
            break;
          case Type.Float:
            _valueText.text = $"{sliderValue:F2}";
            break;
        }

        _slider.value = sliderValue / ((MaxValue - MinValue) * Multiplier);
      }
      catch (Exception e)
      {
        Debug.LogWarning($"'{variableName}' için değer alınamadı: {e.Message}");
      }
    }
    else
    {
      Debug.LogWarning($"'{variableName}' adında bir field bulunamadı.");
    }
  }
    
  void SetTerrainVariable(string variableName, object value)
  {
    if (_terrainGenerator == null)
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
        field.SetValue(_terrainGenerator, convertedValue);
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
