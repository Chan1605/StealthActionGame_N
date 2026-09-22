using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/*
public class MenuManager : MonoBehaviour
{
    [SerializeField] private InputAction keys;
    [SerializeField] private PlayerInput input;
    [SerializeField] private GameObject menu_ob;

    private SaveSetting save;

    private void Awake()
    {
        save = GetComponentInChildren<SaveSetting>(true);
        save.LoadSettings();
    }

    private void OnEnable()
    {
        if (input != null)
        {
            input.OnMenuToggled += MenuToggle;
        }
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.OnMenuToggled -= MenuToggle;
        }
    }

    private void MenuToggle(bool isOn)
    {
        if (isOn)
        {
            OpenMenu();
        }
        else
        {
            CloseMenu(); 
        }
    }

    private void OpenMenu()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        menu_ob.SetActive(true);
    }

    private void CloseMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        menu_ob.SetActive(false);
        save.LoadSettings();
    }
}
 */


