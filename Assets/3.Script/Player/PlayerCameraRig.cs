using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(PlayerInput))]
public class PlayerCameraRig : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float startPitch = 45;
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 50f;

    [Header("Zoom")]
    [SerializeField] private CinemachineThirdPersonFollow thirdPersonFollow;
    [SerializeField] private float defaultDistance = 3.5f;
    [SerializeField] private float zoomDuration = 0.25f;

    [Header("Shake")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    private PlayerInput _input;
    private float _yaw;
    private float _pitch;
    private int _lookFrame = -1;
    private Coroutine _zoomRoutine;

    public float yaw
    {
        get
        {
            UpdateLook();
            return _yaw;
        }
    }

    public float pitch
    {
        get
        {
            UpdateLook();
            return _pitch;
        }
    }

    private void Awake()
    {
        _input = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        if (thirdPersonFollow != null)
        {
            thirdPersonFollow.CameraDistance = defaultDistance;
        }

        ResetLook();
    }

    private void Update()
    {
        UpdateLook();
    }

    private void LateUpdate()
    {
        ApplyPivot();
    }

    public void ResetLook()
    {
        _yaw = transform.eulerAngles.y;
        _pitch = Mathf.Clamp(startPitch, minPitch, maxPitch);
        _lookFrame = Time.frameCount;

        ApplyPivot();
    }

    private void UpdateLook()
    {
        if (_lookFrame == Time.frameCount)
        {
            return;
        }

        _lookFrame = Time.frameCount;

        Vector2 look = _input.LookInput;
        _yaw = Mathf.Repeat(_yaw + look.x, 360f);
        _pitch = Mathf.Clamp(_pitch + look.y, minPitch, maxPitch);
    }

    private void ApplyPivot()
    {
        if (cameraPivot == null)
        {
            return;
        }

        cameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    public void ZoomTo(float distance)
    {
        if (thirdPersonFollow == null)
        {
            return;
        }

        if (_zoomRoutine != null)
        {
            StopCoroutine(_zoomRoutine);
        }

        _zoomRoutine = StartCoroutine(Zoom_co(distance));
    }

    public void ResetZoom()
    {
        ZoomTo(defaultDistance);
    }

    public void Shake(float force)
    {
        if (impulseSource == null)
        {
            return;
        }

        impulseSource.GenerateImpulseWithForce(force);
    }

    private IEnumerator Zoom_co(float targetDistance)
    {
        float start = thirdPersonFollow.CameraDistance;
        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            thirdPersonFollow.CameraDistance = Mathf.Lerp(start, targetDistance, elapsed / zoomDuration);
            yield return null;
        }

        thirdPersonFollow.CameraDistance = targetDistance;
        _zoomRoutine = null;
    }
}
