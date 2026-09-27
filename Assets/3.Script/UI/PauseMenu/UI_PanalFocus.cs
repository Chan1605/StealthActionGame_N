using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_PanelFocus : MonoBehaviour
{
    [SerializeField] private Image outlineImage; 

    private void Update()
    {
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        bool isFocused = currentSelected != null && currentSelected.transform.IsChildOf(this.transform);

        if (outlineImage != null)
        {
            outlineImage.enabled = isFocused;
        }
    }
}