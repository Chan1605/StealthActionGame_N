using UnityEngine;
using FMODUnity;

public class SceneAudioStarter : MonoBehaviour
{
    [SerializeField] private string[] banksToLoad;

    [SerializeField] private EventReference bgmEvent;
    [SerializeField] private EventReference ambEvent;

    private void Start()
    {
        if (AudioManager.Instance != null)
        {


            AudioManager.Instance.PlayBGM(bgmEvent);
            AudioManager.Instance.PlayAMB(ambEvent);
        }
        else
        {
            Debug.LogWarning("AudioManager가 없어 음악을 재생할 수 없습니다.");
        }
    }
}