using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCInteractionObject : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    [SerializeField] private string npcName;

    [Header("Mission")]
    [SerializeField] private bool completeMissionTargetOnDialogueEnd = false;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string talkBoolParam = "IsTalking";
    [Header("Random Motion")]
    [SerializeField] private string idleIndexParam = "IdleIndex";
    [SerializeField] private int idleClipCount = 3;
    [SerializeField] private string talkIndexParam = "TalkIndex";
    [SerializeField] private int talkClipCount = 3;
    [Header("Look At Player")]
    [SerializeField] private bool lookAtPlayerWhileTalking = true;
    [SerializeField] private float turnSpeed = 6f;

    [Header("Object Data")]
    [SerializeField] private Object_Data objectData;
    [SerializeField] private Transform uiAnchor;

    [Header("Debug")]
    [SerializeField] private bool isDebugLog = false;

    private PlayerInteractor _interactor;
    private UI_Outliner _outLine;
    private UI_ObjKeyPanal _keyPanal;
    private HUDManager _hudManager;
    private SubtitleTrigger _subtitleTrigger;

    private bool _isPanelOn;
    private Coroutine _endWatchRoutine;
    private Transform _playerTransform;
    private Quaternion _initialRotation;

    public Transform ObjectTransform
    {
        get { return transform; }
    }

    public Action OnUse { get; set; }
    public Action OnLook { get; set; }

    public event Action OnTargetCompleted;

    // 대화가 끝났을 때 미션 시작 트리거 쪽에서 구독할 이벤트
    public event Action OnDialogueEnded;

    public bool IsPlayerLook { get; set; }

    public bool isTalking { get; private set; }

    public bool IsInteractable
    {
        get
        {
            // 대화 중엔 다시 실행되지 않도록 막는다 (재상호작용 방지)
            return !isTalking;
        }
    }

    private void Awake()
    {
        TryGetComponent(out _outLine);
        TryGetComponent(out _subtitleTrigger);
        _initialRotation = transform.rotation;
    }

    private void Start()
    {
        _hudManager = FindAnyObjectByType<HUDManager>();
        _interactor = FindAnyObjectByType<PlayerInteractor>();

        if (_hudManager != null)
        {
            _keyPanal = _hudManager.GetKeyPanal();
        }

        if (_interactor == null)
        {
            Debug.LogWarning($"[NPCInteraction] 씬에서 PlayerInteractor를 찾지 못했습니다.", this);
        }

        if (_subtitleTrigger == null)
        {
            Debug.LogWarning($"[NPCInteraction] '{name}'에 SubtitleTrigger 컴포넌트가 없습니다. 같은 오브젝트에 추가하고 End Subtitles에 대화 내용을 넣어주세요.", this);
        }

        GameObject playerObj = GameObject.FindWithTag("Player");

        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning($"[NPCInteraction] 'Player' 태그를 가진 오브젝트를 찾지 못했습니다.", this);
        }
        RandomizeMotion(idleIndexParam, idleClipCount);
        SetPanelActive(false);
    }

    private void RandomizeMotion(string paramName, int clipCount)
    {
        if (animator == null || string.IsNullOrEmpty(paramName) || clipCount <= 0)
        {
            return;
        }

        animator.SetFloat(paramName, UnityEngine.Random.Range(0, clipCount));
    }

    private void Update()
    {
        bool isLooked = _interactor != null && ReferenceEquals(_interactor.CurrentTarget, this);
        bool isWanted = isLooked && IsInteractable;

        if (isWanted != _isPanelOn)
        {
            if (isWanted)
            {
                OnLook?.Invoke();
            }
            SetPanelActive(isWanted);
        }

        if (lookAtPlayerWhileTalking)
        {
            if (isTalking && _playerTransform != null)
            {
                FaceTowardsPlayer();
            }
            else
            {
                ReturnToInitialRotation();
            }
        }
    }

    private void FaceTowardsPlayer()
    {
        Vector3 direction = _playerTransform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
    }

    // 대화 중이 아닐 때는 원래 바라보던 방향으로 서서히 복귀한다.
    private void ReturnToInitialRotation()
    {
        if (Quaternion.Angle(transform.rotation, _initialRotation) < 0.05f)
        {
            return;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, _initialRotation, Time.deltaTime * turnSpeed);
    }

    // NPCDialogueAction이 상호작용 실행 시 호출한다
    public void StartDialogue()
    {
        if (isTalking)
        {
            return;
        }

        List<Narration_Data> lines = _subtitleTrigger != null ? _subtitleTrigger.EndSubtitles : null;

        if (lines == null || lines.Count == 0)
        {
            Debug.LogWarning($"[NPCInteraction] '{name}'의 SubtitleTrigger End Subtitles가 비어있습니다.", this);
            return;
        }

        isTalking = true;
        SetPanelActive(false);

        if (animator == null)
        {
            Debug.LogWarning($"[NPCInteraction] '{name}'의 NPCInteractionObject에 Animator가 연결되어 있지 않습니다. 인스펙터의 Animator 필드를 채워주세요.", this);
        }
        else if (string.IsNullOrEmpty(talkBoolParam))
        {
            Debug.LogWarning($"[NPCInteraction] '{name}'의 Talk Bool Param 이름이 비어있습니다.", this);
        }
        else
        {
            animator.SetBool(talkBoolParam, true);
            RandomizeMotion(talkIndexParam, talkClipCount);
            Debug.Log($"[NPCInteraction] animator.SetBool(\"{talkBoolParam}\", true) 호출함 (대상 Animator: {animator.name}, Controller: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "없음")})", this);
        }


        float totalDuration = 0f;
        for (int i = 0; i < lines.Count; i++)
        {
            totalDuration += lines[i].duration;
        }

        if (_endWatchRoutine != null)
        {
            StopCoroutine(_endWatchRoutine);
        }

        _endWatchRoutine = StartCoroutine(WaitForDialogueEnd_co(totalDuration));

        if (isDebugLog)
        {
            Debug.Log($"[NPCInteraction] {npcName} 대화 시작 (총 {totalDuration:F1}초)", this);
        }
    }

    private IEnumerator WaitForDialogueEnd_co(float totalDuration)
    {
        yield return new WaitForSeconds(totalDuration);

        _endWatchRoutine = null;
        isTalking = false;

        if (animator != null && !string.IsNullOrEmpty(talkBoolParam))
        {
            animator.SetBool(talkBoolParam, false);
            RandomizeMotion(idleIndexParam, idleClipCount);
        }

        OnDialogueEnded?.Invoke();

        if (completeMissionTargetOnDialogueEnd)
        {
            OnTargetCompleted?.Invoke();
        }

        if (isDebugLog)
        {
            Debug.Log($"[NPCInteraction] {npcName} 대화 종료", this);
        }
    }

    private void SetPanelActive(bool isOn)
    {
        _isPanelOn = isOn;
        IsPlayerLook = isOn;

        if (_outLine != null)
        {
            if (isOn) _outLine.SetOutLine_On();
            else _outLine.SetOutLine_Off();
        }

        if (_keyPanal != null)
        {
            if (isOn)
            {
                Transform anchor = uiAnchor != null ? uiAnchor : transform;

                if (objectData != null) _keyPanal.SetPanal_On(anchor, objectData);
                else _keyPanal.SetPanal_On(anchor);
            }
            else
            {
                _keyPanal.SetPanal_Off();
            }
        }

        if (isDebugLog)
        {
            Debug.Log($"[NPCInteraction] {name} 패널 {(isOn ? "표시" : "숨김")}", this);
        }
    }

    public void EnableInteraction() { }

    public void DisableInteraction()
    {
        SetPanelActive(false);
    }
}
