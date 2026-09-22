using System;
using UnityEngine;

public class AttackCommand : ICommand
{
    private enum Phase { Windup, Recovery, Done }

    private Phase _phase;
    private float _timer;
    private EnemyStateMachine _fsm;
    private Action _onComplete;

    private Quaternion _startRotation;
    private Quaternion _targetRotation;

    public void Start(EnemyStateMachine fsm, Action onComplete)
    {
        _fsm = fsm;
        _onComplete = onComplete;
        _phase = Phase.Windup;
        _timer = 0f;

        fsm.Movement.Stop();
        fsm.Movement.SetAutoRotation(false);
        fsm.SuppressMovementAnim = true;

        _startRotation = fsm.Owner.transform.rotation;
        _targetRotation = _startRotation;

        if (fsm.Perception.Target != null)
        {
            Vector3 flat = fsm.Perception.Target.Position - fsm.Owner.transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.001f)
            {
                _targetRotation = Quaternion.LookRotation(flat);
            }
        }

        fsm.Movement.PlayAttackAnimation();
    }

    public void Tick()
    {
        _timer += Time.deltaTime;

        switch (_phase)
        {
            case Phase.Windup:
                float t = Mathf.Clamp01(_timer / Mathf.Max(0.01f, _fsm.Data.attackWindupTime));
                _fsm.Owner.transform.rotation = Quaternion.Slerp(_startRotation, _targetRotation, t);

                if (_timer >= _fsm.Data.attackWindupTime)
                {
                    _fsm.DamageTarget?.TakeDamage(_fsm.Data.attackDamage);
                    _phase = Phase.Recovery;
                    _timer = 0f;
                }
                break;

            case Phase.Recovery:
                if (_timer >= _fsm.Data.attackRecoveryTime)
                {
                    _phase = Phase.Done;
                    _fsm.Movement.SetAutoRotation(true);
                    _onComplete?.Invoke();
                }
                break;
        }
    }

    public void Cancel()
    {
        _fsm.Movement.SetAutoRotation(true);
        _fsm.SuppressMovementAnim = false;
        _phase = Phase.Done;
    }
}