using System;
using UnityEngine;

public class KeyDropOnDeath : MonoBehaviour, IInteractable, ICompletionState
{
    [SerializeField] private GameObject keyPrefab;
    [SerializeField] private Transform dropPoint;

    public Transform ObjectTransform => transform;
    public Action OnUse { get; set; }
    public Action OnLook { get; set; }
    public event Action OnTargetCompleted;

    public bool IsPlayerLook { get; set; }
    public bool IsInteractable { get; private set; } = true;

    private TakedownVictim _victim;

    // ICompletionState: 적이 쓰러졌다면(열쇠를 이미 떨궜다면) true
    private bool _isCompleted;
    public bool IsCompleted
    {
        get { return _isCompleted; }
    }

    private void Awake()
    {
        TryGetComponent(out _victim);
    }

    private void OnEnable()
    {
        if (_victim != null) _victim.OnDowned += HandleDowned;
    }

    private void OnDisable()
    {
        if (_victim != null) _victim.OnDowned -= HandleDowned;
    }

    private void HandleDowned(TakedownVictim victim)
    {
        Vector3 spawnPos = dropPoint != null ? dropPoint.position : transform.position;
        Instantiate(keyPrefab, spawnPos, Quaternion.identity);

        _isCompleted = true;   // 완료 기록 (미션 차례 전에 쓰러져도 남는다)
        OnTargetCompleted?.Invoke();
    }

    public void EnableInteraction() { }
    public void DisableInteraction() { }
}