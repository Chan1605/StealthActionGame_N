using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private bool isinvertY = false;

    [Header("Crouch")]
    [SerializeField] private bool isCrouchToggle = true;

    [Header("Vision")]
    [SerializeField] private bool isVisionToggle = false;

    [Header("Cursor")]
    [SerializeField] private bool isLockCursor = true;

    public event Action<bool> OnMenuToggled;

    private bool isMenuOn;
    private HitMan _actions;

    public HitMan actions
    {
        get
        {
            if (_actions == null)
            {
                _actions = new HitMan();
            }

            return _actions;
        }
    }

    public Vector2 MoveInput
    {
        get
        {
            return Vector2.ClampMagnitude(actions.Player.Move.ReadValue<Vector2>(), 1f);
        }
    }

    public Vector2 LookInput
    {
        get
        {
            Vector2 d = actions.Player.Look.ReadValue<Vector2>() * lookSensitivity;
            if (isinvertY)
            {
                d.y = -d.y;
            }
            return d;
        }
    }

    public bool isSprint
    {
        get
        {
            return actions.Player.Sprint.IsPressed();
        }
    }

    public bool isCrouching { get; private set; }

    public bool isVisionOn { get; private set; }

    public event Action OnJumpPressed;
    public event Action OnInteractPressed;
    public event Action OnThrowPressed;
    public event Action OnThrowReleased;
    public event Action<bool> OnCrouchChanged;
    public event Action<bool> OnVisionChanged;

    private void Awake()
    {
        _actions = actions;
    }

    private void OnEnable()
    {
        actions.Player.Enable();

        actions.Player.Jump.performed += HandleJump;
        actions.Player.Interact.performed += HandleInteract;
        actions.Player.Throw.performed += HandleThrow;
        actions.Player.Throw.canceled += HandleThrowCanceled;
        actions.Player.Crouch.performed += HandleCrouchPerformed;
        actions.Player.Crouch.canceled += HandleCrouchCanceled;
        actions.Player.Vision.performed += HandleVisionPerformed;
        actions.Player.Vision.canceled += HandleVisionCanceled;
        actions.Player.Menu.performed += HandleMenuPerformed;
    }

    private void OnDisable()
    {
        if (_actions == null)
        {
            return;
        }

        _actions.Player.Jump.performed -= HandleJump;
        _actions.Player.Interact.performed -= HandleInteract;
        _actions.Player.Throw.performed -= HandleThrow;
        _actions.Player.Throw.canceled -= HandleThrowCanceled;
        _actions.Player.Crouch.performed -= HandleCrouchPerformed;
        _actions.Player.Crouch.canceled -= HandleCrouchCanceled;
        _actions.Player.Vision.performed -= HandleVisionPerformed;
        _actions.Player.Vision.canceled -= HandleVisionCanceled;
        _actions.Player.Menu.performed -= HandleMenuPerformed;

        _actions.Player.Disable();

        SetCrouch(false);
        SetVision(false);
    }

    private void OnDestroy()
    {
        if (_actions != null)
        {
            _actions.Dispose();
        }
    }

    private void Start()
    {
        if (isLockCursor)
        {
            SetCursorLocked(true);
        }

        isMenuOn = false;
    }

    private void HandleJump(InputAction.CallbackContext context)
    {
        OnJumpPressed?.Invoke();
    }

    private void HandleInteract(InputAction.CallbackContext context)
    {
        OnInteractPressed?.Invoke();
    }

    private void HandleThrow(InputAction.CallbackContext context)
    {
        OnThrowPressed?.Invoke();
    }

    private void HandleThrowCanceled(InputAction.CallbackContext context)
    {
        OnThrowReleased?.Invoke();
    }

    private void HandleCrouchPerformed(InputAction.CallbackContext context)
    {
        if (isCrouchToggle)
        {
            SetCrouch(!isCrouching);
        }
        else
        {
            SetCrouch(true);
        }
    }

    private void HandleCrouchCanceled(InputAction.CallbackContext context)
    {
        if (isCrouchToggle)
        {
            return;
        }

        SetCrouch(false);
    }

    private void SetCrouch(bool isOn)
    {
        if (isCrouching == isOn)
        {
            return;
        }

        isCrouching = isOn;
        OnCrouchChanged?.Invoke(isOn);
    }

    private void HandleVisionPerformed(InputAction.CallbackContext context)
    {
        if (isVisionToggle)
        {
            SetVision(!isVisionOn);
        }
        else
        {
            SetVision(true);
        }
    }

    private void HandleVisionCanceled(InputAction.CallbackContext context)
    {
        if (isVisionToggle)
        {
            return;
        }

        SetVision(false);
    }

    private void SetVision(bool isOn)
    {
        if (isVisionOn == isOn)
        {
            return;
        }

        isVisionOn = isOn;
        OnVisionChanged?.Invoke(isOn);
    }

    public void SetInputEnabled(bool isEnabled)
    {
        if (isEnabled)
        {
            actions.Player.Enable();
        }
        else
        {
            actions.Player.Disable();
        }

        SetCursorLocked(isEnabled);
    }

    private void SetCursorLocked(bool isLocked)
    {
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }

    public void HandleMenuPerformed(InputAction.CallbackContext context)
    {
        if (context.phase.Equals(InputActionPhase.Performed))
        {
            isMenuOn = !isMenuOn;
            OnMenuToggled?.Invoke(isMenuOn);
        }
    }
}
