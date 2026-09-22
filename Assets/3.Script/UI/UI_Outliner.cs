using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


public class UI_Outliner : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private Material outline;

    private void Awake()
    {
        TryGetComponent(out meshRenderer);
        outline = meshRenderer.materials[1];
    }
    public void SetOutLine_On()
    {
        outline.SetInt("_Transprate", 0);
        outline.SetInt("_isHighlight", 1);

        outline.DOFloat(1f, "_Transprate", 2f).SetEase(Ease.OutBack);
    }

    public void SetOutLine_Off()
    {
        outline.DOFloat(0f, "_Transprate", 1f);
        outline.SetInt("_isHighlight", 0);
    }
}
