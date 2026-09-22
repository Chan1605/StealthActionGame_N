using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    private KeyInventory _pendingKeys;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SavePlayerKeys(KeyInventory inventory)
    {
        _pendingKeys = inventory;
    }

    public KeyInventory TakePendingKeys()
    {
        KeyInventory result = _pendingKeys;
        _pendingKeys = null;
        return result;
    }
}