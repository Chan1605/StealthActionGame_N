using UnityEngine;

public class DetectedState : IEnemyState
{
    private float _loseTimer;
    private ChaseCommand _chase;
    private AttackCommand _attack;

    public void Enter(EnemyStateMachine fsm)
    {
        fsm.IsAssassinable = false;
        fsm.Owner.Indicator.ShowDetected();
        fsm.Owner.PlayDetectedSound();
        fsm.Movement.SetStoppingDistance(fsm.Data.attackRange);

        _loseTimer = 0f;
        _chase = new ChaseCommand(fsm.Perception, fsm.Data.chaseRepathInterval);
        _chase.Start(fsm, null);
    }

    public void Tick(EnemyStateMachine fsm)
    {
        if (_attack != null)
        {
            _attack.Tick();
            return;
        }

        _chase.Tick();
        if (_chase.IsTargetUnreachable)
        {
            _loseTimer += fsm.Data.detectedLoseTime; // 강제로 포기 타이머를 즉시 만료시켜 바로 복귀 판정으로
        }
        TickAttack(fsm);

        if (fsm.Perception.MaxScore >= fsm.Data.strongSuspicionThreshold)
        {
            _loseTimer = 0f;
            return;
        }
        fsm.Owner.PlayLostPlayerSound();
        _loseTimer += Time.deltaTime;
        if (_loseTimer < fsm.Data.detectedLoseTime) return;

        if (fsm.Perception.MaxScore >= fsm.Data.weakSuspicionThreshold)
            fsm.ChangeState(new WeakSuspicionState());
        else
            fsm.ChangeState(new NormalState());
    }

    private void TickAttack(EnemyStateMachine fsm)
    {
        if (fsm.Perception.Target == null) return;

        float distance = Vector3.Distance(fsm.Owner.transform.position, fsm.Perception.Target.Position);
        if (distance > fsm.Data.attackRange) return;

        _attack = new AttackCommand();
        _attack.Start(fsm, () =>
        {
            fsm.SuppressMovementAnim = false;
            _attack = null;
        });
    }

    public void Exit(EnemyStateMachine fsm)
    {
        _attack?.Cancel();
        _chase.Cancel();
        fsm.SuppressMovementAnim = false;
        fsm.Movement.SetAutoRotation(true);
        fsm.Movement.SetStoppingDistance(fsm.Data.stoppingDistance);
    }
}