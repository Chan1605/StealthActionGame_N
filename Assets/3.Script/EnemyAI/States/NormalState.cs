using System;
using UnityEngine;

public class NormalState : IEnemyState
{
    private ICommand _idleCommand;
    private float _singlePointTimer;
    private bool _lookAroundPending;
    private float _pendingLookAroundDuration;

    public void Enter(EnemyStateMachine fsm)
    {
        fsm.Owner.Indicator.Hide();
        fsm.Movement.SetPatrolSpeed(fsm.Data);
        MoveToCurrentWaypoint(fsm);
        _singlePointTimer = UnityEngine.Random.Range(0f, fsm.Data.singlePointIdleInterval);
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

        if (_lookAroundPending)
        {
            TryStartPendingLookAround(fsm, () =>
            {
                _idleCommand = null;
                AdvanceWaypoint(fsm);
            });
            return;
        }

        if (fsm.Movement.HasArrived())
        {
            if (UnityEngine.Random.value < fsm.Data.patrolIdleChance)
            {
                _pendingLookAroundDuration = UnityEngine.Random.Range(fsm.Data.patrolIdleMinDuration, fsm.Data.patrolIdleMaxDuration);
                _lookAroundPending = true;
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
        _lookAroundPending = false;
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

        if (_lookAroundPending)
        {
            TryStartPendingLookAround(fsm, () => _idleCommand = null);
            return;
        }

        _singlePointTimer += Time.deltaTime;
        if (_singlePointTimer < fsm.Data.singlePointIdleInterval) return;

        _singlePointTimer = 0f;
        _pendingLookAroundDuration = UnityEngine.Random.Range(fsm.Data.patrolIdleMinDuration, fsm.Data.patrolIdleMaxDuration);
        _lookAroundPending = true;
    }

    // Idle 애니메이션이 루프 경계(막 끝났거나 막 시작한 지점)에 왔을 때만 실제로 LookAroundCommand를 시작한다.
    // 재생 중인 클립이 도중에 뚝 끊기지 않도록 하기 위함.
    private void TryStartPendingLookAround(EnemyStateMachine fsm, Action onComplete)
    {
        if (!fsm.Movement.IsIdleAtLoopBoundary()) return;

        _lookAroundPending = false;
        _idleCommand = new LookAroundCommand(fsm.Owner.transform, fsm.Movement, _pendingLookAroundDuration, fsm.Data.lookAroundAngle);
        _idleCommand.Start(fsm, onComplete);
    }
}
