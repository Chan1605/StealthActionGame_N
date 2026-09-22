using System;
using UnityEngine;

public class ChaseCommand : ICommand
{
    private readonly EnemyPerception _perception;
    private readonly float _repathInterval;
    private EnemyStateMachine _fsm;
    private float _timer;
    private float _unreachableTimer;

    public bool IsTargetUnreachable { get; private set; }

    public ChaseCommand(EnemyPerception perception, float repathInterval)
    {
        _perception = perception;
        _repathInterval = repathInterval;
    }

    public void Start(EnemyStateMachine fsm, Action onComplete)
    {
        _fsm = fsm;
        fsm.Movement.SetChaseSpeed(fsm.Data);
        _timer = 0f;
        _unreachableTimer = 0f;
        IsTargetUnreachable = false;
        MoveToTarget();
    }

    public void Tick()
    {
        _timer += Time.deltaTime;
        if (_timer < _repathInterval) return;
        _timer = 0f;
        MoveToTarget();
    }

    private void MoveToTarget()
    {
        if (_perception.Target == null) return;

        Vector3 targetPos = _perception.Target.Position;

        if (_fsm.Movement.IsReachable(targetPos))
        {
            _unreachableTimer = 0f;
            IsTargetUnreachable = false;
            _fsm.Movement.MoveTo(targetPos);
        }
        else
        {
            _unreachableTimer += _repathInterval;
            if (_unreachableTimer >= _fsm.Data.unreachableGiveUpTime)
            {
                IsTargetUnreachable = true;
            }
        }
    }

    public void Cancel() { }
}