using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpAction : InteractionAction
{
    [Header("PickUp")]
    [SerializeField] private HoldableItem holdable;
    [SerializeField] private string itemId;

    public string id
    {
        get
        {
            return itemId;
        }
    }

    public HoldableItem item
    {
        get
        {
            return holdable;
        }
    }

    public event Action<PickUpAction> OnPickedUp;

    protected override void Awake()
    {
        base.Awake();

        if (holdable == null)
        {
            TryGetComponent(out holdable);
        }

        if (holdable == null)
        {
            Debug.LogWarning("[PickUpAction] HoldableItem이 없습니다. 같은 오브젝트에 추가하거나 슬롯에 연결하세요.", this);
        }
    }

    protected override bool CanExecute(Transform user)
    {
        if (holdable == null || holdable.isHeld)
        {
            return false;
        }

        PlayerHand hand = FindHand(user);

        return hand != null && !hand.isHolding;
    }

    protected override void OnExecute(Transform user)
    {
        PlayerHand hand = FindHand(user);
        if (hand == null)
        {
            return;
        }

        if (!hand.Hold(holdable))
        {
            return;
        }

        OnPickedUp?.Invoke(this);
    }

    private PlayerHand FindHand(Transform user)
    {
        if (user == null)
        {
            return null;
        }

        return user.GetComponentInChildren<PlayerHand>();
    }
}
