using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class UI_OptionSlider : MonoBehaviour
{
    [SerializeField] Slider slider;
    [SerializeField] Text Value_t;

    [System.Serializable]
    public class SliderEvent : UnityEvent<float> { }

    [Header("외부 데이터 연결")]
    [SerializeField] private SliderEvent onDataChanged;


    private void Start()
    {
        if (slider != null)
        {
            slider.onValueChanged.AddListener(UpdateText);
            slider.onValueChanged.AddListener(NotifyDataChanged);
            UpdateText(slider.value);
        }
    }
    public void UpdateText(float value)
    {
        Value_t.text = Mathf.RoundToInt(value * 100f).ToString();
    }

    private void NotifyDataChanged(float value)
    {
        if (onDataChanged != null)
        {
            onDataChanged.Invoke(value);
        }
    }

    private void OnDestroy()
    {
        if(slider != null)
        {
            slider.onValueChanged.RemoveListener(UpdateText);
        }
    }

}
