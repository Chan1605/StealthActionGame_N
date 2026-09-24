using System;
using UnityEngine;

// "특정 장소에 도달" 목표용 타겟.
// E로 상호작용하지 않고, 차례가 된 뒤 플레이어가 트리거 안에 들어오면 완료된다.
[RequireComponent(typeof(Collider))]
public class ReachZoneTarget : MonoBehaviour, IInteractable, ICompletionState
{
    [Header("Debug")]
    [SerializeField] private bool isDebugLog = true;

    [Header("Gizmo")]
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.5f, 0.25f);

    private bool _isActive;
    private bool _isCompleted;

    public Transform ObjectTransform
    {
        get { return transform; }
    }

    public Action OnUse { get; set; }
    public Action OnLook { get; set; }

    public event Action OnTargetCompleted;

    public bool IsPlayerLook { get; set; }

    // 시선/E 입력으로는 반응하지 않는다. (PlayerInteractor가 잡아도 무시된다)
    public bool IsInteractable
    {
        get { return false; }
    }

    public bool IsCompleted
    {
        get { return _isCompleted; }
    }

    private void Awake()
    {
        Collider zone = GetComponent<Collider>();

        if (!zone.isTrigger)
        {
            zone.isTrigger = true;
            Debug.LogWarning($"[ReachZone] '{name}'의 Collider를 Is Trigger로 켰습니다.", this);
        }
    }

    // StageManager가 이 타겟의 차례가 되면 호출한다.
    public void EnableInteraction()
    {
        _isActive = true;
    }

    public void DisableInteraction()
    {
        _isActive = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryComplete(other);
    }

    // 차례가 됐을 때 플레이어가 이미 안에 서 있는 경우를 위해 Stay도 본다.
    private void OnTriggerStay(Collider other)
    {
        TryComplete(other);
    }

    private void TryComplete(Collider other)
    {
        if (!_isActive || _isCompleted)
        {
            return;
        }

        if (other == null || !other.CompareTag("Player"))
        {
            return;
        }

        _isCompleted = true;
        _isActive = false;

        if (isDebugLog)
        {
            Debug.Log($"[ReachZone] '{name}' 도달", this);
        }

        OnUse?.Invoke();
        OnTargetCompleted?.Invoke();
    }

    private void OnDrawGizmos()
    {
        Collider zone = GetComponent<Collider>();

        if (zone == null)
        {
            return;
        }

        Gizmos.color = gizmoColor;
        Bounds bounds = zone.bounds;
        Gizmos.DrawCube(bounds.center, bounds.size);
    }
}
