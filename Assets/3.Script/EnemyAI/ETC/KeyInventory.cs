using System.Collections.Generic;
using UnityEngine;

public class KeyInventory : MonoBehaviour
{
    private readonly HashSet<string> _keys = new HashSet<string>();

    [Header("디버그 (읽기 전용)")]
    [SerializeField] private List<string> debugHeldKeys = new List<string>();

    public void AddKey(string keyId)
    {
        _keys.Add(keyId);
        debugHeldKeys.Add(keyId);
        Debug.Log($"[KeyInventory] 키 획득: {keyId} (보유 {_keys.Count}개)");
    }

    public bool HasKey(string keyId)
    {
        return _keys.Contains(keyId);
    }

    public void RestoreFrom(KeyInventory other)
    {
        if (other == null) return;
        foreach (string keyId in other.debugHeldKeys)
        {
            AddKey(keyId);
        }
    }
}