using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UI_MissionObjective : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Text objectiveText;

    private void Awake()
    {
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    public void Show(string description)
    {
        objectiveText.text = description;
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}