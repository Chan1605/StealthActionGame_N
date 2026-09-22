using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using DG.Tweening;

public class UI_PlayerHealth : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    [SerializeField] private GameObject healthUIContainer;
    [SerializeField] private CanvasGroup healthUI;
    [SerializeField] private Image healthFillImage;

    [Header("볼륨")]
    [SerializeField] private Volume FinchVolume;

    [Header("위험 체력 기준")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float dangerThreshold = 30f;

    [Header("보간 세팅")]
    [SerializeField] private float fadeDuration = 0.5f;

    private bool isUIShowing = false;
    private bool isVolumeShowing = false;

    private void Awake()
    {
        healthUI.alpha = 0f;
        healthUIContainer.SetActive(false);
        if (FinchVolume !=null)
        {
            FinchVolume.weight = 0f;
        }
    }

    public void UpdateHealthUI(float currentHealth)
    {

        if(healthFillImage !=null)
        {
            healthFillImage.fillAmount = currentHealth / maxHealth;
        }

        bool shouldShowUI = currentHealth < maxHealth;
        if(shouldShowUI != isUIShowing)
        {
            isUIShowing = shouldShowUI;
            healthUI.DOKill();

            if (shouldShowUI)
            {
                healthUIContainer.SetActive(true);
                healthUI.DOFade(1f, fadeDuration);
            }
            else
            {
                healthUI.DOFade(0f, fadeDuration).OnComplete(() => healthUIContainer.SetActive(false));
            }
        }

        if (FinchVolume!=null)
        {
            bool shouldShowVolume = currentHealth <= dangerThreshold;
            if (shouldShowVolume != isVolumeShowing)
            {
                isVolumeShowing = shouldShowVolume;

                DOTween.Kill(FinchVolume);

                float targetWeight;

                if (shouldShowVolume)
                {
                    targetWeight = 1f;
                }
                else
                {
                    targetWeight = 0f;
                }

                DOTween.To(
                    () => FinchVolume.weight,
                    x => FinchVolume.weight = x,
                    targetWeight, 
                    fadeDuration
                    ).SetTarget(FinchVolume);
            }
        }
    }

    public void ForceHideDangerVolume(float duration)
    {
        if (FinchVolume == null) return;
        DOTween.Kill(FinchVolume);
        DOTween.To(() => FinchVolume.weight, x => FinchVolume.weight = x, 0f, duration)
            .SetTarget(FinchVolume);
        isVolumeShowing = false;
    }
}
