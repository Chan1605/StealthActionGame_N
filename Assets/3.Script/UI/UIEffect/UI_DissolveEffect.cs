using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UI_DissolveEffect : MonoBehaviour
{
    [Header("디졸브 대상 이미지들")]
    [SerializeField] private Graphic[] targetGraphics;

    private MaterialPropertyBlock propBlock;
    private static readonly int CutoffID = Shader.PropertyToID("_Cutoff");

    public void PlayOpenEffect(float duration, Action onComplete)
    {
        gameObject.SetActive(true);

        SetDissolveAmount(0f);

        DOTween.To(() => GetDissolveAmount(), x => SetDissolveAmount(x), 1f, duration)
               .SetUpdate(true)
               .OnComplete(() => {
                   onComplete?.Invoke();
               });
    }

    public void PlayCloseEffect(float duration, Action onComplete)
    {
        DOTween.To(() => GetDissolveAmount(), x => SetDissolveAmount(x), 0f, duration)
               .SetUpdate(true)
               .OnComplete(() => {
                   gameObject.SetActive(false);
                   onComplete?.Invoke();
               });
    }

    private float GetDissolveAmount()
    {
        if (targetGraphics.Length > 0 && targetGraphics[0].material != null)
        {
            return targetGraphics[0].material.GetFloat("_Cutoff");
        }
        return 1f;
    }

    private void SetDissolveAmount(float value)
    {
        for (int i = 0; i < targetGraphics.Length; i++)
        {
            if (targetGraphics[i] != null && targetGraphics[i].material != null)
            {
                targetGraphics[i].material.SetFloat("_Cutoff", value);
            }
        }
    }
}