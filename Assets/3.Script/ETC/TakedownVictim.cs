using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class TakedownVictim : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private string deathTrigger = "Die";

    [Header("Freeze")]
    [SerializeField] private MonoBehaviour[] aiScripts;

    [Header("Result")]
    [SerializeField] private bool isLethalTakedown = true;
    [SerializeField] private string bodyLayerName = "Body";

    [Header("IK Grab Point")]
    [SerializeField] private Transform leftNeckGrab;
    [SerializeField] private Transform rightNeckGrab;

    private Animator _animator;
    private NavMeshAgent _agent;
    private CapsuleCollider _mainCollider;

    private Rigidbody[] _ragdollBodies;
    private Collider[] _ragdollColliders;
    private int _originalLayer;
    private bool _isDeathTriggerReady;

    public bool isDown { get; private set; }

    public bool isDeathTriggerReady
    {
        get
        {
            return _isDeathTriggerReady;
        }
    }

    public bool isLethal
    {
        get
        {
            return isLethalTakedown;
        }
    }

    public Transform neckGrabLeft
    {
        get
        {
            return leftNeckGrab;
        }
    }

    public Transform neckGrabRight
    {
        get
        {
            return rightNeckGrab;
        }
    }

    public event Action<TakedownVictim> OnFrozen;
    public event Action<TakedownVictim> OnDowned;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _mainCollider = GetComponent<CapsuleCollider>();

        _ragdollBodies = GetComponentsInChildren<Rigidbody>();
        _ragdollColliders = GetComponentsInChildren<Collider>();
        SetRagdollActive(false);
        _originalLayer = gameObject.layer;
        int parts = 0;
        foreach (Rigidbody body in _ragdollBodies)
        {
            if (body.gameObject != gameObject)
            {
                parts++;
            }
        }

        if (parts < 5)
        {
            Debug.LogWarning($"[TakedownVictim] 래그돌 파츠가 {parts}개뿐입니다. GameObject > 3D Object > Ragdoll... 로 만들어주세요 (정상이면 11개 전후).", this);
        }

        _isDeathTriggerReady = HasTrigger(deathTrigger);
        if (!_isDeathTriggerReady)
        {
            Debug.LogWarning($"[TakedownVictim] Animator에 '{deathTrigger}' 트리거가 없어 사망 클립을 건너뜁니다. 컨트롤러에 Trigger 파라미터를 추가하세요.", this);
        }
    }

    private bool HasTrigger(string parameterName)
    {
        if (_animator == null || _animator.runtimeAnimatorController == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in _animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    public void FreezeForTakedown()
    {
        if (_agent != null && _agent.enabled)
        {
            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = true;
            }

            _agent.enabled = false;
        }

        foreach (MonoBehaviour ai in aiScripts)
        {
            if (ai != null)
            {
                ai.enabled = false;
            }
        }

        OnFrozen?.Invoke(this);
    }

    public void PlayDeathClip()
    {
        if (_animator == null || !_isDeathTriggerReady)
        {
            return;
        }

        _animator.SetTrigger(deathTrigger);
    }

    public void EnableRagdoll()
    {
        if (isDown)
        {
            return;
        }

        isDown = true;

        if (_animator != null)
        {
            _animator.enabled = false;
        }

        if (_mainCollider != null)
        {
            _mainCollider.enabled = false;
        }

        SetRagdollActive(true);
        MoveToBodyLayer();

        OnDowned?.Invoke(this);
    }

    private void MoveToBodyLayer()
    {
        if (string.IsNullOrEmpty(bodyLayerName))
        {
            return;
        }

        int layer = LayerMask.NameToLayer(bodyLayerName);
        if (layer < 0)
        {
            Debug.LogWarning($"[TakedownVictim] '{bodyLayerName}' 레이어가 없어 건너뜁니다. Project Settings > Tags and Layers 에서 만들어주세요.", this);
            return;
        }

        SetLayerRecursively(gameObject, layer);
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;

        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private void SetRagdollActive(bool isOn)
    {
        foreach (Rigidbody body in _ragdollBodies)
        {
            if (body.gameObject == gameObject)
            {
                continue;
            }

            body.isKinematic = !isOn;
            body.detectCollisions = isOn;
        }

        foreach (Collider col in _ragdollColliders)
        {
            if (col == _mainCollider)
            {
                continue;
            }

            col.enabled = isOn;
        }
    }

    public void Revive()
    {
        if (!isDown) return;

        isDown = false;
        SetRagdollActive(false);

        if (_mainCollider != null) _mainCollider.enabled = true;

        if (_animator != null)
        {
            _animator.enabled = true;
            _animator.Rebind();
            _animator.Update(0f);
        }

        SetLayerRecursively(gameObject, _originalLayer);

        foreach (MonoBehaviour ai in aiScripts)
        {
            if (ai != null) ai.enabled = true;
        }

        Debug.Log("체크포인트 리셋으로 되살아남");
    }
}
