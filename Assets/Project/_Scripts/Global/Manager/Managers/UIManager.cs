using System;
using System.Collections.Generic;
using System.Reflection;
using Project._Scripts.Terrain;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Project._Scripts.Global.Manager.Managers
{
  public class UIManager : MonoBehaviour
  {
    public ProceduralTerrainGenerator terrainGenerator;
    
    [Serializable]
    public struct Setting
    {
      public Slider Slider; 
      public TMP_Text ValueText;
      public float MinValue;
      public float MaxValue;
    }

    public List<Setting> Settings;
    public Setting GetSetting(string settingName) => Settings.Find(x => x.Slider.name == settingName);

    [SerializeField]private float debounceDelay = 0.3f; // saniye cinsinden bekleme süresi
    private float debounceTimer = 0f;
    private bool pendingUpdate = false;

    public Dropdown Dropdown;

    public void TestD()
    {
      Debug.Log(Dropdown.value);
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

    public void SetSlider(string settingName)
    {
      Setting setting = GetSetting(settingName);
      var sliderValue = Mathf.Clamp(Mathf.CeilToInt(setting.Slider.value * setting.MaxValue), setting.MinValue, setting.MaxValue);
      setting.ValueText.text = $"{sliderValue}";

      SetTerrainVariable(settingName, sliderValue);

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
}