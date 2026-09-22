using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeUIPanel : MonoBehaviour
{
    [SerializeField] private GameObject[] Panels;

    public void SwitchPanel(GameObject targetPanel)
    {
        for (int i = 0; i < Panels.Length; i++)
        {
            Panels[i].SetActive(false);
        }
        targetPanel.SetActive(true);
    }
}
