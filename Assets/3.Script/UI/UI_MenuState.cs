using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using DG.Tweening;

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
    [SerializeField] private VerticalLayoutGroup pauseLayout;
    [SerializeField] private RectTransform[] pauseButtons;

    [SerializeField] private GameObject optionPanal;
    [SerializeField] private VerticalLayoutGroup optionLayout;
    [SerializeField] private RectTransform[] optionTabButtons;

    [SerializeField] private Text big_Pause_Text;
    [SerializeField] private Text big_Option_Text;

    [SerializeField] private RectTransform bottomBar;

    [Header("ChangeUIPanal 오브젝트")]
    [SerializeField] private ChangeUIPanel optionTabSwitch;

    [Header("뎁스 판정용")]
    private GameObject lastActiveButton;

    [SerializeField] private InputActionAsset inputAsset;

    private void Awake()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.menuState = this;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(pauseLayout.GetComponent<RectTransform>());
        pauseLayout.enabled = false;

        LayoutRebuilder.ForceRebuildLayoutImmediate(optionLayout.GetComponent<RectTransform>());
        optionLayout.enabled = false;

    }

    private void Start()
    {
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
        currentState = MenuState.InPauseMenu;
        pausePanal.SetActive(true);

        //TODO 등장 연출
        Color textColor = big_Pause_Text.color;
        textColor.a = 0f;
        big_Pause_Text.color = textColor;

        big_Pause_Text.DOFade(0.7f, 0.5f).SetUpdate(true);
        bottomBar.anchoredPosition = new Vector2(0, -100f);
        bottomBar.DOAnchorPosY(0f, 0.5f).SetEase(Ease.OutQuint).SetUpdate(true);

        Sequence btnSeq = DOTween.Sequence().SetUpdate(true);
        for (int i = 0; i < pauseButtons.Length; i++)
        {
            CanvasGroup cg = pauseButtons[i].GetComponent<CanvasGroup>();
            cg.alpha = 0f;

            pauseButtons[i].anchoredPosition = new Vector2(-200f, pauseButtons[i].anchoredPosition.y);

            btnSeq.Insert(i * 0.1f, pauseButtons[i].DOAnchorPosX(0f, 0.4f).SetEase(Ease.OutBack));
            btnSeq.Insert(i * 0.1f, cg.DOFade(1f, 0.4f));
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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
        currentState = MenuState.InOptionMenu;
        optionPanal.SetActive(true);

        Sequence sequence = DOTween.Sequence().SetUpdate(true);

        sequence.Insert(0f, big_Pause_Text.DOFade(0f, 0.3f));

        Color optColor = big_Option_Text.color;
        optColor.a = 0f;
        big_Option_Text.color = optColor;
        sequence.Insert(0.2f, big_Option_Text.DOFade(0.7f, 0.3f));

        for (int i = 0; i < pauseButtons.Length; i++)
        {
            CanvasGroup cg = pauseButtons[i].GetComponent<CanvasGroup>();

            pauseButtons[i].anchoredPosition = new Vector2(-200f, pauseButtons[i].anchoredPosition.y);

            sequence.Insert(i * 0.1f, pauseButtons[i].DOAnchorPosX(-200f, 0.4f).SetEase(Ease.InBack));
            sequence.Insert(i * 0.1f, cg.DOFade(0f, 0.4f));
        }

        for (int i = 0; i < optionTabButtons.Length; i++)
        {
            CanvasGroup cg = optionTabButtons[i].GetComponent<CanvasGroup>();

            if (cg !=null)
            {
                cg.alpha = 0f;
                optionTabButtons[i].anchoredPosition = new Vector2(200f, optionTabButtons[i].anchoredPosition.y);

                sequence.Insert(0.3f + (i * 0.1f), optionTabButtons[i].DOAnchorPosX(0f, 0.4f).SetEase(Ease.OutBack));
                sequence.Insert(0.3f + (i * 0.1f), cg.DOFade(1f, 0.4f));
            }
        }

        sequence.OnComplete(() => { pausePanal.SetActive(false); });
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
