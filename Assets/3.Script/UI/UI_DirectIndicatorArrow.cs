using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_DirectIndicatorArrow : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image arrowImage;

    public void UpdateIndicator(float angle)
    {
        rectTransform.localEulerAngles = new Vector3(0, 0, angle);
    }

    public void SetActive(bool isActive)
    {
        gameObject.SetActive(isActive);
    }
}
