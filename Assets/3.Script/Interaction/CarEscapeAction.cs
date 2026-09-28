using UnityEngine;

// 마지막 목표인 차량에 붙인다. 상호작용하면 엔딩 화면을 띄운다.
// 조작을 막지 않으므로 isHoldingPlayer는 기본값(false) 그대로 둔다.
public class CarEscapeAction : InteractionAction
{
    [Header("Ending")]
    [SerializeField] private EndingSequence ending;

    protected override void OnExecute(Transform user)
    {
        if (ending == null)
        {
            ending = EndingSequence.Instance != null
                ? EndingSequence.Instance
                : FindAnyObjectByType<EndingSequence>();
        }

        if (ending == null)
        {
            Debug.LogError("[CarEscape] 씬에서 EndingSequence를 찾지 못했습니다.", this);
            return;
        }

        ending.Play();
    }
}
