using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using FMOD.Studio;
using UnityEngine;

public class AudioManager : MonoBehaviour
{

    public static AudioManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);

        RuntimeManager.LoadBank("Master.strings", true);
        RuntimeManager.LoadBank("Master", true);
    }

    private EventInstance bgmInstance;
    [SerializeField] private EventReference playerFootstepPath;

    private string currentAreaBank;

    public void PlayOneShot(EventReference soundEvent, Vector3 worldPos)
    {
        RuntimeManager.PlayOneShot(soundEvent, worldPos);
    }

    public void PlayBGM(EventReference bgmEvent)
    {
        if(bgmInstance.isValid())
        {
            bgmInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            bgmInstance.release();
        }

        bgmInstance = RuntimeManager.CreateInstance(bgmEvent);
        bgmInstance.start();
    }

    public void PauseBGM(bool isPaused)
    {
        if (bgmInstance.isValid())
        {
            bgmInstance.setPaused(isPaused);
        }
    }

    public void PlayFootStep(Vector3 worldpos, float surfaceType, float stanceType)
    {
        EventInstance footstepInstance = RuntimeManager.CreateInstance(playerFootstepPath);

        footstepInstance.set3DAttributes(RuntimeUtils.To3DAttributes(worldpos));
        footstepInstance.setParameterByName("Surface", surfaceType);
        footstepInstance.setParameterByName("Stance", stanceType);

        footstepInstance.start();
        footstepInstance.release();
    }

    public void LoadTitleAudioBank()
    {
        RuntimeManager.LoadBank("Title");
    }

    public void LoadGameSceneAudioBank()
    {
        RuntimeManager.UnloadBank("Title");

        RuntimeManager.LoadBank("BGM");
        RuntimeManager.LoadBank("SFX");
        RuntimeManager.LoadBank("Enemy");
    }

    public void LoadEndingBank()
    {
        RuntimeManager.UnloadBank("BGM");
        RuntimeManager.UnloadBank("SFX");
        RuntimeManager.UnloadBank("Enemy");

        RuntimeManager.LoadBank("Ending", true);
    }

    public void LoadAreaBank(string AreaNum)
    {
        if (!string.IsNullOrEmpty(currentAreaBank))
        {
            RuntimeManager.UnloadBank(currentAreaBank);
        }

        RuntimeManager.LoadBank($"Area_{AreaNum}", true);
        currentAreaBank = AreaNum;
    }
}
