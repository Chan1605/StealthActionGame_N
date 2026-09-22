using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class UI_MenuState : MonoBehaviour
{
    public enum MenuState
    {
        InPlaying,
        InPauseMenu,
        InOptionMenu,
        InOptionTap
    }

    private MenuState currentState = MenuState.InPlaying;

    [Header("메인 패널")]
    [SerializeField] private GameObject pausePanal;
    [SerializeField] private GameObject optionPanal;

    [Header("ChangeUIPanal 오브젝트")]
    [SerializeField] private ChangeUIPanel optionTabSwitch;

    [Header("뎁스 판정용")]
    private GameObject lastActiveButton;

    [SerializeField] private InputActionAsset inputAsset;

    private void Start()
    {
        GameManager.Instance.menuState = this;
        pausePanal.SetActive(false);
        optionPanal.SetActive(false);
    }
    public void HandleMenuInput()
    {
        switch (currentState)
        {
            case MenuState.InPlaying:
                OpenPauseMenu();
                break;
            case MenuState.InPauseMenu:
                ClosePauseMenu();
                break;
            case MenuState.InOptionMenu:
                CloseOptionMenu();
                break;
            case MenuState.InOptionTap:
                LeaveOptionTab();
                break;
        }
    }
    private void OpenPauseMenu()
    {
        optionPanal.SetActive(false);

        Time.timeScale = 0f;
        pausePanal.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        currentState = MenuState.InPauseMenu;
    }

    private void ClosePauseMenu()
    {
        GameManager.Instance.UpdatePlayerKeyBinds
        (
            inputAsset.SaveBindingOverridesAsJson()
        );

        Time.timeScale = 1f;
        pausePanal.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentState = MenuState.InPlaying;
    }

    private void CloseOptionMenu()
    {
        optionPanal.SetActive(false);
        pausePanal.SetActive(true);

        currentState = MenuState.InPauseMenu;
    }

    private void LeaveOptionTab()
    {
        if (lastActiveButton != null)
        {
            EventSystem.current.SetSelectedGameObject(lastActiveButton);
        }

        currentState = MenuState.InOptionMenu;
    }

    public void OnClickOptionButton()
    {
        pausePanal.SetActive(false);
        optionPanal.SetActive(true);

        currentState = MenuState.InOptionMenu;
    }

    public void OnClickResumeButton()
    {
        ClosePauseMenu();
    }

    public void OnClickMainButton()
    {
        //TODO. 현재 체크포인트 저장 후 타이틀 씬 연결
        Debug.Log("UI_MenuState : 타이틀 구현 전입니다.");
    }

    public void OnClickTabButton(GameObject targetPanel)
    {
        optionTabSwitch.SwitchPanel(targetPanel);
        lastActiveButton = EventSystem.current.currentSelectedGameObject;

        currentState = MenuState.InOptionTap;
    }

}
