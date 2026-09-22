using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrownItemDespawn : MonoBehaviour
{
    [Header("Despawn")]
    [SerializeField] private float delay = 2f;
    [SerializeField] private LayerMask landingMask = ~0;
    [SerializeField] private float minImpactSpeed = 0.5f;
    [SerializeField] private bool isDestroyOnFinish = true;

    private HoldableItem _item;
    private Coroutine _routine;

    public bool isArmed { get; private set; }

    public bool isCountingDown
    {
        get
        {
            return _routine != null;
        }
    }

    public event Action<ThrownItemDespawn> OnLanded;
    public event Action<ThrownItemDespawn> OnDespawned;

    private void Awake()
    {
        TryGetComponent(out _item);
    }

    private void OnEnable()
    {
        if (_item != null)
        {
            _item.OnHeld += HandleHeld;
        }
    }

    private void OnDisable()
    {
        if (_item != null)
        {
            _item.OnHeld -= HandleHeld;
        }
    }

    private void HandleHeld(HoldableItem item)
    {
        Cancel();
    }

    public void Arm(float despawnDelay, LayerMask mask)
    {
        delay = despawnDelay;
        landingMask = mask;

        Arm();
    }

    public void Arm()
    {
        Cancel();

        isArmed = true;
    }

    public void Cancel()
    {
        isArmed = false;

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isArmed)
        {
            return;
        }

        if ((landingMask.value & (1 << collision.gameObject.layer)) == 0)
        {
            return;
        }

        if (collision.relativeVelocity.magnitude < minImpactSpeed)
        {
            return;
        }

        isArmed = false;

        OnLanded?.Invoke(this);

        _routine = StartCoroutine(Despawn_co());
    }

    private IEnumerator Despawn_co()
    {
        yield return new WaitForSeconds(delay);

        _routine = null;

        OnDespawned?.Invoke(this);

        if (isDestroyOnFinish)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
