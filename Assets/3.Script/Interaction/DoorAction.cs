using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorAction : InteractionAction
{
    [Header("Door")]
    [SerializeField] private Transform door;
    [SerializeField] private Transform hingePoint;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float swingDuration = 0.9f;

    [Header("Close Animation")]
    [SerializeField] private string closeTrigger;
    [SerializeField] private float closeActionDelay = -1f;
    [SerializeField] private float closeDuration = -1f;

    [Header("Lock")]
    [SerializeField] private bool isLockable = false;
    [SerializeField] private string requiredKeyId;
    [SerializeField] private bool isUnlockedDuringFreeTime = true;
    [SerializeField] private bool isUnlockedPermanently = true;
    [SerializeField] private bool isClosedWhenRelocked = true;

    [Header("Rule")]
    [SerializeField] private bool isSwingAwayFromUser = false;
    [SerializeField] private bool isCloseAllowed = true;

    [Header("Debug")]
    [SerializeField] private bool isDebugLog = true;

    private Vector3 _closedPosition;
    private Quaternion _closedRotation;
    private Vector3 _hingePosition;
    private Vector3 _probeOffset;
    private float _currentAngle;
    private Coroutine _swingRoutine;
    private bool _isUnlocked;

    public bool isOpen { get; private set; }

    public bool isLockedNow
    {
        get
        {
            if (!isLockable || _isUnlocked)
            {
                return false;
            }

            if (isUnlockedDuringFreeTime
                && PrisonScheduleManager.Instance != null
                && PrisonScheduleManager.Instance.IsFreeTime)
            {
                return false;
            }

            return true;
        }
    }

    public string keyId
    {
        get
        {
            return requiredKeyId;
        }
    }

    public event Action<bool> OnDoorStateChanged;
    public event Action<DoorAction> OnLockedAttempt;
    public event Action<DoorAction> OnUnlocked;

    public override string animationTriggerName
    {
        get
        {
            if (isOpen && !string.IsNullOrEmpty(closeTrigger))
            {
                return closeTrigger;
            }

            return base.animationTriggerName;
        }
    }

    public override float actionDelayTime
    {
        get
        {
            if (isOpen && closeActionDelay >= 0f)
            {
                return closeActionDelay;
            }

            return base.actionDelayTime;
        }
    }

    public override float durationTime
    {
        get
        {
            if (isOpen && closeDuration >= 0f)
            {
                return closeDuration;
            }

            return base.durationTime;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        if (door == null)
        {
            door = transform;
        }

        _closedPosition = door.position;
        _closedRotation = door.rotation;
        _hingePosition = hingePoint != null ? hingePoint.position : door.position;

        _probeOffset = _closedPosition - _hingePosition;

        if (_probeOffset.sqrMagnitude < 0.0001f)
        {
            _probeOffset = _closedRotation * Vector3.right;
        }
    }

    private void Start()
    {
        if (PrisonScheduleManager.Instance != null)
        {
            PrisonScheduleManager.Instance.OnScheduleChanged += HandleScheduleChanged;
        }
    }

    private void OnDestroy()
    {
        if (PrisonScheduleManager.Instance != null)
        {
            PrisonScheduleManager.Instance.OnScheduleChanged -= HandleScheduleChanged;
        }
    }

    private void HandleScheduleChanged(bool isFreeTime)
    {
        if (!isClosedWhenRelocked || !isLockedNow || !isOpen)
        {
            return;
        }

        Log("일과가 끝나 문이 닫히고 잠깁니다.");
        SetOpen(false, null);
    }

    public bool HasRequiredKey(Transform user)
    {
        if (string.IsNullOrEmpty(requiredKeyId))
        {
            return false;
        }

        if (user == null)
        {
            return false;
        }

        KeyInventory inventory = user.GetComponentInChildren<KeyInventory>();

        return inventory != null && inventory.HasKey(requiredKeyId);
    }

    protected override bool CanExecute(Transform user)
    {
        if (isOpen && !isCloseAllowed)
        {
            return false;
        }

        return true;
    }

    protected override void OnExecute(Transform user)
    {
        if (isLockedNow)
        {
            if (!HasRequiredKey(user))
            {
                Log($"잠겨 있습니다. '{requiredKeyId}' 열쇠가 필요합니다.");
                OnLockedAttempt?.Invoke(this);
                return;
            }

            if (isUnlockedPermanently)
            {
                _isUnlocked = true;
            }

            Log($"'{requiredKeyId}' 열쇠로 잠금을 풀었습니다.");
            OnUnlocked?.Invoke(this);
        }

        SetOpen(!isOpen, user);
    }

    private void SetOpen(bool isOn, Transform user)
    {
        isOpen = isOn;

        float target = isOn ? GetSignedAngle(user) : 0f;

        if (_swingRoutine != null)
        {
            StopCoroutine(_swingRoutine);
        }

        _swingRoutine = StartCoroutine(Swing_co(target));
        OnDoorStateChanged?.Invoke(isOn);
    }

    private float GetSignedAngle(Transform user)
    {
        if (!isSwingAwayFromUser || user == null)
        {
            return openAngle;
        }

        Vector3 plusEnd = _hingePosition + Quaternion.AngleAxis(openAngle, Vector3.up) * _probeOffset;
        Vector3 minusEnd = _hingePosition + Quaternion.AngleAxis(-openAngle, Vector3.up) * _probeOffset;

        float plusDistance = (plusEnd - user.position).sqrMagnitude;
        float minusDistance = (minusEnd - user.position).sqrMagnitude;

        return plusDistance >= minusDistance ? openAngle : -openAngle;
    }

    private IEnumerator Swing_co(float targetAngle)
    {
        float start = _currentAngle;
        float elapsed = 0f;

        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, elapsed / swingDuration);
            ApplyAngle(Mathf.Lerp(start, targetAngle, k));
            yield return null;
        }

        ApplyAngle(targetAngle);
        _swingRoutine = null;
    }

    private void ApplyAngle(float angle)
    {
        _currentAngle = angle;

        Quaternion spin = Quaternion.AngleAxis(angle, Vector3.up);
        door.SetPositionAndRotation(_hingePosition + spin * (_closedPosition - _hingePosition), spin * _closedRotation);
    }

    private void Log(string message)
    {
        if (isDebugLog)
        {
            Debug.Log($"[Door] {name} - {message}", this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 pivot = hingePoint != null ? hingePoint.position : (door != null ? door.position : transform.position);

        Gizmos.color = new Color(0.1f, 0.8f, 0.4f, 0.9f);
        Gizmos.DrawSphere(pivot, 0.06f);
        Gizmos.DrawLine(pivot + Vector3.down * 1f, pivot + Vector3.up * 1f);

        if (!isLockable)
        {
            return;
        }

        Gizmos.color = Application.isPlaying && !isLockedNow
            ? new Color(0.2f, 1f, 0.2f, 0.9f)
            : new Color(1f, 0.3f, 0.1f, 0.9f);

        Gizmos.DrawWireCube(pivot + Vector3.up * 1.2f, Vector3.one * 0.18f);
    }
}
