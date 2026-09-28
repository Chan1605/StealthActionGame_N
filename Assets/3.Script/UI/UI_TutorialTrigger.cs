using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    public enum TriggerCondition
    {
        OnLook,
        OnUse,
        OnComplete,
        OnTriggerEnter
    }
    [Header("발동 조건")]

    [Tooltip("이 팝업이 언제 뜰지 선택하세요.")]
    [SerializeField] private TriggerCondition condition;

    [Header("튜토리얼 데이터")]
    [SerializeField] private Sprite tutorialImage;                 
    [TextArea(3, 5)]
    [SerializeField] private string tutorialText;

    private UI_TutorialPopup cachedPopup;
    private IInteractable interactableTarget;
    private bool hasTriggered = false;

    private void Start()
    {
        cachedPopup = FindAnyObjectByType<UI_TutorialPopup>(FindObjectsInactive.Include);
        interactableTarget = GetComponent<IInteractable>();

        if (interactableTarget != null)
        {
            if (condition == TriggerCondition.OnLook)
                interactableTarget.OnLook += FireTutorial;
            else if (condition == TriggerCondition.OnUse)
                interactableTarget.OnUse += FireTutorial;
            else if (condition == TriggerCondition.OnComplete)
                interactableTarget.OnTargetCompleted += FireTutorial;
        }
        else if (condition != TriggerCondition.OnTriggerEnter)
        {
            Debug.LogWarning($"[TutorialTrigger] {gameObject.name}에 IInteractable이 없는데 상호작용 조건이 설정되어 있습니다.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (condition == TriggerCondition.OnTriggerEnter)
        {
            if (other.CompareTag("Player"))
            {
                FireTutorial();
            }
        }
    }

    private void FireTutorial()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        if (cachedPopup != null)
        {
            cachedPopup.Setup(tutorialImage, tutorialText);
            cachedPopup.gameObject.SetActive(true);
            cachedPopup.OpenTutorial();
        }
    }

    private void OnDestroy()
    {
        if (interactableTarget != null)
        {
            if (condition == TriggerCondition.OnLook)
                interactableTarget.OnLook -= FireTutorial;
            else if (condition == TriggerCondition.OnUse)
                interactableTarget.OnUse -= FireTutorial;
            else if (condition == TriggerCondition.OnComplete)
                interactableTarget.OnTargetCompleted -= FireTutorial;
        }
    }
}