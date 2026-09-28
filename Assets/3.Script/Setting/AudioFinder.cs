using UnityEngine;
using UnityEngine.UI;

public class AudioFinder : MonoBehaviour
{
    public enum AudioType { Master, BGM, SFX, AMB }

    [Header("이 슬라이더가 조절할 오디오 타입")]
    public AudioType audioType;

    private void Start()
    {
        Slider slider = GetComponentInChildren<Slider>();

        if (slider != null && AudioManager.Instance != null)
        {
            switch (audioType)
            {
                case AudioType.Master:
                    slider.onValueChanged.AddListener(AudioManager.Instance.SetMasterVolume);
                    break;
                case AudioType.BGM:
                    slider.onValueChanged.AddListener(AudioManager.Instance.SetBGMVolume);
                    break;
                case AudioType.SFX:
                    slider.onValueChanged.AddListener(AudioManager.Instance.SetSFXVolume);
                    break;
                case AudioType.AMB:
                    slider.onValueChanged.AddListener(AudioManager.Instance.SetAMBVolume);
                    break;
            }
        }
    }
}