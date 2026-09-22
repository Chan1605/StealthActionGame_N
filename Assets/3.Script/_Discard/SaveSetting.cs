using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/*
public class SaveSetting : MonoBehaviour
{
    [SerializeField] private InputActionAsset actions;

    public void SaveSettings()
    {
        PlayerPrefs.SetString("Keybinds", actions.SaveBindingOverridesAsJson());
    }

    public void LoadSettings() //DontDestroyOnLoad에 있는 객체에서 호출되어야 함 (싱글톤)
    {
        PlayerPrefs.GetString("Keybinds");
    }

    public void ResetSettings()
    {
        actions.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteAll();
    }
}
 */
