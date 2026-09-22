using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_MinimapIcon : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private Image icon_img;
    [SerializeField] private GameObject upArrow;
    [SerializeField] private GameObject downArrow;

    [SerializeField] private float heightThreshold = 2.0f;

    public void UpdateIcon(Vector2 clampedPos, float playerY, float enemyY)
    {
        rectTransform.anchoredPosition = clampedPos;

        float yDiff = enemyY - playerY;

        if (yDiff > heightThreshold)
        {
            upArrow.SetActive(true);
            downArrow.SetActive(false);
        }
        else if (yDiff < -heightThreshold)
        {
            upArrow.SetActive(false);
            downArrow.SetActive(true);
        }
        else
        {
            upArrow.SetActive(false);
            downArrow.SetActive(false);
        }
    }

    public void SetIconColor(Color stateColor)
    {
        icon_img.color = stateColor;
    }

}
