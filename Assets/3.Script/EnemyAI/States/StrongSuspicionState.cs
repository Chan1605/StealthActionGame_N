using UnityEngine;

public class StrongSuspicionState : IEnemyState
{
    private InvestigateCommand _command;
    private Vector3 _investigateTarget;

    public void Enter(EnemyStateMachine fsm)
    {
        fsm.Owner.Indicator.ShowStrong();
        fsm.Movement.SetInvestigateSpeed(fsm.Data);
        fsm.Owner.PlayStrongSuspicionSound();

        _investigateTarget = fsm.Perception.GetSuspectedPosition();
        float duration = fsm.Perception.IsSoundDominant
            ? fsm.Data.soundInvestigateCheckDuration
            : fsm.Data.investigateCheckDuration;

        _command = new InvestigateCommand(_investigateTarget, duration);
        _command.Start(fsm, () => OnInvestigateComplete(fsm));
    }

    public void Tick(EnemyStateMachine fsm)
    {
        if (fsm.Perception.IsCurrentlySensing && IsPlayerNearInvestigateTarget(fsm))
        {
            if (fsm.TryDetectedByProvocation())
            {
                return;
            }

            fsm.ChangeState(new DetectedState());
            return;
        }

        _command.Tick();
    }

    private bool IsPlayerNearInvestigateTarget(EnemyStateMachine fsm)
    {
        if (fsm.Perception.Target == null)
        {
            return false;
        }

        float distance = Vector3.Distance(fsm.Perception.Target.Position, _investigateTarget);
        return distance <= fsm.Data.investigateCatchRadius;
    }

    private void OnInvestigateComplete(EnemyStateMachine fsm)
    {
        fsm.Perception.ReduceScoreSharply();

        if (fsm.Perception.MaxScore >= fsm.Data.weakSuspicionThreshold)
            fsm.ChangeState(new WeakSuspicionState());
        else
            fsm.ChangeState(new NormalState());
    }

    public void Exit(EnemyStateMachine fsm)
    {
        _command?.Cancel();
    }
}