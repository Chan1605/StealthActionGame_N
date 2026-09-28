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

    [Header("런 기록 (읽기 전용)")]
    [SerializeField] private int assassinationCount;
    [SerializeField] private float playTime;

    public int assassinationTotal
    {
        get { return assassinationCount; }
    }

    public float playTimeTotal
    {
        get { return playTime; }
    }

    // 엔딩에서 타이머를 멈출 때 끈다.
    public bool isTimerRunning { get; set; } = true;

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

    private void Update()
    {
        // Time.deltaTime은 일시정지(timeScale 0) 동안 0이라 메뉴 시간은 빠진다.
        if (isTimerRunning)
        {
            playTime += Time.deltaTime;
        }
    }

    public void AddAssassination()
    {
        assassinationCount++;
    }

    public void StopTimer()
    {
        isTimerRunning = false;
    }

    /// <summary>새 게임을 시작할 때 호출한다. 이전 판의 기록과 키를 모두 지운다.</summary>
    public void ResetRun()
    {
        assassinationCount = 0;
        playTime = 0f;
        isTimerRunning = true;
        _pendingKeys.Clear();
    }

    /// <summary>mm:ss 형태의 플레이 타임 문자열.</summary>
    public string GetPlayTimeText()
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(playTime));
        return $"{total / 60:00}:{total % 60:00}";
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
