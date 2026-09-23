using System.Collections.Generic;
using UnityEngine;

// 씬을 넘어가도 유지되는 세션 데이터. (DontDestroyOnLoad)
// 키는 "오브젝트 참조"가 아니라 "id 문자열"로 저장한다.
// 씬에 배치하지 않아도 처음 접근할 때 자동으로 만들어진다.
public class GameSession : MonoBehaviour
{
    private static GameSession _instance;
    private static bool _isQuitting;

    public static GameSession Instance
    {
        get
        {
            if (_instance == null && !_isQuitting)
            {
                GameObject host = new GameObject("[GameSession]");
                _instance = host.AddComponent<GameSession>();
            }

            return _instance;
        }
    }

    private readonly List<string> _pendingKeys = new List<string>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    // 씬을 넘어가기 직전에 호출. 인벤토리의 키 id를 복사해 둔다.
    public void SavePlayerKeys(KeyInventory inventory)
    {
        _pendingKeys.Clear();

        if (inventory != null)
        {
            _pendingKeys.AddRange(inventory.GetKeys());
        }

        Debug.Log($"[GameSession] 키 {_pendingKeys.Count}개 저장: {string.Join(", ", _pendingKeys)}");
    }

    // 저장된 키 id 목록. (지우지 않으므로 씬을 다시 로드해도 복원된다)
    public List<string> GetPendingKeys()
    {
        return new List<string>(_pendingKeys);
    }

    public void ClearPendingKeys()
    {
        _pendingKeys.Clear();
    }
}
