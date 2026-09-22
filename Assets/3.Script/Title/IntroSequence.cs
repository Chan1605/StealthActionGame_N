using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine;

public class IntroSequence : MonoBehaviour
{
    [System.Serializable]
    public class IntroSlide
    {
        public CanvasGroup canvasGroup;
        public float holdDuration = 2f;
    }

    [SerializeField] private IntroSlide[] slides;
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private float skipFadeDuration = 0.25f;
    [SerializeField] private string titleSceneName;

    private bool skipRequested;

    private void Start()
    {
        foreach (var slide in slides)
        {
            slide.canvasGroup.alpha = 0f;
            slide.canvasGroup.gameObject.SetActive(false);
        }
        StartCoroutine(PlaySequence());
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            skipRequested = true;
        }
    }

    private IEnumerator PlaySequence()
    {
        foreach (var slide in slides)
        {
            skipRequested = false; // 슬라이드마다 스킵 신호 초기화

            slide.canvasGroup.gameObject.SetActive(true);
            yield return StartCoroutine(FadeSkippable(slide.canvasGroup, 0f, 1f, fadeDuration));

            if (!skipRequested)
            {
                yield return WaitSkippable(slide.holdDuration);
            }

            float outDuration = skipRequested ? skipFadeDuration : fadeDuration;
            skipRequested = false; 
            yield return StartCoroutine(FadeSkippable(slide.canvasGroup, slide.canvasGroup.alpha, 0f, outDuration));

            slide.canvasGroup.gameObject.SetActive(false);
        }

        SceneTransitionMgr.Instance.LoadScene(titleSceneName);
    }

    private IEnumerator FadeSkippable(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < duration)
        {
            if (skipRequested) break;
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
    
        cg.alpha = to;
    }

    private IEnumerator WaitSkippable(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (skipRequested) yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}