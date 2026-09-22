using UnityEngine;
using System.Linq;
using UnityEngine.AI;
using FMODUnity;

[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyPerception))]
public class EnemyAI : MonoBehaviour
{
    [Header("데이터")]
    [SerializeField] private EnemyAIData data;
    [SerializeField] private WaypointGroup waypoints;

    [Header("감지 대상")]
    [SerializeField] private MonoBehaviour targetObject; // IDetectable을 구현한 컴포넌트를 드래그

    [Header("연출")]
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyIndicator indicator;
    [SerializeField] private EventReference SE_Warning;

    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;
    public EnemyIndicator Indicator => indicator;
    [Header("자유시간 예외")]
    [SerializeField] private bool isAlwaysAlert; // 방 지키는 교도관 등

    private EnemyMovement _movement;
    private EnemyPerception _perception;
    private EnemyStateMachine _fsm;
    private IndicatorManager _indicatorManager;
    private MinimapManager _mini;
    public bool CanBeAssassinated => _fsm == null || _fsm.IsAssassinable;

    private void Awake()
    {
        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;
        _movement = GetComponent<EnemyMovement>();
        _movement.Initialize(data, animator);

        _perception = GetComponent<EnemyPerception>();
        _perception.Initialize(data, targetObject as IDetectable);
        _perception.IsAlwaysAlert = isAlwaysAlert;
        _fsm = new EnemyStateMachine(this, _movement, _perception, data, waypoints);
        _fsm.DamageTarget = targetObject != null ? targetObject.GetComponent<IDamageable>() : null;
        if (TryGetComponent(out TakedownVictim victim))
        {
            victim.OnFrozen += HandleFrozen;
        }
        _indicatorManager = FindAnyObjectByType<IndicatorManager>();
        _indicatorManager?.RegisterEnemy(transform);
        _mini = FindAnyObjectByType<MinimapManager>();
        _mini?.RegisterEnemy(transform);
        if (_fsm.DamageTarget == null)
            Debug.LogWarning($"{name}: targetObject에서 IDamageable을 찾지 못했습니다. 발견 상태 공격이 비활성화됩니다.");
    }

    private void Start()
    {
        _fsm.ChangeState(new NormalState());
        if (PrisonScheduleManager.Instance != null)
        {
            PrisonScheduleManager.Instance.OnScheduleChanged += HandleScheduleChanged;
        }
    }

    private void HandleScheduleChanged(bool isFreeTime)
    {
        if (isFreeTime)
        {
            _fsm?.ForceReturnToNormal();
        }
    }

    private void Update()
    {
        _fsm.Tick();
    }

    public void ForceForgetPlayer()
    {
        _fsm?.ForceReturnToNormal();
    }

    private void HandleFrozen(TakedownVictim victim)
    {
        indicator.Hide();
        _indicatorManager?.UnregisterEnemy(transform);
        _mini?.RegisterEnemy(transform);
        enabled = false;
    }

    private void OnDestroy()
    {
        if (TryGetComponent(out TakedownVictim victim))
        {
            victim.OnFrozen -= HandleFrozen;
        }

        _indicatorManager?.UnregisterEnemy(transform);
        _mini?.UnregisterEnemy(transform);
        if (PrisonScheduleManager.Instance != null)
        {
            PrisonScheduleManager.Instance.OnScheduleChanged -= HandleScheduleChanged;
        }
    }
    public void ReviveForCheckpoint()
    {
        if (TryGetComponent(out TakedownVictim victim))
        {
            victim.Revive();
        }

        if (TryGetComponent(out NavMeshAgent agent))
        {
            agent.enabled = true;
            agent.Warp(_spawnPosition);
        }
        transform.rotation = _spawnRotation;

        indicator.Hide();
        _indicatorManager?.RegisterEnemy(transform);

        enabled = true;
        _fsm.CurrentWaypointIndex = 0;
        _fsm.ForceReturnToNormal();
    }

    public void PlayWarningSound()
    {
        // TODO: FMOD 이벤트 재생 연결
        if (!SE_Warning.IsNull)
        {
            AudioManager.Instance?.PlayOneShot(SE_Warning, transform.position);
        }
    }

#if UNITY_EDITOR
    [Header("디버그 (읽기 전용)")]
    [SerializeField, TextArea] private string debugCurrentState;
    [SerializeField] private float debugMaxScore;

    private void OnValidate()
    {
        if (targetObject == null) return;
        if (targetObject is IDetectable) return;

        var candidate = targetObject.GetComponents<MonoBehaviour>().FirstOrDefault(m => m is IDetectable);
        if (candidate != null)
        {
            Debug.LogWarning($"{name}: targetObject가 IDetectable이 아니라 같은 오브젝트의 '{candidate.GetType().Name}'로 자동 교체했습니다.");
            targetObject = candidate;
        }
        else
        {
            Debug.LogWarning($"{name}: targetObject의 오브젝트에서 IDetectable을 구현한 컴포넌트를 찾지 못했습니다.");
        }
    }

    private void LateUpdate()
    {
        if (_fsm == null) return;
        debugCurrentState = _fsm.CurrentStateName;
        debugMaxScore = _fsm.Perception.MaxScore;
    }
#endif
}
