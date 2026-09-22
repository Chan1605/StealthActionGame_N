using System;
using UnityEngine;

public class CarryableObject : MonoBehaviour, IInteractable
{
    public Transform ObjectTransform => transform;

    public Action OnUse { get; set; }
    public Action OnLook { get; set; }
    public event Action OnTargetCompleted;

    [SerializeField] private Transform uiAnchor;

    public bool IsPlayerLook { get; set; }
    public bool isAct;

    private CarriableBody body;
    private CarrySystem _carrySystem;
    private UI_Outliner outLine;
    private UI_ObjKeyPanal keyPanal;
    private HUDManager hudManager;
    [SerializeField] private bool completeOnCarry = true;
    private bool _isCompleted;

    public bool IsInteractable
    {
        get
        {
            if (_carrySystem == null || body == null) return false;
            return _carrySystem.CanCarry(body);
        }
    }

    private void Awake()
    {
        TryGetComponent(out outLine);
        TryGetComponent(out body);
    }

    private void Start()
    {
        hudManager = FindAnyObjectByType<HUDManager>();
        keyPanal = hudManager.GetKeyPanal();
        _carrySystem = FindAnyObjectByType<CarrySystem>();

        if (_carrySystem != null)
        {
            _carrySystem.OnCarryTargetAcquired += HandleTargetAcquired;
            _carrySystem.OnCarryTargetLost += HandleTargetLost;
            if (completeOnCarry) _carrySystem.OnCarryStarted += HandleTargetCompleted;
        }

        KeyPanal_Off();
    }

    private void OnDestroy()
    {
        if (_carrySystem != null)
        {
            _carrySystem.OnCarryTargetAcquired -= HandleTargetAcquired;
            _carrySystem.OnCarryTargetLost -= HandleTargetLost;
            if (completeOnCarry) _carrySystem.OnCarryStarted -= HandleTargetCompleted;
        }
    }

    private void HandleTargetCompleted(CarriableBody target)
    {
        if (target != body) return;
        if (_isCompleted) return;
        _isCompleted = true;
        OnTargetCompleted?.Invoke();
    }

    private void HandleTargetAcquired(CarriableBody target)
    {
        if (target != body) return;
        IsPlayerLook = true;
        isAct = true;
        outLine?.SetOutLine_On();
        keyPanal.SetPanal_On(uiAnchor != null ? uiAnchor : transform);
    }

    private void HandleTargetLost()
    {
        if (!isAct) return;
        KeyPanal_Off();
        isAct = false;
        IsPlayerLook = false;
    }

    private void KeyPanal_Off()
    {
        outLine?.SetOutLine_Off();
        keyPanal.SetPanal_Off();
    }

    public void EnableInteraction() { }
    public void DisableInteraction() { }
}