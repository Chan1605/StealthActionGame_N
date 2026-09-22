using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class EnemyIndicator : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text label;
    [SerializeField] private Color weakColor = new Color(1f, 0.6f, 0f);
    [SerializeField] private Color strongColor = Color.red;
    [SerializeField] private Color detectedColor = Color.red;
    [SerializeField] private float popDuration = 0.2f;
    [SerializeField] private float fadeDuration = 0.15f;
    private Tween _pulseTween;
    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (_cam == null) return;
        transform.forward = _cam.transform.forward;
    }

    public void ShowWeak() => Show("?", weakColor, false);
    public void ShowStrong() => Show("?", strongColor, true);
    public void ShowDetected() => Show("!", detectedColor, true);

    private void Show(string text, Color color, bool pulse)
    {
        label.text = text;
        label.color = color;

        DOTween.Kill(transform);
        DOTween.Kill(canvasGroup);
        _pulseTween?.Kill();

        transform.localScale = Vector3.zero;
        canvasGroup.DOFade(1f, fadeDuration);

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScale(1f, popDuration).SetEase(Ease.OutBack));

        if (pulse)
        {
            seq.AppendCallback(() =>
            {
                _pulseTween = transform.DOScale(1.15f, 0.4f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            });
        }
    }

    public void Hide()
    {
        _pulseTween?.Kill();
        DOTween.Kill(transform);
        DOTween.Kill(canvasGroup);
        canvasGroup.DOFade(0f, fadeDuration);
    }

    private void OnDisable()
    {
        _pulseTween?.Kill();
        DOTween.Kill(transform);
        DOTween.Kill(canvasGroup);
    }
}