using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/*
public class SoundSetting : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Slider masterSlider, bgmSlider, sfxSlider;

    private void Start()
    {
        masterSlider.value = PlayerPrefs.GetFloat("Master", 100f);
        bgmSlider.value = PlayerPrefs.GetFloat("BGM", 100f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFX", 100f);

        masterSlider.onValueChanged.AddListener(v => SetVolume("Master", v));
        bgmSlider.onValueChanged.AddListener(v => SetVolume("BGM", v));
        sfxSlider.onValueChanged.AddListener(v => SetVolume("SFX", v));
    }

    private void SetVolume(string param, float sliderValue)
    {
        float normalized = sliderValue / 100f;
        float db = Mathf.Log10(Mathf.Max(normalized, 0.0001f)) * 20f;
        mixer.SetFloat(param, db);
        PlayerPrefs.SetFloat(param, sliderValue);
    }
}
 */
