using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UI_ObjKeyPanal : MonoBehaviour
{
    //UI 키 표시용 스크립트 입니다.
    private CanvasGroup canvasGroup;
    private Text Key_t;
    private Text Behaviour_t;
    private Text Caution_t;

    private RectTransform rectTransform;
    private Transform targetTransform;

    private Camera cam;

    private void Awake()
    {
        TryGetComponent(out rectTransform);
        TryGetComponent(out canvasGroup);

        transform.GetChild(2).TryGetComponent(out Key_t);
        transform.GetChild(3).TryGetComponent(out Behaviour_t);
        transform.GetChild(4).TryGetComponent(out Caution_t);

        cam = Camera.main;
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (targetTransform == null)
        {
            return;
        }

        Vector3 screenPos = cam.WorldToScreenPoint(targetTransform.position);

        if(screenPos.z>0)
        {
            rectTransform.position = screenPos;
        }
    }

    public void SetPanal_On(Transform target)
    {
        targetTransform = target;

        canvasGroup.DOKill();
        canvasGroup.alpha = 0f;
        gameObject.SetActive(true);

        canvasGroup.DOFade(1f, 2f).SetEase(Ease.OutBack);
    }

    public void SetPanal_On(Transform target, Object_Data data)
    {
        targetTransform = target;

        if ( data != null)
        {
            if (Key_t != null)
            {
                string actionString = data.ActionName.ToString();
                string boundKey = GameManager.Instance.curPlayer.GetActionKeyName(actionString);

                Key_t.text = $"{boundKey}";
            }

            if (Behaviour_t != null)
            {
                Behaviour_t.text = data.Behaviour;
            }

            if ( Caution_t != null)
            {
                Caution_t.text = data.Caution;
            }
        }

        canvasGroup.DOKill();
        canvasGroup.alpha = 0f;
        gameObject.SetActive(true);

        canvasGroup.DOFade(1f, 1f).SetEase(Ease.OutBack);
    }

    public void SetPanal_Off()
    {
        canvasGroup.DOKill();
        canvasGroup.DOFade(0f, 0.5f).OnComplete(()=>
        gameObject.SetActive(false));
    }

}
