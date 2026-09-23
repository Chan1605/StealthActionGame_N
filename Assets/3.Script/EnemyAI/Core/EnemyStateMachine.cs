using UnityEngine;

public class EnemyStateMachine
{
    public EnemyAI Owner { get; }
    public EnemyMovement Movement { get; }
    public EnemyPerception Perception { get; }
    public EnemyAIData Data { get; }
    public WaypointGroup Waypoints { get; }

    public int CurrentWaypointIndex { get; set; }
    public bool SuppressMovementAnim { get; set; }
    public bool IsAssassinable { get; set; } = true;
    private IEnemyState _current;
    public IDamageable DamageTarget { get; set; }

    public EnemyStateMachine(EnemyAI owner, EnemyMovement movement, EnemyPerception perception, EnemyAIData data, WaypointGroup waypoints)
    {
        Owner = owner;
        Movement = movement;
        Perception = perception;
        Data = data;
        Waypoints = waypoints;
    }

    public void ChangeState(IEnemyState next)
    {
        _current?.Exit(this);
        IsAssassinable = true; // 상태 전환마다 기본값으로 리셋, 필요한 상태가 개별적으로 false 설정
        _current = next;
        _current.Enter(this);
    }

    public void Tick()
    {
        Perception.Tick(Time.deltaTime);
        _current?.Tick(this);
        Movement.TickAnimator(SuppressMovementAnim);
    }

    public void ForceReturnToNormal()
    {
        Perception.ForceReset();
        ChangeState(new NormalState());
    }

    public bool TryDetectedByProvocation()
    {
        bool isZonedFreeTime = PrisonScheduleManager.Instance != null
            && PrisonScheduleManager.Instance.IsFreeTime
            && !Perception.IsAlwaysAlert;

        if (!isZonedFreeTime)
        {
            return false; // 예외 대상이 아니면 false로 반환, 호출한 쪽이 원래 로직대로 처리
        }

        Perception.RegisterSuspiciousAction(Data.suspiciousActionCooldown);

        if (Perception.SuspiciousActionCount >= Data.suspiciousActionLimit)
        {
            ChangeState(new DetectedState());
            return true;
        }

        Owner.PlayWarningSound();
        Perception.ReduceScoreSharply();
        return true; // 예외 대상이라 경고로 흡수했으니 호출한 쪽은 더 진행하면 안 됨
    }

#if UNITY_EDITOR
    public string CurrentStateName => _current?.GetType().Name ?? "None";
#endif
}
