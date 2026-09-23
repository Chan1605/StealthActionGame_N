using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent _agent;
    private Animator _animator;

    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDAttack;
    private int _animIDIdleIndex;

    [Header("Idle 모션 랜덤화")]
    [SerializeField] private int idleClipCount = 1;
    [SerializeField] private float idleLoopSwitchWindow = 0.05f; // 이 구간(시작/끝 근처) 안에서만 전환 허용

    public void Initialize(EnemyAIData data, Animator animator)
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = animator;
        _agent.stoppingDistance = data.stoppingDistance;
        SetPatrolSpeed(data);

        if (_animator != null)
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("IsGrounded");
            _animIDAttack = Animator.StringToHash("Attack");
            _animIDIdleIndex = Animator.StringToHash("IdleIndex");
            if (HasParameter(_animIDSpeed))
            {
                // Speed는 시야/이동에 필수라 없으면 경고만 남기고 넘어감
            }
            else
            {
                Debug.LogWarning($"{name}: Animator에 'Speed' 파라미터가 없습니다. 이동 애니메이션이 재생되지 않을 수 있습니다.");
            }

            if (HasParameter(_animIDGrounded))
            {
                _animator.SetBool(_animIDGrounded, true);
            }
        }
    }

    private bool HasParameter(int hash)
    {
        foreach (AnimatorControllerParameter p in _animator.parameters)
        {
            if (p.nameHash == hash) return true;
        }
        return false;
    }

    public void SetPatrolSpeed(EnemyAIData data) => _agent.speed = data.patrolSpeed;
    public void SetInvestigateSpeed(EnemyAIData data) => _agent.speed = data.investigateSpeed;
    public void SetChaseSpeed(EnemyAIData data) => _agent.speed = data.chaseSpeed;
    public void SetStoppingDistance(float distance) => _agent.stoppingDistance = distance;
    public void SetAutoRotation(bool enabled) => _agent.updateRotation = enabled;

    public void MoveTo(Vector3 destination)
    {
        if (!_agent.isActiveAndEnabled || !_agent.isOnNavMesh) return;

        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
        else
            Debug.LogWarning($"{name}: 목표 지점 근처에서 유효한 NavMesh를 찾지 못했습니다. ({destination})");
    }

    public void Warp(Vector3 position)
    {
        _agent.Warp(position);
    }

    public bool IsReachable(Vector3 destination)
    {
        if (!_agent.isActiveAndEnabled || !_agent.isOnNavMesh) return false;

        NavMeshPath path = new NavMeshPath();
        if (!_agent.CalculatePath(destination, path)) return false;

        return path.status == NavMeshPathStatus.PathComplete;
    }

    public bool HasArrived()
    {
        if (!_agent.isActiveAndEnabled || !_agent.isOnNavMesh) return true;
        if (_agent.pathPending) return false;
        return _agent.remainingDistance <= _agent.stoppingDistance;
    }
    public void PlayAttackAnimation()
    {
        if (_animator == null) return;
        if (!HasParameter(_animIDAttack)) return;
        _animator.SetTrigger(_animIDAttack);
    }

    public void Stop()
    {
        if (_agent.isActiveAndEnabled && _agent.isOnNavMesh) _agent.ResetPath();
    }
    public void TickAnimator(bool suppress = false)
    {
        if (_animator == null) return;
        if (!HasParameter(_animIDSpeed)) return;
        _animator.SetFloat(_animIDSpeed, suppress ? 0f : _agent.velocity.magnitude);
    }

    // 대기(Idle) 모션을 여러 개 중 랜덤으로 고른다. 루프 경계에서만 호출되므로 즉시 세팅해도 어색하지 않다.
    public void RandomizeIdle()
    {
        if (_animator == null || idleClipCount <= 1) return;
        if (!HasParameter(_animIDIdleIndex)) return;
        _animator.SetFloat(_animIDIdleIndex, Random.Range(0, idleClipCount));
    }

    // Idle 인덱스를 특정 값으로 고정한다. LookAround처럼 회전과 겹치는 상황에서 사용.
    public void SetIdleIndex(int index)
    {
        if (_animator == null) return;
        if (!HasParameter(_animIDIdleIndex)) return;
        _animator.SetFloat(_animIDIdleIndex, index);
    }

    // 지금이 현재 재생 중인 Idle 클립의 루프 경계(막 끝났거나 막 시작한 지점)인지 확인한다.
    // 특수행동(LookAround 등) 시작을 이 경계까지 미뤄서 클립이 도중에 끊기지 않게 하는 용도.
    public bool IsIdleAtLoopBoundary()
    {
        if (_animator == null) return true;

        float t = _animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f;
        return t <= idleLoopSwitchWindow || t >= 1f - idleLoopSwitchWindow;
    }
}
