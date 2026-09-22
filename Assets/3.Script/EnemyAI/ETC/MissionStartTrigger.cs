using UnityEngine;

public class MissionStartTrigger : MonoBehaviour
{
    [SerializeField] private string missionId = "Mission";
    [SerializeField] private StageManager stageManager;
    [SerializeField] private Collider entranceCollider;

    private HUDManager _hudManager;
    private bool _isCompleted;
    public bool IsCompleted
    {
        get { return _isCompleted; }
    }

    private void Awake()
    {
        if (entranceCollider == null) entranceCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        _hudManager = FindAnyObjectByType<HUDManager>();
        _hudManager.GetTargetMarker(missionId).SetMarker_On(transform);
        MissionSelector.Instance?.Register(this);

        stageManager.OnMissionCompleted += HandleMissionCompleted;
    }

    private void OnDestroy()
    {
        if (stageManager != null) stageManager.OnMissionCompleted -= HandleMissionCompleted;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isCompleted) return;
        if (!other.CompareTag("Player")) return;
        if (MissionSelector.Instance != null && !MissionSelector.Instance.CanStart(this)) return;

        StartMission();
    }

    public void StartMission()
    {
        if (_isCompleted) return;

        if (MissionSelector.Instance != null)
        {
            MissionSelector.Instance.SelectMission(this);
        }

        stageManager.InitializeQueue();
    }

    private void HandleMissionCompleted()
    {
        _isCompleted = true;
        _hudManager.GetTargetMarker(missionId).SetMarker_Off();
        MissionSelector.Instance?.ReleaseMission();
        gameObject.SetActive(false);
    }

    public void LockEntrance()
    {
        if (entranceCollider != null) entranceCollider.enabled = false;
        _hudManager.GetTargetMarker(missionId).SetMarker_Off();
    }

    public void UnlockEntrance()
    {
        if (_isCompleted) return; // 이미 완료된 미션은 다시 안 열림
        if (entranceCollider != null) entranceCollider.enabled = true;
        _hudManager.GetTargetMarker(missionId).SetMarker_On(transform);
    }
}