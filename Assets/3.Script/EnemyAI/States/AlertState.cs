using UnityEngine;

public class AlertState : IEnemyState
{
    private float _timer;
    private LookAroundCommand _lookCommand;
    private readonly Vector3 _corpsePosition;

    public AlertState(Vector3 corpsePosition)
    {
        _corpsePosition = corpsePosition;
    }

    public void Enter(EnemyStateMachine fsm)
    {
        fsm.Owner.Indicator.ShowStrong();
        fsm.Movement.Stop();
        _timer = 0f;

        _lookCommand = new LookAroundCommand(fsm.Owner.transform, fsm.Movement, fsm.Data.corpseAlertDuration, fsm.Data.lookAroundAngle, _corpsePosition);
        _lookCommand.Start(fsm, () => _lookCommand = null);
    }

    public void Tick(EnemyStateMachine fsm)
    {
        if (fsm.Perception.IsCurrentlySensing)
        {
            fsm.ChangeState(new DetectedState());
            return;
        }

        if (fsm.Perception.MaxScore >= fsm.Data.strongSuspicionThreshold)
        {
            fsm.ChangeState(new StrongSuspicionState());
            return;
        }

        _timer += Time.deltaTime;
        _lookCommand?.Tick();

        if (_timer >= fsm.Data.corpseAlertDuration)
        {
            fsm.ChangeState(new NormalState());
        }
    }

    public void Exit(EnemyStateMachine fsm)
    {
        _lookCommand?.Cancel();
    }
}