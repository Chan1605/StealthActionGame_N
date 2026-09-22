using System;
using UnityEngine;

public class InvestigateCommand : ICommand
{
    private enum Phase { Moving, Checking, Done }

    private readonly Vector3 _targetPos;
    private readonly float _checkDuration;
    private EnemyStateMachine _fsm;
    private Action _onComplete;
    private Phase _phase;
    private LookAroundCommand _lookCommand;

    public InvestigateCommand(Vector3 targetPos, float checkDuration)
    {
        _targetPos = targetPos;
        _checkDuration = checkDuration;
    }

    public void Start(EnemyStateMachine fsm, Action onComplete)
    {
        _fsm = fsm;
        _onComplete = onComplete;
        _phase = Phase.Moving;
        fsm.Movement.MoveTo(_targetPos);
    }

    public void Tick()
    {
        switch (_phase)
        {
            case Phase.Moving:
                if (_fsm.Movement.HasArrived())
                {
                    _phase = Phase.Checking;
                    _lookCommand = new LookAroundCommand(_fsm.Owner.transform, _fsm.Movement, _checkDuration, _fsm.Data.lookAroundAngle, _targetPos);
                    _lookCommand.Start(_fsm, () =>
                    {
                        _phase = Phase.Done;
                        _onComplete?.Invoke();
                    });
                }
                break;

            case Phase.Checking:
                _lookCommand.Tick();
                break;
        }
    }

    public void Cancel()
    {
        _lookCommand?.Cancel();
        _phase = Phase.Done;
    }
}