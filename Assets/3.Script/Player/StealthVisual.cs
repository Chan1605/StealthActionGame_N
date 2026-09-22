using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class StealthVisual : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Volume volume;
    [SerializeField] [Range(0f, 1f)] private float maxWeight = 1f;
    [SerializeField] private float fadeInTime = 0.35f;
    [SerializeField] private float fadeOutTime = 0.2f;

    [Header("Condition")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerDetectable detectable;
    [SerializeField] private bool isCancelledWhenAlerted = true;
    [SerializeField] private float alertScore = 30f;
    [SerializeField] private float enemyScanInterval = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool isDebugLog = false;

    private EnemyPerception[] _enemies;
    private float _scanTimer;
    private bool _isAlerted;
    private bool _isStealthPrev;

    public bool isStealth
    {
        get
        {
            if (detectable != null && detectable.IsDeadOrRespawning)
            {
                return false;
            }

            if (playerController == null || !playerController.isCrouched)
            {
                return false;
            }

            if (isCancelledWhenAlerted && _isAlerted)
            {
                return false;
            }

            return true;
        }
    }

    public event Action<bool> OnStealthChanged;

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController>();
        }

        if (detectable == null)
        {
            detectable = GetComponentInParent<PlayerDetectable>();
        }

        if (volume == null)
        {
            volume = GetComponentInChildren<Volume>(true);
        }

        if (volume == null)
        {
            Debug.LogError($"[StealthVisual] Volume이 없습니다. Player 아래에 Volume을 만들고 연결하세요.", this);
            enabled = false;
            return;
        }

        if (!volume.isGlobal)
        {
            Debug.LogWarning($"[StealthVisual] Volume의 Is Global이 꺼져 있습니다. 로컬 볼륨은 카메라 위치로 판정하므로 3인칭에서는 적용되지 않습니다.", volume);
        }

        volume.weight = 0f;
    }

    private void Start()
    {
        RefreshEnemies();
    }

    public void RefreshEnemies()
    {
        _enemies = FindObjectsByType<EnemyPerception>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }

    private void Update()
    {
        TickAlert();
        TickWeight();
    }

    private void TickAlert()
    {
        if (!isCancelledWhenAlerted)
        {
            _isAlerted = false;
            return;
        }

        _scanTimer -= Time.deltaTime;

        if (_scanTimer > 0f)
        {
            return;
        }

        _scanTimer = Mathf.Max(0.05f, enemyScanInterval);

        if (_enemies == null)
        {
            RefreshEnemies();
        }

        bool isAnyAlerted = false;

        foreach (EnemyPerception enemy in _enemies)
        {
            if (enemy == null || !enemy.isActiveAndEnabled)
            {
                continue;
            }

            if (enemy.MaxScore >= alertScore)
            {
                isAnyAlerted = true;
                break;
            }
        }

        _isAlerted = isAnyAlerted;
    }

    private void TickWeight()
    {
        bool isOn = isStealth;

        if (isOn != _isStealthPrev)
        {
            _isStealthPrev = isOn;
            OnStealthChanged?.Invoke(isOn);

            if (isDebugLog)
            {
                Debug.Log($"[StealthVisual] 은신 {(isOn ? "진입" : "해제")}", this);
            }
        }

        float target = isOn ? maxWeight : 0f;
        float duration = isOn ? fadeInTime : fadeOutTime;

        if (duration <= 0f)
        {
            volume.weight = target;
            return;
        }

        volume.weight = Mathf.MoveTowards(volume.weight, target, Time.deltaTime / duration);
    }
}
