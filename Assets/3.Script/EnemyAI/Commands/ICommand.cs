using System;

public interface ICommand
{
    void Start(EnemyStateMachine fsm, Action onComplete);
    void Tick();
    void Cancel();
}