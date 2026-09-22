using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorpseContainerObject : MonoBehaviour, IInteractable
{
    [Header("Debug")]
    [SerializeField] private bool isDebugLog = false;

    private CorpseContainerAction _container;
    private CarrySystem _carry;
    private PlayerInteractor _interactor;
    private UI_Outliner _outLine;
    private UI_ObjKeyPanal _keyPanal;
    private HUDManager _hudManager;

    private bool _isPanelOn;

    public Transform ObjectTransform
    {
        get
        {
            return transform;
        }
    }

    public Action OnUse { get; set; }
    public Action OnLook { get; set; }

    public event Action OnTargetCompleted;

    public bool IsPlayerLook { get; set; }

    public bool IsInteractable
    {
        get
        {
            if (_carry == null || !_carry.isCarrying)
            {
                return false;
            }

            if (_container != null && _container.isFull)
            {
                return false;
            }

            return true;
        }
    }

    private void Awake()
    {
        TryGetComponent(out _outLine);
        TryGetComponent(out _container);

        if (_outLine == null)
        {
            Debug.LogWarning($"[CorpseContainer] '{name}'에 UI_Outliner가 없습니다. MeshRenderer와 머티리얼 슬롯 2개가 필요합니다.", this);
        }
    }

    private void Start()
    {
        _hudManager = FindAnyObjectByType<HUDManager>();
        _carry = FindAnyObjectByType<CarrySystem>();
        _interactor = FindAnyObjectByType<PlayerInteractor>();

        if (_hudManager != null)
        {
            _keyPanal = _hudManager.GetKeyPanal();
        }

        if (_carry == null)
        {
            Debug.LogWarning($"[CorpseContainer] 씬에서 CarrySystem을 찾지 못했습니다.", this);
        }

        if (_interactor == null)
        {
            Debug.LogWarning($"[CorpseContainer] 씬에서 PlayerInteractor를 찾지 못했습니다.", this);
        }

        SetPanelActive(false);
    }

    private void Update()
    {
        bool isLooked = _interactor != null && ReferenceEquals(_interactor.CurrentTarget, this);
        bool isWanted = isLooked && IsInteractable;

        if (isWanted == _isPanelOn)
        {
            return;
        }

        SetPanelActive(isWanted);
    }

    private void SetPanelActive(bool isOn)
    {
        _isPanelOn = isOn;
        IsPlayerLook = isOn;

        if (_outLine != null)
        {
            if (isOn)
            {
                _outLine.SetOutLine_On();
            }
            else
            {
                _outLine.SetOutLine_Off();
            }
        }

        if (_keyPanal != null)
        {
            if (isOn)
            {
                _keyPanal.SetPanal_On(transform);
            }
            else
            {
                _keyPanal.SetPanal_Off();
            }
        }

        if (isDebugLog)
        {
            Debug.Log($"[CorpseContainer] {name} 패널 {(isOn ? "표시" : "숨김")}", this);
        }
    }

    public void EnableInteraction()
    {
    }

    public void DisableInteraction()
    {
        SetPanelActive(false);
    }
}
