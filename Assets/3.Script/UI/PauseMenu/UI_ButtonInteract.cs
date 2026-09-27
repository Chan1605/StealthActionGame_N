using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
public class UI_ButtonInteract : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("UI 요소 연결")]
    [Tooltip("자식으로 추가한 Hover 이미지의 CanvasGroup을 넣으세요.")]
    [SerializeField] private CanvasGroup hoverImageGroup;

    [Tooltip("색상을 변경할 텍스트를 넣으세요.")]
    [SerializeField] private Text buttonText;

    [Header("효과 설정")]
    [SerializeField] private Color hoverTextColor = Color.white;
    private Color originalTextColor;

    private bool isTabLocked = false;


    private void Awake()
    {
        if (buttonText != null)
        {
            originalTextColor = buttonText.color;
        }

        if (hoverImageGroup != null)
        {
            hoverImageGroup.alpha = 0f;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => HoverOn();
    public void OnPointerExit(PointerEventData eventData) => HoverOff();
    public void OnSelect(BaseEventData eventData) => HoverOn();
    public void OnDeselect(BaseEventData eventData) => HoverOff();

    private void HoverOn()
    {
        if (hoverImageGroup != null)
        {
            hoverImageGroup.DOFade(1f, 0.2f).SetUpdate(true); 
        }
        if (buttonText != null)
        {
            buttonText.DOColor(hoverTextColor, 0.2f).SetUpdate(true);
        }
    }

    private void HoverOff(bool forceOff = false)
    {
        if (isTabLocked && !forceOff) return;

        if (hoverImageGroup != null) hoverImageGroup.DOFade(0f, 0.2f).SetUpdate(true);
        if (buttonText != null) buttonText.DOColor(originalTextColor, 0.2f).SetUpdate(true);
    }

    public void SetTabActive(bool isActive)
    {
        isTabLocked = isActive;
        if (isActive) HoverOn();
        else HoverOff(true); 
    }


}