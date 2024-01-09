using MobX.Mediator.Values;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace MobX.UI.Mediator
{
    [ExecuteAlways]
    public class FloatValueAssetSlider : MonoBehaviour
    {
        [SerializeField] private LocalizedString displayName;
        [Space]
        [SerializeField] private float minValue;
        [SerializeField] private float maxValue = 1;
        [Space]
        [SerializeField] private ValueAssetRW<float> valueAsset;
        [Space]
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text nameTextField;
        [SerializeField] private TMP_Text valueTextField;

        private void OnEnable()
        {
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = valueAsset.Value;
            slider.onValueChanged.AddListener(OnSliderValueChanged);
            valueTextField.text = valueAsset.Value.ToString(CultureInfo.InvariantCulture);
            displayName.StringChanged += OnLocalizedDisplayNameChanged;
        }

        private void OnDisable()
        {
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
            displayName.StringChanged -= OnLocalizedDisplayNameChanged;
        }

        private void OnSliderValueChanged(float sliderValue)
        {
#if UNITY_EDITOR
            if (Application.isPlaying is false)
            {
                return;
            }
#endif
            valueAsset.Value = sliderValue;
            valueTextField.text = valueAsset.Value.ToString(CultureInfo.InvariantCulture);
        }

        private void OnLocalizedDisplayNameChanged(string value)
        {
            nameTextField.SetText(value);
        }
    }
}