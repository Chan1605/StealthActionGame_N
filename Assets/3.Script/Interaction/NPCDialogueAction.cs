using UnityEngine;

public class NPCDialogueAction : InteractionAction
{
    [Header("Debug")]
    [SerializeField] private bool isDebugLog = false;

    private NPCInteractionObject _npc;

    protected override void Awake()
    {
        base.Awake();
        TryGetComponent(out _npc);

        if (_npc == null)
        {
            Debug.LogWarning($"[NPCDialogueAction] '{name}'에 NPCInteractionObject가 없습니다.", this);
        }
    }

    protected override void OnExecute(Transform user)
    {
        if (_npc == null)
        {
            return;
        }

        _npc.StartDialogue();

        if (isDebugLog)
        {
            Debug.Log($"[NPCDialogueAction] '{name}' 대화 실행", this);
        }
    }
}
