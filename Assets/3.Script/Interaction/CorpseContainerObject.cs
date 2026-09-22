using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorpseContainerObject : MonoBehaviour, IInteractable, ICompletionState
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

    private bool _isTracking;
    private bool _isCompleted;

    //  미션 차례 전에 숨겼어도 기록은 남는다
    public bool IsCompleted
    {
        get { return _isCompleted; }
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

        if (_container != null)
        {
            _container.OnCorpseStored += HandleCorpseStored;   // 추가: 실제 완료 이벤트 구독
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
        _isTracking = true;
    }

    public void DisableInteraction()
    {
        _isTracking = false;
        SetPanelActive(false);
    }

    private void OnDestroy()
    {
        if (_container != null)
        {
            _container.OnCorpseStored -= HandleCorpseStored;
        }
    }

    private void HandleCorpseStored(CarriableBody body)
    {
        if (_isCompleted) return;

        _isCompleted = true;   

        if (_isTracking)       // 차례일 때만 StageManager에 알린다
        {
            OnTargetCompleted?.Invoke();
        }
    }
}
