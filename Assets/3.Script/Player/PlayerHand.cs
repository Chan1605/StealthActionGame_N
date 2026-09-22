using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    [Header("Socket")]
    [SerializeField] private Transform handSocket;

    public HoldableItem heldItem { get; private set; }

    public bool isHolding
    {
        get
        {
            return heldItem != null;
        }
    }

    public Transform socket
    {
        get
        {
            return handSocket;
        }
    }

    public event Action<HoldableItem> OnItemHeld;
    public event Action<HoldableItem> OnItemReleased;

    public bool Hold(HoldableItem item)
    {
        if (item == null || item.isHeld || isHolding)
        {
            return false;
        }

        if (handSocket == null)
        {
            Debug.LogWarning("[PlayerHand] Hand Socket이 비어 있습니다. 오른손 본 아래에 빈 오브젝트를 만들어 연결하세요.", this);
            return false;
        }

        heldItem = item;
        item.AttachTo(handSocket);
        OnItemHeld?.Invoke(item);

        return true;
    }

    public HoldableItem Release(Vector3 velocity)
    {
        if (!isHolding)
        {
            return null;
        }

        HoldableItem item = heldItem;
        heldItem = null;

        item.Detach(velocity);
        OnItemReleased?.Invoke(item);

        return item;
    }

    public HoldableItem Drop()
    {
        return Release(Vector3.zero);
    }

    public HoldableItem PutDown(Vector3 position, Quaternion rotation)
    {
        HoldableItem item = Release(Vector3.zero);
        if (item == null)
        {
            return null;
        }

        item.transform.SetPositionAndRotation(position, rotation);

        return item;
    }
}
