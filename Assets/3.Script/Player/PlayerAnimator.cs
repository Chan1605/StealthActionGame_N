using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Animator animator;

    [Header("Damping")]
    [SerializeField] private float directionDamping = 0.12f;

    [Header("Action Layer")]
    [SerializeField] private int actionLayer = 1;
    [SerializeField] private string emptyStateName = "none";
    [SerializeField] private float emptyBlendTime = 0.1f;

    [Header("Hand IK")]
    [SerializeField] private int ikLayer = 1;
    [SerializeField] [Range(0f, 1f)] private float handRotationWeight = 0f;

    [Header("Upper Body Layer")]
    [SerializeField] private int upperBodyLayer = 2;
    [SerializeField] private string upperBodyEmptyStateName = "none";
    [SerializeField] private float upperBodyBlendIn = 0.12f;
    [SerializeField] private float upperBodyBlendOut = 0.22f;

    private PlayerController _controller;
    private Coroutine _upperFadeRoutine;

    private bool _isLeftHandIkOn;
    private bool _isRightHandIkOn;
    private Vector3 _leftHandPosition;
    private Vector3 _rightHandPosition;
    private Quaternion _leftHandRotation = Quaternion.identity;
    private Quaternion _rightHandRotation = Quaternion.identity;
    private float _leftHandWeight;
    private float _rightHandWeight;

    public bool isIkPassActive { get; private set; }

    private static readonly int MoveXId = Animator.StringToHash("MoveX");
    private static readonly int MoveYId = Animator.StringToHash("MoveY");
    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int IsCrouchedId = Animator.StringToHash("IsCrouched");
    private static readonly int IsGroundedId = Animator.StringToHash("IsGrounded");
    private static readonly int JumpId = Animator.StringToHash("Jump");
    private static readonly int AssassinateId = Animator.StringToHash("Assassinate");
    private static readonly int IsDieId = Animator.StringToHash("IsDie");

    public event Action OnFootstepPlayed;
    public event Action OnLandImpact;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void OnEnable()
    {
        _controller.OnJumped += HandleJumped;
        _controller.OnCrouchStateChanged += HandleCrouchStateChanged;
    }

    private void OnDisable()
    {
        _controller.OnJumped -= HandleJumped;
        _controller.OnCrouchStateChanged -= HandleCrouchStateChanged;
    }

    private void Start()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(IsCrouchedId, _controller.isCrouched);
        animator.SetBool(IsGroundedId, _controller.isGrounded);
    }

    private void LateUpdate()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat(MoveXId, _controller.moveX, directionDamping, Time.deltaTime);
        animator.SetFloat(MoveYId, _controller.moveY, directionDamping, Time.deltaTime);
        animator.SetFloat(SpeedId, _controller.normalizedSpeed);
        animator.SetBool(IsGroundedId, _controller.isGrounded);
    }

    private void HandleJumped()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetTrigger(JumpId);
    }

    private void HandleCrouchStateChanged(bool isOn)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetBool(IsCrouchedId, isOn);
    }

    public void PlayAssassinate()
    {
        PlayAction(AssassinateId);
    }
    public void PlayDie()
    {
        if (animator == null) return;
        animator.SetBool(IsDieId, true);
    }

    public void ResetDie()
    {
        if (animator == null) return;
        animator.SetBool(IsDieId, false);
    }
    public void EndAssassinate()
    {
        EndAction(AssassinateId);
    }

    public bool PlayAction(string triggerName)
    {
        if (!HasTrigger(triggerName))
        {
            Debug.LogWarning($"[PlayerAnimator] Animator에 '{triggerName}' 트리거가 없습니다. 컨트롤러에 Trigger 파라미터를 추가하세요.", this);
            return false;
        }

        PlayAction(Animator.StringToHash(triggerName));
        return true;
    }

    public void EndAction(string triggerName)
    {
        if (!HasTrigger(triggerName))
        {
            EndAction(0);
            return;
        }

        EndAction(Animator.StringToHash(triggerName));
    }

    public bool PlayUpperAction(string triggerName)
    {
        if (!HasTrigger(triggerName))
        {
            Debug.LogWarning($"[PlayerAnimator] Animator에 '{triggerName}' 트리거가 없습니다. 컨트롤러에 Trigger 파라미터를 추가하세요.", this);
            return false;
        }

        if (animator == null)
        {
            return false;
        }

        FadeUpperLayer(1f, upperBodyBlendIn);
        animator.SetTrigger(triggerName);

        return true;
    }

    public void EndUpperAction(string triggerName)
    {
        if (animator == null)
        {
            return;
        }

        FadeUpperLayer(0f, upperBodyBlendOut);

        if (HasTrigger(triggerName))
        {
            animator.ResetTrigger(triggerName);
        }
    }

    private void FadeUpperLayer(float target, float duration)
    {
        if (_upperFadeRoutine != null)
        {
            StopCoroutine(_upperFadeRoutine);
        }

        _upperFadeRoutine = StartCoroutine(FadeUpperLayer_co(target, duration));
    }

    private IEnumerator FadeUpperLayer_co(float target, float duration)
    {
        float start = animator.GetLayerWeight(upperBodyLayer);

        if (duration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                animator.SetLayerWeight(upperBodyLayer, Mathf.Lerp(start, target, elapsed / duration));
                yield return null;
            }
        }

        animator.SetLayerWeight(upperBodyLayer, target);

        if (target <= 0f && !string.IsNullOrEmpty(upperBodyEmptyStateName))
        {
            animator.CrossFade(upperBodyEmptyStateName, 0f, upperBodyLayer);
        }

        _upperFadeRoutine = null;
    }

    public float upperStateLength
    {
        get
        {
            if (animator == null)
            {
                return 0f;
            }

            AnimatorStateInfo info = animator.GetNextAnimatorStateInfo(upperBodyLayer);

            if (info.length <= 0f)
            {
                info = animator.GetCurrentAnimatorStateInfo(upperBodyLayer);
            }

            return info.length / Mathf.Max(0.01f, info.speed == 0f ? 1f : Mathf.Abs(info.speed));
        }
    }

    public void SetHandIK(bool isLeft, Vector3 position, Quaternion rotation, float weight)
    {
        float clamped = Mathf.Clamp01(weight);

        if (isLeft)
        {
            _isLeftHandIkOn = clamped > 0.001f;
            _leftHandPosition = position;
            _leftHandRotation = rotation;
            _leftHandWeight = clamped;

            return;
        }

        _isRightHandIkOn = clamped > 0.001f;
        _rightHandPosition = position;
        _rightHandRotation = rotation;
        _rightHandWeight = clamped;
    }

    public void ClearHandIK()
    {
        _isLeftHandIkOn = false;
        _isRightHandIkOn = false;
        _leftHandWeight = 0f;
        _rightHandWeight = 0f;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || layerIndex != ikLayer)
        {
            return;
        }

        isIkPassActive = true;

        ApplyHandIK(AvatarIKGoal.LeftHand, _isLeftHandIkOn, _leftHandPosition, _leftHandRotation, _leftHandWeight);
        ApplyHandIK(AvatarIKGoal.RightHand, _isRightHandIkOn, _rightHandPosition, _rightHandRotation, _rightHandWeight);
    }

    private void ApplyHandIK(AvatarIKGoal goal, bool isOn, Vector3 position, Quaternion rotation, float weight)
    {
        if (!isOn)
        {
            animator.SetIKPositionWeight(goal, 0f);
            animator.SetIKRotationWeight(goal, 0f);

            return;
        }

        animator.SetIKPositionWeight(goal, weight);
        animator.SetIKPosition(goal, position);

        animator.SetIKRotationWeight(goal, weight * handRotationWeight);
        animator.SetIKRotation(goal, rotation);
    }

    public bool HasTrigger(string triggerName)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(triggerName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == triggerName)
            {
                return true;
            }
        }

        return false;
    }

    private void PlayAction(int triggerId)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetLayerWeight(actionLayer, 1f);
        animator.SetTrigger(triggerId);
    }

    private void EndAction(int triggerId)
    {
        if (animator == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(emptyStateName))
        {
            animator.CrossFade(emptyStateName, emptyBlendTime, actionLayer);
        }

        animator.SetLayerWeight(actionLayer, 0f);

        if (triggerId != 0)
        {
            animator.ResetTrigger(triggerId);
        }
    }

    public bool isRootMotionEnabled
    {
        get
        {
            return animator != null && animator.applyRootMotion;
        }
        set
        {
            if (animator != null)
            {
                animator.applyRootMotion = value;
            }
        }
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight <= 0.5f)
        {
            return;
        }

        OnFootstepPlayed?.Invoke();
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight <= 0.5f)
        {
            return;
        }

        OnLandImpact?.Invoke();
    }
}
