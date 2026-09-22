using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralObject : MonoBehaviour, IInteractable, ICompletionState
{
    public Transform ObjectTransform => transform;

    public Action OnUse { get; set; }
    public Action OnLook { get; set; }

    public event Action OnTargetCompleted;


    public bool IsPlayerLook { get; set; }
    public bool isAct;

    private Object_Data Data;

    private UI_Outliner outLine;
    private UI_ObjKeyPanal keyPanal;

    private HUDManager hudManager;
    [SerializeField] private bool completeOnUse = true;
    private bool _isCompleted;

    private bool _isTracking;

    // ICompletionState: 미션 차례 전에 사용됐더라도 "완료됨"으로 기록된다.
    public bool IsCompleted
    {
        get { return _isCompleted; }
    }
    [SerializeField] private bool lockUntilMissionTurn = false; //미션 연동

    public bool IsInteractable
    {
        get
        {
            // 잠금 옵션이 켜져 있고, 아직 미션 차례가 아니면 상호작용 불가
            if (lockUntilMissionTurn && !_isTracking)
            {
                return false;
            }

            return true;
        }
    }


    private void Awake()
    {
        TryGetComponent(out outLine);
    }

    private void Start()
    {
        hudManager = FindAnyObjectByType<HUDManager>();
        keyPanal = hudManager.GetKeyPanal();

        OnLook += Look;
        if (completeOnUse) OnUse += HandleUsed;
        KeyPanal_Off();
    }

    private void Update()
    {
        if (!IsPlayerLook && isAct)
        {
            KeyPanal_Off();
            isAct = false;
        }
    }

    private void OnDestroy()
    {
        OnLook -= Look;
        if (completeOnUse) OnUse -= HandleUsed;
    }

    private void Look()
    {
        KeyPanal_On();
    }

    private void KeyPanal_On()
    {
        Debug.Log($"outline 할당 상태 : {outLine != null}");
        Debug.Log($"keyPanal 할당 상태 : {keyPanal != null}");

        IsPlayerLook = true;
        isAct = true;
        outLine.SetOutLine_On();
        keyPanal.SetPanal_On(transform);
    }

    public void KeyPanal_Off()
    {
        outLine.SetOutLine_Off();
        keyPanal.SetPanal_Off();
    }
    private void HandleUsed()
    {
        if (_isCompleted) return;

        _isCompleted = true;   // 미션 차례 전에 써도 "했다"는 기록은 남긴다

        if (_isTracking)       // 차례일 때만 StageManager에 알린다
        {
            OnTargetCompleted?.Invoke();
        }
    }

    public void Debug_InvokeAction()
    {
        OnLook.Invoke();
    }

    public void Debug_PlayerDontLook()
    {
        IsPlayerLook = false;
    }
    public void EnableInteraction()
    {
        _isTracking = true;
    }

    public void DisableInteraction()
    {
        _isTracking = false;
        Debug.Log("오브젝트의 IInteractable 스크립트가 상시 개체용입니다. 리스트를 확인해주세요.");
    }



}
