using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractableObj : MonoBehaviour
{
    // 더이상 사용하지 않습니다. IInteractable 타입으로 바꿔주세요.
    public Action OnLook;
    public Action OnUse;

    public bool isPlayerLook;
    public bool isAct;

    private Object_Data Data;

    private UI_Outliner outLine;
    private UI_ObjKeyPanal keyPanal;

    private HUDManager hudManager;

    private void Awake()
    {
        TryGetComponent(out outLine);
    }

    private void Start()
    {
        hudManager = FindAnyObjectByType<HUDManager>();
        keyPanal = hudManager.GetKeyPanal();

        OnLook += KeyPanal_On;
        KeyPanal_Off();
    }


    private void Update()
    {

        if (!isPlayerLook && isAct)
        {
            KeyPanal_Off();
            isAct = false;
        }
    }

    private void OnDestroy()
    {
        OnLook -= KeyPanal_On;
    }


    private void KeyPanal_On()
    {
        Debug.Log($"outline 할당 상태 : {outLine != null}");
        Debug.Log($"keyPanal 할당 상태 : {keyPanal != null}");

        isPlayerLook = true;
        isAct = true;
        outLine.SetOutLine_On();
        keyPanal.SetPanal_On(transform);
    }

    private void KeyPanal_Off()
    {
        outLine.SetOutLine_Off();
        keyPanal.SetPanal_Off();
    }

    public void Debug_InvokeAction()
    {
        OnLook.Invoke();
    }

    public void Debug_PlayerDontLook()
    {
        isPlayerLook = false;
    }



}
