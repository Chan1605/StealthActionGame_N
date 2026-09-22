using UnityEngine;

public class NormalState : IEnemyState
{
    private ICommand _idleCommand;
    private float _singlePointTimer;

    public void Enter(EnemyStateMachine fsm)
    {
        fsm.Owner.Indicator.Hide();
        fsm.Movement.SetPatrolSpeed(fsm.Data);
        MoveToCurrentWaypoint(fsm);
        _singlePointTimer = Random.Range(0f, fsm.Data.singlePointIdleInterval);
    }

    public void Tick(EnemyStateMachine fsm)
    {
        if (fsm.Perception.TryDetectCorpse(out Vector3 corpsePos, fsm.Data.corpseLayer, fsm.Data.corpseDetectRadius))
        {
            fsm.ChangeState(new AlertState(corpsePos));
            return;
        }

        if (fsm.Perception.MaxScore >= fsm.Data.weakSuspicionThreshold)
        {
            fsm.ChangeState(new WeakSuspicionState());
            return;
        }

        if (fsm.Waypoints.IsSinglePoint)
        {
            TickSinglePointIdle(fsm);
            return;
        }

        if (_idleCommand != null)
        {
            _idleCommand.Tick();
            return;
        }

        if (fsm.Movement.HasArrived())
        {
            if (Random.value < fsm.Data.patrolIdleChance)
            {
                float duration = Random.Range(fsm.Data.patrolIdleMinDuration, fsm.Data.patrolIdleMaxDuration);
                _idleCommand = new LookAroundCommand(fsm.Owner.transform, fsm.Movement, duration, fsm.Data.lookAroundAngle);
                _idleCommand.Start(fsm, () =>
                {
                    _idleCommand = null;
                    AdvanceWaypoint(fsm);
                });
            }
            else
            {
                AdvanceWaypoint(fsm);
            }
        }
    }

    public void Exit(EnemyStateMachine fsm)
    {
        _idleCommand?.Cancel();
        _idleCommand = null;
    }

    private void AdvanceWaypoint(EnemyStateMachine fsm)
    {
        fsm.CurrentWaypointIndex = fsm.Waypoints.GetNextIndex(fsm.CurrentWaypointIndex);
        MoveToCurrentWaypoint(fsm);
    }

    private void MoveToCurrentWaypoint(EnemyStateMachine fsm)
    {
        if (fsm.Waypoints.Count == 0) return;
        fsm.Movement.MoveTo(fsm.Waypoints.GetPosition(fsm.CurrentWaypointIndex));
    }

    private void TickSinglePointIdle(EnemyStateMachine fsm)
    {
        if (_idleCommand != null)
        {
            _idleCommand.Tick();
            return;
        }

        _singlePointTimer += Time.deltaTime;
        if (_singlePointTimer < fsm.Data.singlePointIdleInterval) return;

        _singlePointTimer = 0f;
        float duration = Random.Range(fsm.Data.patrolIdleMinDuration, fsm.Data.patrolIdleMaxDuration);
        _idleCommand = new LookAroundCommand(fsm.Owner.transform, fsm.Movement, duration, fsm.Data.lookAroundAngle);
        _idleCommand.Start(fsm, () => _idleCommand = null);
    }
}