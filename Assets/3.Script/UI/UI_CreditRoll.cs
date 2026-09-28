using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class UI_CreditRoll : MonoBehaviour
{
    [Header("크레딧 UI")]
    [SerializeField] private RectTransform creditContent;

    [Header("연출 설정")]
    [SerializeField] private float fadeInDuration = 2f;
    [SerializeField] private float delayBeforeScroll = 1f;
    [SerializeField] private float scrollDuration = 10f;
    [SerializeField] private float targetPosY = 2000f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        PlayCredit();
    }

    private void PlayCredit()
    {
        Sequence creditSeq = DOTween.Sequence();

        creditSeq.Append(canvasGroup.DOFade(1f, fadeInDuration))
                 .AppendInterval(delayBeforeScroll)
                 .Append(creditContent.DOAnchorPosY(targetPosY, scrollDuration).SetEase(Ease.Linear))
                 .OnComplete(() =>
                 {
                     GameManager.Instance.LoadWithLoadingScreen("TitleScene");
                 });
    }
}