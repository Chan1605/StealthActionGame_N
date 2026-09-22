using System;
using UnityEngine;

public class DummyDamageable : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float debugHealth = 100f;
    [SerializeField] private UI_PlayerHealth healthUI;

    [Header("리커버리")]
    [SerializeField] private float regenDelay = 4f;
    [SerializeField] private float regenInterval = 1f;
    [SerializeField] private float regenAmount = 10f;

    private float _timeSinceLastDamage;
    private float _regenTimer;
    private bool _isDead;

    public event Action OnDied;

    private void Awake()
    {
        if (healthUI == null) healthUI = FindAnyObjectByType<UI_PlayerHealth>();
        debugHealth = maxHealth;
        _timeSinceLastDamage = regenDelay;
    }

    private void Start()
    {
        healthUI?.UpdateHealthUI(debugHealth);
    }

    private void Update()
    {
        if (_isDead) return;
        if (debugHealth >= maxHealth) return;

        _timeSinceLastDamage += Time.deltaTime;
        if (_timeSinceLastDamage < regenDelay) return;

        _regenTimer += Time.deltaTime;
        if (_regenTimer < regenInterval) return;

        _regenTimer = 0f;
        debugHealth = Mathf.Min(maxHealth, debugHealth + regenAmount);
        healthUI?.UpdateHealthUI(debugHealth);
    }

    public void TakeDamage(float amount)
    {
        if (_isDead) return;

        debugHealth = Mathf.Max(0f, debugHealth - amount);
        _timeSinceLastDamage = 0f;
        _regenTimer = 0f;
        healthUI?.UpdateHealthUI(debugHealth);

        if (debugHealth <= 0f && !_isDead)
        {
            _isDead = true; // OnDied 호출 전에 먼저 세팅 — 재진입 완전 차단
            OnDied?.Invoke();
        }
    }

    public void ResetHealth()
    {
        _isDead = false;
        debugHealth = maxHealth;
        _timeSinceLastDamage = regenDelay;
        _regenTimer = 0f;
        healthUI?.UpdateHealthUI(debugHealth);
    }
}