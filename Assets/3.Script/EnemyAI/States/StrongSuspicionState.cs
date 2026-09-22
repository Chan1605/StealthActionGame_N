using UnityEngine;

public class StrongSuspicionState : IEnemyState
{
    private InvestigateCommand _command;

    public void Enter(EnemyStateMachine fsm)
    {
        fsm.Owner.Indicator.ShowStrong();
        fsm.Movement.SetInvestigateSpeed(fsm.Data);

        Vector3 target = fsm.Perception.GetSuspectedPosition();
        float duration = fsm.Perception.IsSoundDominant
            ? fsm.Data.soundInvestigateCheckDuration
            : fsm.Data.investigateCheckDuration;

        _command = new InvestigateCommand(target, duration);
        _command.Start(fsm, () => OnInvestigateComplete(fsm));
    }

    public void Tick(EnemyStateMachine fsm)
    {
        if (fsm.Perception.IsCurrentlySensing)
        {
            fsm.ChangeState(new DetectedState());
            return;
        }

        _command.Tick();
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