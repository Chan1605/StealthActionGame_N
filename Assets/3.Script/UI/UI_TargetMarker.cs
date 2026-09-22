using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_TargetMarker : MonoBehaviour
{
    private CanvasGroup canvasGroup;

    private Image Target_img;
    private Text TargetDistance_t;

    private RectTransform rectTransform;

    private Transform playerTransform;
    private Transform targetTransform;

    private Camera cam;

    private void Awake()
    {
        TryGetComponent(out rectTransform);
        TryGetComponent(out canvasGroup);

        transform.GetChild(1).TryGetComponent(out TargetDistance_t);
        transform.GetChild(2).TryGetComponent(out Target_img);

        cam = Camera.main;
        gameObject.SetActive(false);
    }
    private void Start()
    {
        GameObject.FindWithTag("Player").TryGetComponent(out playerTransform);
    }
    private void LateUpdate()
    {
        if (targetTransform == null || playerTransform == null)
        {
            return;
        }

        Vector3 targetPos = targetTransform.position + new Vector3(0, 1f, 0);
        Vector3 screenPos = cam.WorldToScreenPoint(targetPos);

        if (screenPos.z > 0)
        {
            if (canvasGroup.alpha == 0f)
            {
                canvasGroup.alpha = 1f;
            }
            rectTransform.position = new Vector3(screenPos.x, screenPos.y, 0f) ;
        }
        else
        {
            if (canvasGroup.alpha == 1f)
            {
                canvasGroup.alpha = 0f;
            }
        }

        float distance = Vector3.Distance(playerTransform.position, targetTransform.position);
        TargetDistance_t.text = $"{ Mathf.RoundToInt(distance)}m";
    }

    public void SetMarker_On(Transform target)
    {
        targetTransform = target;
        gameObject.SetActive(true);
    }

    public void SetMarker_Off()
    {
        targetTransform = null;
        gameObject.SetActive(false);
    }
}
