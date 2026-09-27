using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class UI_AutoScroll : MonoBehaviour
{
    private ScrollRect scrollRect;
    private GameObject lastSelected;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    private void Update()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected == null)
        {
            return;
        }

        if (selected == lastSelected)
        {
            return;
        }

        if (selected.transform.IsChildOf(scrollRect.content) == true)
        {
            lastSelected = selected;
            ScrollToTarget(selected.GetComponent<RectTransform>());
        }
    }

    private void ScrollToTarget(RectTransform target)
    {
        Canvas.ForceUpdateCanvases();

        RectTransform viewport = scrollRect.viewport;
        if (viewport == null)
        {
            viewport = scrollRect.GetComponent<RectTransform>();
        }

        float contentHeight = scrollRect.content.rect.height - viewport.rect.height;
        float normalizedY = 0f;

        if (contentHeight > 0f)
        {
            Vector2 targetLocalPos = scrollRect.content.InverseTransformPoint(target.position);

            float targetY = Mathf.Abs(targetLocalPos.y);

            float offset = viewport.rect.height * 0.5f;
            float centeredY = targetY - offset;

            normalizedY = 1f - (centeredY / contentHeight);
        }

        if (normalizedY < 0f)
        {
            normalizedY = 0f;
        }

        if (normalizedY > 1f)
        {
            normalizedY = 1f;
        }

        scrollRect.verticalNormalizedPosition = normalizedY;
    }
}