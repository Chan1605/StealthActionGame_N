using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class AssassinationSystem : MonoBehaviour
{
    [Header("Detect")]
    [SerializeField] private float range = 1.4f;
    [SerializeField] private float backAngle = 100f;
    [SerializeField] private LayerMask npcLayer;

    [Header("Snap")]
    [SerializeField] private float snapDistance = 0.55f;
    [SerializeField] private float snapTime = 0.15f;
    [SerializeField] private bool isRootLockEnabled = true;

    [Header("Sequence")]
    [SerializeField] private GameObject killCamera;
    [SerializeField] private float clipLength = 2.3f;
    [SerializeField] private float victimDelay = 0.4f;
    [SerializeField] private float ragdollAfterVictimStart = 0.9f;

    [Header("Debug")]
    [SerializeField] private bool isDebugLog = true;

    private CharacterController _controller;
    private PlayerInput _input;
    private PlayerController _movement;
    private PlayerCameraRig _cameraRig;
    private PlayerAnimator _playerAnimator;

    private Vector3 _lockedPosition;
    private Quaternion _lockedRotation;
    private float _sequenceStartTime;
    private bool _isRootLocked;
    private bool _isPrevRootMotion;

    public bool isBusy { get; private set; }

    public event Action<TakedownVictim> OnTakedownStarted;
    public event Action<TakedownVictim> OnTakedownComplete;
    public event Action<TakedownVictim> OnTargetAcquired;
    public event Action OnTargetLost;

    private TakedownVictim _lastNotifiedVictim;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<PlayerInput>();
        _movement = GetComponent<PlayerController>();
        _cameraRig = GetComponent<PlayerCameraRig>();
        _playerAnimator = GetComponent<PlayerAnimator>();

        if (killCamera != null)
        {
            killCamera.SetActive(false);
        }
    }

    private void OnEnable()
    {
        _input.OnInteractPressed += HandleInteractPressed;
    }

    private void OnDisable()
    {
        _input.OnInteractPressed -= HandleInteractPressed;
    }

    private void Update()
    {
        if (!isBusy)
        {
            TakedownVictim current = FindVictim();
            if (current != _lastNotifiedVictim)
            {
                _lastNotifiedVictim = current;
                if (current != null) OnTargetAcquired?.Invoke(current);
                else OnTargetLost?.Invoke();
            }
        }

        if (!isBusy) return;

        if (Time.time - _sequenceStartTime > clipLength + 2f)
        {
            Debug.LogWarning($"[Assassination] 연출이 {clipLength + 2f:F1}초를 넘겨 강제 종료했습니다. Clip Length 값을 확인하세요.", this);
            ForceRelease();
        }
    }

    private void LateUpdate()
    {
        if (_isRootLocked)
        {
            transform.SetPositionAndRotation(_lockedPosition, _lockedRotation);
        }
    }

    private void HandleInteractPressed()
    {
        if (isBusy)
        {
            return;
        }

        TakedownVictim victim = FindVictim();
        if (victim == null)
        {
            if (isDebugLog)
            {
                Debug.Log("[Assassination] 조건에 맞는 대상 없음 (거리 / 각도 / 레이어 확인)");
            }

            return;
        }

        StartCoroutine(Execute_co(victim));
    }

    private TakedownVictim FindVictim()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range, npcLayer);

        TakedownVictim best = null;
        float bestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            TakedownVictim victim = hit.GetComponentInParent<TakedownVictim>();
            if (victim == null || !CanTarget(victim))
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, victim.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = victim;
            }
        }

        return best;
    }

    public bool CanTarget(TakedownVictim victim)
    {
        if (PrisonScheduleManager.Instance != null && PrisonScheduleManager.Instance.IsFreeTime) return false;
        if (victim == null || victim.isDown) return false;

        Vector3 toPlayer = (transform.position - victim.transform.position).normalized;
        float angleFromBack = Vector3.Angle(-victim.transform.forward, toPlayer);
        if (angleFromBack > backAngle * 0.5f) return false;

        EnemyAI enemyAI = victim.GetComponent<EnemyAI>();
        if (enemyAI != null && !enemyAI.CanBeAssassinated) return false;

        float distance = Vector3.Distance(transform.position, victim.transform.position);
        if (distance > range) return false;

        return true;
    }

    private IEnumerator Execute_co(TakedownVictim victim)
    {
        isBusy = true;
        _sequenceStartTime = Time.time;

        if (_playerAnimator != null)
        {
            _isPrevRootMotion = _playerAnimator.isRootMotionEnabled;
        }

        try
        {
            OnTakedownStarted?.Invoke(victim);

            SetPlayerControlEnabled(false);
            victim.FreezeForTakedown();
            _controller.enabled = false;

            if (isDebugLog)
            {
                Debug.Log("[Assassination] 1. 제어권 회수");
            }

            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;

            Vector3 targetPosition = victim.transform.position - victim.transform.forward * snapDistance;
            targetPosition.y = startPosition.y;
            Quaternion targetRotation = victim.transform.rotation;

            if (killCamera != null)
            {
                killCamera.SetActive(true);
            }

            float elapsed = 0f;
            while (elapsed < snapTime)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, elapsed / snapTime);
                transform.position = Vector3.Lerp(startPosition, targetPosition, k);
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, k);
                yield return null;
            }

            transform.SetPositionAndRotation(targetPosition, targetRotation);

            if (_playerAnimator != null)
            {
                _playerAnimator.isRootMotionEnabled = false;
            }

            _lockedPosition = targetPosition;
            _lockedRotation = targetRotation;
            _isRootLocked = isRootLockEnabled;

            if (isDebugLog)
            {
                Debug.Log("[Assassination] 2. 스냅 완료 · 루트 고정");
            }

            if (_playerAnimator != null)
            {
                _playerAnimator.PlayAssassinate();
            }

            StartCoroutine(PlayVictimDelayed_co(victim));

            if (isDebugLog)
            {
                Debug.Log($"[Assassination] 3. 클립 재생 (길이 {clipLength}초)");
            }

            float hold = victimDelay + ragdollAfterVictimStart;
            if (hold > 0f)
            {
                yield return new WaitForSeconds(hold);
            }

            victim.EnableRagdoll();

            if (isDebugLog)
            {
                Debug.Log("[Assassination] 4. 래그돌 전환");
            }
        }
        finally
        {
            Release();
            OnTakedownComplete?.Invoke(victim);

            if (isDebugLog)
            {
                Debug.Log("[Assassination] 5. 조작 복귀 완료");
            }
        }
    }

    private IEnumerator PlayVictimDelayed_co(TakedownVictim victim)
    {
        if (victimDelay > 0f)
        {
            yield return new WaitForSeconds(victimDelay);
        }

        victim.PlayDeathClip();

        if (isDebugLog)
        {
            Debug.Log($"[Assassination] 피해자 클립 트리거 +{Time.time - _sequenceStartTime:F2}초 (트리거 준비 {victim.isDeathTriggerReady})");
        }
    }

    private void Release()
    {
        if (_isRootLocked)
        {
            _isRootLocked = false;
            transform.SetPositionAndRotation(_lockedPosition, _lockedRotation);
        }

        if (_playerAnimator != null)
        {
            _playerAnimator.isRootMotionEnabled = _isPrevRootMotion;
            _playerAnimator.EndAssassinate();
        }

        if (killCamera != null)
        {
            killCamera.SetActive(false);
        }

        _controller.enabled = true;
        SetPlayerControlEnabled(true);

        isBusy = false;
    }

    public void ForceRelease()
    {
        StopAllCoroutines();
        Release();
    }

    private void SetPlayerControlEnabled(bool isEnabled)
    {
        if (_movement != null)
        {
            _movement.enabled = isEnabled;
        }

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.75f, 0.09f, 0.13f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
