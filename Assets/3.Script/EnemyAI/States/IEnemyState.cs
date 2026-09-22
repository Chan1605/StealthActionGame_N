
// 5가지 적 AI 상태(활동 전 / 일반 활동 / 약한 의심 / 강한 의심 / 발견)
// 실제 실행은 EnemyMovement / Command에 위임한다.

public interface IEnemyState
{
    void Enter(EnemyStateMachine fsm);
    void Tick(EnemyStateMachine fsm);
    void Exit(EnemyStateMachine fsm);
}
