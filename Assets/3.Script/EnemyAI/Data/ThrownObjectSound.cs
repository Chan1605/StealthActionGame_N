using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ThrownObjectSound : MonoBehaviour
{
    [SerializeField] private float soundIntensity = 90f;
    [SerializeField] private float soundRadius = 25f;
    [SerializeField] private LayerMask surfaceMask = ~0;
    [SerializeField] private float armDelayAfterRelease = 0.15f;

    private bool _hasTriggered;
    private bool _armed = true;
    private HoldableItem _holdable;
    private bool _hasBeenThrownAtLeastOnce;

    private void Awake()
    {
        TryGetComponent(out _holdable);
    }

    private void OnEnable()
    {
        if (_holdable != null)
        {
            _armed = false;
            _holdable.OnHeld += HandleHeld;
            _holdable.OnReleased += HandleReleased;
        }
        else
        {
            _armed = true;
        }
    }

    private void OnDisable()
    {
        if (_holdable != null)
        {
            _holdable.OnHeld -= HandleHeld;
            _holdable.OnReleased -= HandleReleased;
        }
    }

    private void HandleHeld(HoldableItem item)
    {
        _armed = false;
    }

    private void HandleReleased(HoldableItem item)
    {
        bool wasThrown = item.itemBody != null && item.itemBody.linearVelocity.sqrMagnitude > 0.01f;

        if (!wasThrown)
        {
            _armed = false; // 그냥 내려놓았으면 계속 무장 해제 상태 유지
            return;
        }
        _hasBeenThrownAtLeastOnce = true;
        _hasTriggered = false;
        CancelInvoke(nameof(Arm));
        Invoke(nameof(Arm), armDelayAfterRelease);
    }

    private void Arm()
    {
        _armed = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryRegister(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryRegister(other.gameObject);
    }

    private void TryRegister(GameObject other)
    {
        if (!_hasBeenThrownAtLeastOnce && _holdable != null) return; // 한 번도 안 던져졌으면 씬 시작 낙하는 무시
        if (!_armed || _hasTriggered) return;
        if ((surfaceMask.value & (1 << other.layer)) == 0) return;
        _hasTriggered = true;

        EnemyPerception[] enemies = FindObjectsByType<EnemyPerception>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.RegisterSound(transform.position, soundIntensity, true, soundRadius);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, soundRadius);
    }
}