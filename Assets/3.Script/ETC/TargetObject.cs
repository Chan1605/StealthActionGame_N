using System;
using System.Collections.Generic;
using UnityEngine;

public class TargetObject : MonoBehaviour, IInteractable
{
    public Transform ObjectTransform => transform;

    public Action OnUse { get; set; }
    public Action OnLook { get; set; }
    public event Action OnTargetCompleted;

    public bool IsInteractable => isInteractable;
    private bool isInteractable = false;
    public bool IsPlayerLook { get; set; }

    public bool isAct;

    private UI_Outliner outLine;
    private UI_ObjKeyPanal keyPanal;
    private HUDManager hudManager;

    [SerializeField] private Object_Data objectData;

    private void Awake()
    {
        TryGetComponent(out outLine);
        OnLook += Look;
        OnUse += Use;
    }
    private void Start()
    {
        hudManager = FindAnyObjectByType<HUDManager>();
        keyPanal = hudManager.GetKeyPanal();

        if (outLine != null)
        {
            outLine.SetOutLine_Off();
        }
    }
    private void Update()
    {
        if (!IsPlayerLook && isAct)
        {
            KeyPanal_Off();
            isAct = false;
        }
    }

    public void EnableInteraction()
    {
        isInteractable = true;
    }

    public void DisableInteraction()
    {
        isInteractable = false;
        OnLook = null;
        OnUse = null;
        OnTargetCompleted = null;
    }

    private void Look()
    {
        if (!IsInteractable)
        {
            return;
        }

        KeyPanal_On();

    }

    private void Use()
    {
        if (!IsInteractable)
        {
            return;
        }

        if (objectData != null && !objectData.SoundEvent.IsNull)
        {
            AudioManager.Instance.PlayOneShot(objectData.SoundEvent, transform.position);
        }

        KeyPanal_Off();
        OnTargetCompleted?.Invoke();

    }
    private void KeyPanal_On()
    {
        Debug.Log($"outline 할당 상태 : {outLine != null}");
        Debug.Log($"keyPanal 할당 상태 : {keyPanal != null}");

        if (isAct)
        {
            return;
        }

        IsPlayerLook = true;
        isAct = true;
        outLine.SetOutLine_On();

        if (objectData != null)
        {
            keyPanal.SetPanal_On(transform, objectData);
        }
        else
        {
            keyPanal.SetPanal_On(transform);
        }
    }
    private void KeyPanal_Off()
    {
        outLine.SetOutLine_Off();
        keyPanal.SetPanal_Off();
    }
}
