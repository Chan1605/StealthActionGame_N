// "이 타겟은 이미 완료됐는가"를 알려주는 인터페이스.
// 열쇠 획득, 적 암살처럼 1회성 타겟에만 구현한다.
// 구현하지 않은 타겟은 기존처럼 동작한다. (StageManager가 차례가 된 타겟만 검사)
public interface ICompletionState
{
    bool IsCompleted { get; }
}
