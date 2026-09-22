using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerInput))]
public class PlayerThrow : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private string throwTrigger = "Throw";
    [SerializeField] private float releaseDelay = 0.35f;
    [SerializeField] private float recoverDuration = 0f;

    [Header("Throw")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private float throwPower = 12f;
    [SerializeField] private float launchAngle = 20f;
    [SerializeField] private float cameraPitchInfluence = 0.6f;
    [SerializeField] private Vector3 originOffset = new Vector3(0f, 0.1f, 0.35f);

    [Header("Despawn")]
    [SerializeField] private bool isDespawnAfterLanding = true;
    [SerializeField] private float despawnDelay = 2f;
    [SerializeField] private LayerMask landingMask = ~0;

    [Header("Aim")]
    [SerializeField] private ThrowTrajectory trajectory;
    [SerializeField] private bool isAimRequired = true;
    [SerializeField] private bool isFaceAimDirection = true;
    [SerializeField] private float aimTurnTime = 0.08f;

    private PlayerInput _input;
    private PlayerHand _hand;
    private PlayerAnimator _playerAnimator;
    private PlayerCameraRig _cameraRig;
    private PlayerInteractionRunner _runner;
    private AssassinationSystem _assassination;

    private float _turnVelocity;

    public bool isBusy { get; private set; }

    public bool isAiming { get; private set; }

    public event Action<HoldableItem> OnThrown;
    public event Action<bool> OnAimChanged;

    private void Awake()
    {
        _input = GetComponent<PlayerInput>();
        _hand = GetComponentInChildren<PlayerHand>();
        _playerAnimator = GetComponent<PlayerAnimator>();
        _cameraRig = GetComponent<PlayerCameraRig>();
        _runner = GetComponent<PlayerInteractionRunner>();
        _assassination = GetComponent<AssassinationSystem>();

        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        if (trajectory == null)
        {
            trajectory = GetComponentInChildren<ThrowTrajectory>(true);
        }
    }

    private void OnEnable()
    {
        _input.OnThrowPressed += HandleThrowPressed;
        _input.OnThrowReleased += HandleThrowReleased;
    }

    private void OnDisable()
    {
        _input.OnThrowPressed -= HandleThrowPressed;
        _input.OnThrowReleased -= HandleThrowReleased;

        SetAiming(false);
    }

    private void Update()
    {
        if (!isAiming)
        {
            return;
        }

        if (!CanThrow())
        {
            SetAiming(false);
            return;
        }

        if (trajectory != null)
        {
            trajectory.Show(GetThrowOrigin(), GetAimDirection() * throwPower);
        }
    }

    private void LateUpdate()
    {
        if (!isAiming || !isFaceAimDirection)
        {
            return;
        }

        float targetYaw = _cameraRig != null ? _cameraRig.yaw : transform.eulerAngles.y;
        float yaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref _turnVelocity, aimTurnTime);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private bool CanThrow()
    {
        if (isBusy)
        {
            return false;
        }

        if (_hand == null || !_hand.isHolding)
        {
            return false;
        }

        if (_runner != null && _runner.isBusy)
        {
            return false;
        }

        if (_assassination != null && _assassination.isBusy)
        {
            return false;
        }

        return true;
    }

    private void HandleThrowPressed()
    {
        if (!CanThrow())
        {
            return;
        }

        if (!isAimRequired)
        {
            StartCoroutine(Throw_co());
            return;
        }

        SetAiming(true);
    }

    private void HandleThrowReleased()
    {
        if (!isAiming)
        {
            return;
        }

        SetAiming(false);

        if (!CanThrow())
        {
            return;
        }

        StartCoroutine(Throw_co());
    }

    private void SetAiming(bool isOn)
    {
        if (isAiming == isOn)
        {
            return;
        }

        isAiming = isOn;

        if (isOn)
        {
            _turnVelocity = 0f;
        }
        else if (trajectory != null)
        {
            trajectory.Hide();
        }

        OnAimChanged?.Invoke(isOn);
    }

    private IEnumerator Throw_co()
    {
        isBusy = true;

        try
        {
            if (_playerAnimator != null)
            {
                _playerAnimator.PlayUpperAction(throwTrigger);
            }

            yield return null;

            float total = recoverDuration > 0f
                ? recoverDuration
                : (_playerAnimator != null ? _playerAnimator.upperStateLength : 1f);

            float delay = Mathf.Max(0f, releaseDelay);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            ReleaseItem();

            float rest = total - delay;
            if (rest > 0f)
            {
                yield return new WaitForSeconds(rest);
            }
        }
        finally
        {
            if (_playerAnimator != null)
            {
                _playerAnimator.EndUpperAction(throwTrigger);
            }

            isBusy = false;
        }
    }

    private void ReleaseItem()
    {
        if (_hand == null || !_hand.isHolding)
        {
            return;
        }

        Vector3 origin = GetThrowOrigin();
        Vector3 direction = GetAimDirection();

        HoldableItem held = _hand.heldItem;
        if (held != null)
        {
            held.transform.position = origin;
        }

        HoldableItem item = _hand.Release(direction * throwPower);

        if (item != null)
        {
            if (isDespawnAfterLanding)
            {
                ArmDespawn(item);
            }

            OnThrown?.Invoke(item);
        }
    }

    private void ArmDespawn(HoldableItem item)
    {
        ThrownItemDespawn despawn = item.GetComponent<ThrownItemDespawn>();

        if (despawn == null)
        {
            despawn = item.gameObject.AddComponent<ThrownItemDespawn>();
        }

        despawn.Arm(despawnDelay, landingMask);
    }

    private Vector3 GetThrowOrigin()
    {
        Transform anchor = throwOrigin;

        if (anchor == null && _hand != null)
        {
            anchor = _hand.socket;
        }

        Vector3 basePosition = anchor != null
            ? anchor.position
            : transform.position + Vector3.up * 1.4f;

        return basePosition + transform.TransformDirection(originOffset);
    }

    private Vector3 GetAimDirection()
    {
        Vector3 flat = transform.forward;
        flat.y = 0f;

        if (flat.sqrMagnitude < 0.0001f)
        {
            flat = Vector3.forward;
        }

        flat.Normalize();

        float elevation = Mathf.Clamp(launchAngle + GetCameraPitch() * cameraPitchInfluence, -60f, 80f);
        Vector3 right = Vector3.Cross(Vector3.up, flat);

        return Quaternion.AngleAxis(-elevation, right) * flat;
    }

    private float GetCameraPitch()
    {
        if (aimCamera == null)
        {
            return 0f;
        }

        float pitch = aimCamera.transform.eulerAngles.x;

        if (pitch > 180f)
        {
            pitch -= 360f;
        }

        return -pitch;
    }
}
