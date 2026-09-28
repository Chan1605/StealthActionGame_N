using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_TutorialPopup : MonoBehaviour
{
    [Header("구성 요소")]
    [SerializeField] private UI_DissolveEffect dissolveEffect;
    [SerializeField] private Button closeButton;

    [Header("UI 연결")]
    [SerializeField] private Image popupImage;
    [SerializeField] private Text popupText;  

    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseTutorial);
        }
    }
    public void Setup(Sprite img, string text)
    {
        if (popupImage != null && img != null)
        {
            popupImage.sprite = img;
        }

        if (popupText != null)
        {
            popupText.text = text;
        }
    }

    public void OpenTutorial()
    {
        Time.timeScale = 0f;

        if (GameManager.Instance != null && GameManager.Instance.curPlayer != null)
        {
            GameManager.Instance.curPlayer.SwitchInputMode(true);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (dissolveEffect != null)
        {
            dissolveEffect.PlayOpenEffect(0.5f, () => {
                if (closeButton != null)
                {
                    closeButton.Select();
                }
            });
        }
    }

    public void CloseTutorial()
    {
        if (dissolveEffect != null)
        {
            dissolveEffect.PlayCloseEffect(0.4f, () =>
            {
                Time.timeScale = 1f;

                if (GameManager.Instance != null && GameManager.Instance.curPlayer != null)
                {
                    GameManager.Instance.curPlayer.SwitchInputMode(false);
                }

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                gameObject.SetActive(false);
            });
        }
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseTutorial);
        }
    }
}