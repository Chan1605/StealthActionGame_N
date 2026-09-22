using UnityEngine;
using UnityEngine.AI;

public class CivilianWander : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float wanderRadius = 6f;
    [SerializeField] private float walkSpeed = 1.2f;
    [SerializeField] private float runSpeed = 3.5f;
    [SerializeField, Range(0f, 1f)] private float runChance = 0.15f;

    [Header("대기")]
    [SerializeField] private float idleMinDuration = 2f;
    [SerializeField] private float idleMaxDuration = 5f;

    [Header("애니메이션")]
    [SerializeField] private Animator animator;

    private NavMeshAgent _agent;
    private Vector3 _centerPosition;
    private float _idleTimer;
    private bool _isIdling;
    private int _animIDSpeed;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _centerPosition = transform.position;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) _animIDSpeed = Animator.StringToHash("Speed");
    }

    private void Start()
    {
        MoveToRandomPoint();
    }

    private void Update()
    {
        TickAnimator();

        if (_isIdling)
        {
            _idleTimer -= Time.deltaTime;
            if (_idleTimer <= 0f)
            {
                _isIdling = false;
                MoveToRandomPoint();
            }
            return;
        }

        if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
        {
            _isIdling = true;
            _idleTimer = Random.Range(idleMinDuration, idleMaxDuration);
        }
    }

    private void MoveToRandomPoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        Vector3 randomPoint = _centerPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            _agent.speed = Random.value < runChance ? runSpeed : walkSpeed;
            _agent.SetDestination(hit.position);
        }
    }

    private void TickAnimator()
    {
        if (animator == null) return;
        if (!HasParameter(_animIDSpeed)) return;
        animator.SetFloat(_animIDSpeed, _agent.velocity.magnitude);
    }

    private bool HasParameter(int hash)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.nameHash == hash) return true;
        }
        return false;
    }
}