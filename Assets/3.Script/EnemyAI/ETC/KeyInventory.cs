using System.Collections.Generic;
using UnityEngine;

public class KeyInventory : MonoBehaviour
{
    private readonly HashSet<string> _keys = new HashSet<string>();

    [Header("디버그용 (읽기 전용)")]
    [SerializeField] private List<string> debugHeldKeys = new List<string>();

    [Header("복원")]
    [SerializeField] private bool isRestoreOnStart = true;

    private void Start()
    {
        if (!isRestoreOnStart)
        {
            return;
        }

        // 이전 씬에서 넘어온 키를 복원한다.
        RestoreFrom(GameSession.Instance.GetPendingKeys());
    }

    public void AddKey(string keyId)
    {
        if (string.IsNullOrEmpty(keyId))
        {
            return;
        }

        if (!_keys.Add(keyId))
        {
            return;
        }

        debugHeldKeys.Add(keyId);
        Debug.Log($"[KeyInventory] 키 획득: {keyId} (현재 {_keys.Count}개)");
    }

    public bool HasKey(string keyId)
    {
        return _keys.Contains(keyId);
    }

    public List<string> GetKeys()
    {
        return new List<string>(_keys);
    }

    public void RestoreFrom(IEnumerable<string> keyIds)
    {
        if (keyIds == null)
        {
            return;
        }

        foreach (string keyId in keyIds)
        {
            AddKey(keyId);
        }
    }

    public void RestoreFrom(KeyInventory other)
    {
        if (other == null)
        {
            return;
        }

        RestoreFrom(other.GetKeys());
    }
}
