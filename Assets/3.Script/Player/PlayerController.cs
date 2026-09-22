using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float sprintSpeed = 6.5f;
    [SerializeField] private float crouchSpeed = 1.5f;
    [SerializeField] private float acceleration = 14f;
    [SerializeField] private float deceleration = 40f;

    [Header("Rotation")]
    [SerializeField] private float turnSmoothTime = 0.1f;

    [Header("Jump")]
    [SerializeField] private float jumpHeight = 1.1f;
    [SerializeField] private float gravity = -22f;
    [SerializeField] private float groundedGravity = -2f;
    [SerializeField] private float coyoteTime = 0.12f;

    [Header("Crouch")]
    [SerializeField] private float crouchHeight = 1.1f;
    [SerializeField] private float crouchRadius = 0.5f;
    [SerializeField] private float heightLerpSpeed = 10f;
    [SerializeField] private LayerMask ceilingMask;

    private CharacterController _controller;
    private PlayerInput _input;
    private PlayerCameraRig _cameraRig;
    private PlayerWallClimb _wallClimb;
    private CarrySystem _carry;

    private float _speedOverride = -1f;

    private Vector3 _planarVelocity;
    private Vector3 _lastMoveDirection;
    private float _currentSpeed;
    private Vector2 _moveDirection;
    private float _turnVelocity;
    private float _verticalVelocity;
    private float _standHeight;
    private float _standRadius;
    private float _targetHeight;
    private float _targetRadius;
    private float _capsuleBottom;
    private float _centerX;
    private float _centerZ;
    private float _groundedTimer;
    private bool _isGroundedPrev;

    public bool isGrounded
    {
        get
        {
            return _groundedTimer > 0f;
        }
    }

    public bool isCrouched { get; private set; }

    private bool isCarrying
    {
        get
        {
            return _carry != null && _carry.isCarrying;
        }
    }

    public float maxJumpHeight
    {
        get
        {
            return jumpHeight;
        }
    }

    public void StopVertical()
    {
        _verticalVelocity = 0f;
    }

    /// <summary>운반 중처럼 이동 속도를 고정해야 할 때 사용합니다. 달리기/앉기 속도를 모두 무시합니다.</summary>
    public void SetSpeedOverride(float speed)
    {
        _speedOverride = Mathf.Max(0f, speed);
    }

    public void ClearSpeedOverride()
    {
        _speedOverride = -1f;
    }

    /// <summary>천장 검사 없이 강제로 일어섭니다. 시체를 들어올릴 때처럼 애니메이션이 이미 일어선 경우에 씁니다.</summary>
    public void ForceStand()
    {
        SetCrouched(false);
    }

    public float currentSpeed
    {
        get
        {
            return _planarVelocity.magnitude;
        }
    }

    public float normalizedSpeed
    {
        get
        {
            return _planarVelocity.magnitude / sprintSpeed;
        }
    }

    public float moveX
    {
        get
        {
            return _moveDirection.x;
        }
    }

    public float moveY
    {
        get
        {
            return _moveDirection.y;
        }
    }

    public event Action OnJumped;
    public event Action OnLanded;
    public event Action<bool> OnCrouchStateChanged;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<PlayerInput>();
        _cameraRig = GetComponent<PlayerCameraRig>();
        _wallClimb = GetComponent<PlayerWallClimb>();
        _carry = GetComponent<CarrySystem>();

        _moveDirection = Vector2.up;
        _lastMoveDirection = transform.forward;
        _groundedTimer = coyoteTime;
        _isGroundedPrev = true;
        _standHeight = _controller.height;
        _standRadius = _controller.radius;
        _targetHeight = _standHeight;
        _targetRadius = _standRadius;
        _capsuleBottom = _controller.center.y - _standHeight * 0.5f;
        _centerX = _controller.center.x;
        _centerZ = _controller.center.z;
    }

    private void OnEnable()
    {
        _input.OnJumpPressed += HandleJumpPressed;
        _input.OnCrouchChanged += HandleCrouchChanged;
    }

    private void OnDisable()
    {
        _input.OnJumpPressed -= HandleJumpPressed;
        _input.OnCrouchChanged -= HandleCrouchChanged;
    }

    private void Update()
    {
        ApplyGravity();
        ApplyCapsule();
        ApplyMove();
        UpdateGrounded();
    }

    private void ApplyGravity()
    {
        if (_controller.isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = groundedGravity;
            return;
        }

        _verticalVelocity += gravity * Time.deltaTime;
    }

    private void ApplyCapsule()
    {
        bool isHeightSettled = Mathf.Approximately(_controller.height, _targetHeight);
        bool isRadiusSettled = Mathf.Approximately(_controller.radius, _targetRadius);

        if (isHeightSettled && isRadiusSettled)
        {
            return;
        }

        float k = heightLerpSpeed * Time.deltaTime;
        float height = Mathf.Lerp(_controller.height, _targetHeight, k);
        float radius = Mathf.Min(Mathf.Lerp(_controller.radius, _targetRadius, k), height * 0.5f);

        _controller.height = height;
        _controller.radius = radius;
        _controller.center = new Vector3(_centerX, _capsuleBottom + height * 0.5f, _centerZ);
    }

    private void ApplyMove()
    {
        Vector2 move = _input.MoveInput;
        bool isMoveInput = move.sqrMagnitude > 0.0001f;
        float targetSpeed = 0f;

        if (isMoveInput)
        {
            Vector3 direction = Quaternion.Euler(0f, GetCameraYaw(), 0f) * new Vector3(move.x, 0f, move.y);
            direction.Normalize();

            ApplyTurn(direction);

            _lastMoveDirection = direction;
            targetSpeed = GetTargetSpeed() * Mathf.Clamp01(move.magnitude);
        }

        float rate = targetSpeed > _currentSpeed ? acceleration : deceleration;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, rate * Time.deltaTime);

        _planarVelocity = _lastMoveDirection * _currentSpeed;

        UpdateMoveDirection();

        Vector3 velocity = _planarVelocity + Vector3.up * _verticalVelocity;
        _controller.Move(velocity * Time.deltaTime);
    }

    private float GetCameraYaw()
    {
        if (_cameraRig != null)
        {
            return _cameraRig.yaw;
        }

        return transform.eulerAngles.y;
    }

    private void ApplyTurn(Vector3 direction)
    {
        float targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float yaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref _turnVelocity, turnSmoothTime);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void UpdateMoveDirection()
    {
        Vector3 local = transform.InverseTransformDirection(_planarVelocity);
        Vector2 flat = new Vector2(local.x, local.z);

        if (flat.sqrMagnitude < 0.0001f)
        {
            return;
        }

        _moveDirection = flat.normalized;
    }

    private float GetTargetSpeed()
    {
        if (_speedOverride >= 0f)
        {
            return _speedOverride;
        }

        if (isCrouched)
        {
            return crouchSpeed;
        }

        if (_input.isSprint)
        {
            return sprintSpeed;
        }

        return walkSpeed;
    }

    private void UpdateGrounded()
    {
        bool isGroundedNow = _controller.isGrounded;

        if (isGroundedNow)
        {
            _groundedTimer = coyoteTime;
        }
        else
        {
            _groundedTimer -= Time.deltaTime;
        }

        if (isGroundedNow && !_isGroundedPrev)
        {
            OnLanded?.Invoke();
        }

        _isGroundedPrev = isGroundedNow;
    }

    private void HandleJumpPressed()
    {
        if (_carry != null && _carry.isBlockingOtherActions)
        {
            return;
        }

        if (_wallClimb != null && _wallClimb.TryClimb())
        {
            return;
        }

        if (!isGrounded)
        {
            return;
        }

        if (isCrouched)
        {
            TryStandUp();
            return;
        }

        _groundedTimer = 0f;
        _isGroundedPrev = false;
        _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        OnJumped?.Invoke();
    }

    private void HandleCrouchChanged(bool isOn)
    {
        if (isCarrying)
        {
            return;
        }

        if (isOn)
        {
            SetCrouched(true);
            return;
        }

        TryStandUp();
    }

    private void TryStandUp()
    {
        if (IsCeilingBlocked())
        {
            return;
        }

        SetCrouched(false);
    }

    private void SetCrouched(bool isOn)
    {
        if (isCrouched == isOn)
        {
            return;
        }

        isCrouched = isOn;
        _targetHeight = isOn ? Mathf.Min(crouchHeight, _standHeight) : _standHeight;
        _targetRadius = isOn ? crouchRadius : _standRadius;
        OnCrouchStateChanged?.Invoke(isOn);
    }

    private bool IsCeilingBlocked()
    {
        float distance = _standHeight - _controller.height;
        if (distance <= 0.01f)
        {
            return false;
        }

        float radius = _controller.radius * 0.95f;
        Vector3 center = transform.TransformPoint(_controller.center);
        Vector3 origin = center + Vector3.up * (_controller.height * 0.5f - radius);

        return Physics.SphereCast(origin, radius, Vector3.up, out _, distance, ceilingMask, QueryTriggerInteraction.Ignore);
    }
}
