using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("주요 오브젝트")]
    [Tooltip("레벨에 따라 상호작용하는 오브젝트를 순서대로 리스트에 넣어주세요.")]
    [SerializeField] private List<GameObject> targetObjectList;

    private Queue<IInteractable> targetObjectQueue = new Queue<IInteractable>();

    private HUDManager hudManager;
    private MinimapManager minimap;
    [Header("미션 식별")]
    [SerializeField] private string missionId = "Mission";
    [Header("UI")]
    [SerializeField] private string missionSelectPrompt = "미션을 선택하세요";
    [SerializeField] private string missionCompletePrompt = "미션 완료";
    [Header("UI 설명 (targetObjectList와 순서 1:1 대응)")]
    [SerializeField] private List<string> targetDescriptions;
    [Header("완료 시 활성화할 오브젝트 (예: 다음 미션 키)")]
    [SerializeField] private GameObject[] unlockOnComplete;

    public event Action OnMissionCompleted;
    public bool isMissionCompleted { get; private set; }
    public bool HasStarted { get; private set; }
    private UI_MissionObjective objectiveUI;
    private Queue<string> descriptionQueue = new Queue<string>();

    private void Awake()
    {
        foreach (GameObject obj in unlockOnComplete)
        {
            if (obj != null) obj.SetActive(false);
        }
    }
    private void Start()
    {
        hudManager = FindAnyObjectByType<HUDManager>();
        minimap = FindAnyObjectByType<MinimapManager>();
        objectiveUI = hudManager.GetObjective();
    }

    public IInteractable GetCurrentTarget()
    {
        if (targetObjectQueue.Count > 0)
        {
            return targetObjectQueue.Peek();
        }

        return null;
    }

    public void InitializeQueue()
    {
        // 이미 시작된 미션이면 무시
        // 입구 트리거에 재진입해도 큐/이벤트가 중복되지 않게 막는다.
        if (HasStarted)
        {
            return;
        }
        HasStarted = true;

        for (int i = 0; i < targetObjectList.Count; i++)
        {
            GameObject targetObj = targetObjectList[i];

            // 인스펙터 리스트의 빈 슬롯(None)이거나 이미 파괴된 오브젝트는 큐에서 제외한다.
            if (targetObj == null)
            {
                Debug.LogWarning($"StageManager : targetObjectList[{i}]가 비어 있거나 이미 파괴됐습니다.");
                continue;
            }

            // GetComponentInChildren은 자기 자신도 검사한다. true = 비활성 오브젝트 포함.
            IInteractable t = targetObj.GetComponentInChildren<IInteractable>(true);

            if (t == null)
            {
                Debug.LogWarning($"StageManager : {targetObj.name}에서 IInteractable을 찾지 못했습니다.");
                continue;
            }

            // 여기서는 구독하지 않는다. 차례가 된 타겟만 구독한다. (ProceedToCurrentTarget 참고)
            targetObjectQueue.Enqueue(t);
            descriptionQueue.Enqueue(i < targetDescriptions.Count ? targetDescriptions[i] : string.Empty);
        }

        // 유효한 타겟이 0개인 설정 실수: 에러를 남기고 미션을 즉시 완료 처리한다. (소프트락 방지)
        if (targetObjectQueue.Count == 0)
        {
            Debug.LogError("StageManager : 유효한 타겟이 없어 미션을 즉시 완료 처리합니다. targetObjectList를 확인하세요.");
            FinishMission();
            return;
        }

        ProceedToCurrentTarget();
    }

    public void CompleateTarget()
    {
        if (targetObjectQueue.Count <= 0)
        {
            return;
        }

        IInteractable pastTarget = targetObjectQueue.Dequeue();
        pastTarget.OnTargetCompleted -= CompleateTarget;   // 구독 해제
        if (descriptionQueue.Count > 0) descriptionQueue.Dequeue();
        pastTarget.DisableInteraction();

        ProceedToCurrentTarget();
    }

    // 다음 차례 타겟을 활성화한다.
    // 이미 완료된 타겟은 건너뛰고, 남은 타겟이 없으면 미션을 끝낸다.
    private void ProceedToCurrentTarget()
    {
        SkipCompletedTargets();

        IInteractable curTarget = GetCurrentTarget();

        if (curTarget == null)
        {
            FinishMission();   // 전부 선완료였다면 여기서 바로 미션 완료
            return;
        }

        // 차례가 된 타겟만 구독 -> 순서가 어긋난 완료 이벤트가 큐를 망가뜨리지 못한다.
        curTarget.OnTargetCompleted += CompleateTarget;
        curTarget.EnableInteraction();
        hudManager.GetTargetMarker(missionId).SetMarker_On(curTarget.ObjectTransform);
        minimap.SetObjectTarget(curTarget.ObjectTransform);
        objectiveUI.Show(descriptionQueue.Peek());
    }

    // 큐 맨 앞이 "이미 완료된 타겟"이면 계속 건너뛴다. (설명 큐도 같이 맞춰서 뺀다)
    private void SkipCompletedTargets()
    {
        while (targetObjectQueue.Count > 0)
        {
            IInteractable head = targetObjectQueue.Peek();

            if (!IsAlreadyCompleted(head))
            {
                break;
            }

            targetObjectQueue.Dequeue();
            if (descriptionQueue.Count > 0) descriptionQueue.Dequeue();
        }
    }

    private bool IsAlreadyCompleted(IInteractable target)
    {
        // 오브젝트가 파괴됐다면 완료된 것으로 본다.
        // (타겟은 GetComponentInChildren으로 찾은 컴포넌트라 항상 Component다)
        Component comp = target as Component;
        if (comp == null)
        {
            return true;
        }

        ICompletionState state = target as ICompletionState;
        if (state != null && state.IsCompleted)
        {
            return true;
        }

        return false;
    }

    private void FinishMission()
    {
        hudManager.GetTargetMarker(missionId).SetMarker_Off();
        minimap.SetObjectTarget(null);
        isMissionCompleted = true;
        OnMissionCompleted?.Invoke();
        foreach (GameObject obj in unlockOnComplete)
        {
            if (obj != null) obj.SetActive(true);
        }
        StartCoroutine(ShowCompleteThenHide());
    }

    private IEnumerator ShowCompleteThenHide()
    {
        objectiveUI.Show(missionCompletePrompt);
        yield return new WaitForSeconds(2f);
        objectiveUI.Hide();
    }

}
