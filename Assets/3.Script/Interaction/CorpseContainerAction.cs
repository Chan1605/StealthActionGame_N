using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorpseContainerAction : InteractionAction
{
    [Header("Container")]
    [SerializeField] private int capacity = 1;

    [Header("Debug")]
    [SerializeField] private bool isDebugLog = true;

    private int _storedCount;

    public int storedCount
    {
        get
        {
            return _storedCount;
        }
    }

    public int freeSlots
    {
        get
        {
            return Mathf.Max(0, capacity - _storedCount);
        }
    }

    public bool isFull
    {
        get
        {
            return _storedCount >= capacity;
        }
    }

    // 시체를 든 채로 쓰는 것이 이 액션의 목적이다.
    public override bool isAllowedWhileCarrying
    {
        get
        {
            return true;
        }
    }

    public event Action<CarriableBody> OnCorpseStored;
    public event Action OnContainerFilled;

    protected override bool CanExecute(Transform user)
    {
        if (isFull)
        {
            Log("가득 찼습니다.");
            return false;
        }

        CarrySystem carry = user.GetComponent<CarrySystem>();

        if (carry == null || !carry.isCarrying)
        {
            return false;
        }

        return true;
    }

    protected override void OnExecute(Transform user)
    {
        CarrySystem carry = user.GetComponent<CarrySystem>();

        if (carry == null)
        {
            return;
        }

        CarriableBody body = carry.ReleaseForDispose();

        if (body == null)
        {
            return;
        }

        body.Dispose();

        _storedCount++;

        Log($"{_storedCount}/{capacity} 처리 완료");

        OnCorpseStored?.Invoke(body);

        if (isFull)
        {
            OnContainerFilled?.Invoke();
        }
    }

    private void Log(string message)
    {
        if (isDebugLog)
        {
            Debug.Log($"[CorpseContainer] {name} - {message}", this);
        }
    }
}
