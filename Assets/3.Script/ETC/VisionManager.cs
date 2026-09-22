using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;
using DG.Tweening;

public class VisionManager : MonoBehaviour
{
    [Header("투시 세팅")]
    [SerializeField] private Camera cam;
    [SerializeField] private Volume visionVolume;

    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float visionFOV = 50f;
    [SerializeField] private float transitionDuration = 1f;

    private PlayerInput _input;

    private void Awake()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        _input = GetComponent<PlayerInput>();

        if (_input == null)
        {
            Debug.LogWarning("[VisionManager] 같은 오브젝트에 PlayerInput이 없습니다. Player에 붙여주세요.", this);
        }
    }

    private void OnEnable()
    {
        if (_input != null)
        {
            _input.OnVisionChanged += HandleVisionChanged;
        }
    }

    private void OnDisable()
    {
        if (_input != null)
        {
            _input.OnVisionChanged -= HandleVisionChanged;
        }
    }

    private void HandleVisionChanged(bool isOn)
    {
        if (isOn)
        {
            StartVision();
        }
        else
        {
            StopVision();
        }
    }

    private void Start()
    {
        Shader.SetGlobalFloat("_VisionAlpha", 0f);
        if(visionVolume !=null)
        {
            visionVolume.weight = 0f;
        }
    }

    public void StartVision()
    {
        ToggleVision(true);
        Debug.Log("투시 시작");
    }

    public void StopVision()
    {
        ToggleVision(false);
        Debug.Log("투시 종료");
    }

    private void ToggleVision(bool state)
    {
        float targetFOV;
        float targetWeight;

        if(state)
        {
            targetFOV = visionFOV;
            targetWeight = 1f;
        }
        else
        {
            targetFOV = normalFOV;
            targetWeight = 0f;
        }

        cam.DOFieldOfView(targetFOV, transitionDuration);
        DOTween.To(
            () => visionVolume.weight, 
            x =>
            {
                visionVolume.weight = x;
                Shader.SetGlobalFloat("_VisionAlpha", x);
            }, 
            targetWeight, transitionDuration);
    }

}
