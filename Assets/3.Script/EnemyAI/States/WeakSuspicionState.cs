using UnityEngine;

public class WeakSuspicionState : IEnemyState
{
    private LookAroundCommand _lookCommand;
    private bool _resumedMovement;

    public void Enter(EnemyStateMachine fsm)
    {
        fsm.Owner.Indicator.ShowWeak();
        fsm.Movement.Stop();
        _resumedMovement = false;

        bool triggeredBySound = fsm.Perception.IsSoundDominant;
        Vector3 focus = fsm.Perception.GetSuspectedPosition();
        float duration = triggeredBySound
            ? fsm.Data.soundGlanceDuration
            : Random.Range(fsm.Data.patrolIdleMinDuration, fsm.Data.patrolIdleMaxDuration);

        _lookCommand = new LookAroundCommand(fsm.Owner.transform, fsm.Movement, duration, fsm.Data.lookAroundAngle, focus);
        _lookCommand.Start(fsm, () => _lookCommand = null);
    }

    public void Tick(EnemyStateMachine fsm)
    {
        if (fsm.Perception.TryDetectCorpse(out Vector3 corpsePos, fsm.Data.corpseLayer, fsm.Data.corpseDetectRadius))
        {
            fsm.ChangeState(new AlertState(corpsePos));
            return;
        }

        if (fsm.Perception.MaxScore >= fsm.Data.strongSuspicionThreshold)
        {
            HandleConfirmedSuspicion(fsm);
            return;
        }

        if (_lookCommand != null)
        {
            _lookCommand.Tick();
            return;
        }

        if (fsm.Perception.MaxScore < fsm.Data.weakSuspicionThreshold)
        {
            fsm.ChangeState(new NormalState());
            return;
        }

        if (!_resumedMovement)
        {
            _resumedMovement = true;
            if (fsm.Waypoints.Count > 0)
                fsm.Movement.MoveTo(fsm.Waypoints.GetPosition(fsm.CurrentWaypointIndex));
        }
    }

    private void HandleConfirmedSuspicion(EnemyStateMachine fsm)
    {
        bool isZonedFreeTime = PrisonScheduleManager.Instance != null
            && PrisonScheduleManager.Instance.IsFreeTime
            && !fsm.Perception.IsAlwaysAlert;

        if (!isZonedFreeTime)
        {
            fsm.ChangeState(new StrongSuspicionState());
            return;
        }

        fsm.Perception.RegisterSuspiciousAction();

        if (fsm.Perception.SuspiciousActionCount >= fsm.Data.suspiciousActionLimit)
        {
            fsm.ChangeState(new DetectedState()); // 2회째는 확인 절차 없이 곧바로 발견/공격
        }
        else
        {
            fsm.Owner.PlayWarningSound();
            fsm.Perception.ReduceScoreSharply();
        }
    }

    public void Exit(EnemyStateMachine fsm)
    {
        _lookCommand?.Cancel();
        _lookCommand = null;
    }
}