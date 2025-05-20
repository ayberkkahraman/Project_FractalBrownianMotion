using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project._Scripts.Library.UI
{
  public class VisualSetting : MonoBehaviour
  {
    #region Components
    private TMP_Text _valueText;
    public Slider Slider;
    #endregion

    #region Fields
    public Material Material;
    public enum Type{MinHeight, MaxHeight, GradientIntensity}
    public Type ValueType;
    public float MinValue;
    public float MaxValue;
    private float _sliderValue;
    #endregion
    
    
    private void Awake()
    {
      _valueText = GetComponentsInChildren<TMP_Text>().FirstOrDefault(x => x.name == "ValueText");

    }

    private void OnEnable()
    {
      Slider.onValueChanged.AddListener(delegate{SetSlider();});
      GetSlider();
    }

    private void OnDisable()
    {
      Slider.onValueChanged.RemoveListener(delegate{SetSlider();});
    }

    public void GetSlider()
    {
      _sliderValue = Material.GetFloat($"_{ValueType}");
      _valueText.text = $"{_sliderValue:F2}";
      Slider.value = _sliderValue / (MaxValue - MinValue);
    }

    public void SetSlider()
    {
      _sliderValue = Mathf.Clamp(Slider.value * MaxValue, MinValue, MaxValue);
      Material.SetFloat($"_{ValueType}", _sliderValue);
      _valueText.text = $"{_sliderValue:F2}";
    }
  }
}
