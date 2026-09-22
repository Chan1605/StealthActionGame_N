using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_OptionSlider : MonoBehaviour
{
    [SerializeField] Slider slider;
    [SerializeField] Text Value_t;

    private void Start()
    {
        slider.onValueChanged.AddListener(UpdateText);
        UpdateText(slider.value);
    }
    public void UpdateText(float value)
    {
        Value_t.text = Mathf.RoundToInt(value).ToString();
    }

    private void OnDestroy()
    {
        if(slider != null)
        {
            slider.onValueChanged.RemoveListener(UpdateText);
        }
    }
}
