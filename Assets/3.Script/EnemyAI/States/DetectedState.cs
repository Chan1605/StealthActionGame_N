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

        // 길이 막혔어도(난간 등) 여전히 보이거나 들리는 상태라면
        // "놓쳤다"가 아니라 "막혀서 못 가지만 알고는 있다"로 취급한다.
        bool isBlockedButAware = _chase.IsTargetUnreachable && fsm.Perception.IsCurrentlySensing;

        if (isBlockedButAware)
        {
            _loseTimer = 0f;
            return;   // 공격 판정도 스킵. 대부분 사거리 밖이라 의미 없다.
        }

        if (_chase.IsTargetUnreachable)
        {
            _loseTimer += fsm.Data.detectedLoseTime; // 안 보이고 길도 없으면 기존처럼 즉시 놓친 것으로 처리
        }
        TickAttack(fsm);

        if (fsm.Perception.MaxScore >= fsm.Data.strongSuspicionThreshold)
        {
            _loseTimer = 0f;
            return;
        }

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