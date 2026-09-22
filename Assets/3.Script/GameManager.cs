using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
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
    }

    public PauseAndInputSetting curPlayer;
    public UI_MenuState menuState;

    public string next_scene;

    public void LoadWithLoadingScreen(string target_scene)
    {
        next_scene = target_scene;
        SceneManager.LoadScene("LoadingScene");
    }

    public void HandleMenuInput()
    {
        menuState.HandleMenuInput();
    }

    public void UpdatePlayerKeyBinds(string bindData)
    {
        if(curPlayer != null)
        {
            curPlayer.ApplyKeybinds(bindData);
        }
    }
}
