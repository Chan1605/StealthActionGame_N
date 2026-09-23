using UnityEngine;

public class WeakSuspicionState : IEnemyState
{
    private LookAroundCommand _lookCommand;
    private bool _resumedMovement;

    public void Enter(EnemyStateMachine fsm)
    {
        fsm.Owner.Indicator.ShowWeak();
        fsm.Owner.PlayWeakSuspicionSound();
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
        if (fsm.Perception.IsAlarmSource)
        {
            // 경보기 같은 환경음은 개인 도발이 아니므로, 예외 대상이라도 일단 수색하러 보낸다.
            fsm.ChangeState(new StrongSuspicionState());
            return;
        }

        if (fsm.TryDetectedByProvocation())
        {
            return;
        }

        fsm.ChangeState(new StrongSuspicionState());
    }

    public void Exit(EnemyStateMachine fsm)
    {
        _lookCommand?.Cancel();
        _lookCommand = null;
    }
}