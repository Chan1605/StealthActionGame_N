using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DayToNightTrigger : MonoBehaviour
{
    [Header("조건")]
    [SerializeField] private int requiredKeyCount = 3;
    [SerializeField] private string[] requiredKeyIds; // 3개의 키 id를 순서대로

    [Header("연출")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private Text messageText;
    [SerializeField] private string message = "밤이 되었습니다...";
    [SerializeField] private float messageHoldDuration = 2.5f;
    [SerializeField] private float fadeDuration = 2f;

    [Header("씬")]
    [SerializeField] private string nightSceneName;

    [Header("조건 (택1: 키 또는 상호작용 오브젝트)")]
    [SerializeField] private InteractionAction[] requiredInteractions;

    [Header("조건 (미션 매니저 전체 완료)")]
    [SerializeField] private StageManager[] requiredMissions;
    [Header("초기 안내")]
    [SerializeField] private string initialGuidePrompt = "두 미션을 모두 완료하세요";
    [SerializeField] private float initialGuideDuration = 3f;

    private KeyInventory _keyInventory;
    private bool _isTriggered;

    private void Start()
    {
        _keyInventory = FindAnyObjectByType<KeyInventory>();
        StartCoroutine(ShowInitialGuide_co());
    }

    private void Update()
    {
        if (_isTriggered) return;

        if (_keyInventory == null)
        {
            _keyInventory = FindAnyObjectByType<KeyInventory>();
            if (_keyInventory == null) return;
        }

        if (HasAllRequiredKeys() && HasCompletedAllInteractions() && HasCompletedAllMissions())
        {
            _isTriggered = true;
            StartCoroutine(TransitionToNight_co());
        }
    }

    private bool HasAllRequiredKeys()
    {
        if (requiredKeyIds == null || requiredKeyIds.Length == 0) return true; // 키 조건 자체가 없으면 통과

        int count = 0;
        foreach (string keyId in requiredKeyIds)
        {
            if (_keyInventory != null && _keyInventory.HasKey(keyId)) count++;
        }

        return count >= requiredKeyCount;
    }
    private bool HasCompletedAllInteractions()
    {
        if (requiredInteractions == null || requiredInteractions.Length == 0) return true;

        foreach (InteractionAction action in requiredInteractions)
        {
            if (action == null) continue;
            if (!action.isUsed) return false;
        }

        return true;
    }

    private bool HasCompletedAllMissions()
    {
        if (requiredMissions == null || requiredMissions.Length == 0) return true;

        foreach (StageManager mission in requiredMissions)
        {
            if (mission == null) continue;
            if (!mission.isMissionCompleted) return false;
        }

        return true;
    }

    private IEnumerator TransitionToNight_co()
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(messageHoldDuration);

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            fadeCanvasGroup.alpha = 0f;
            yield return fadeCanvasGroup.DOFade(1f, fadeDuration).WaitForCompletion();
        }

        GameSession.Instance?.SavePlayerKeys(_keyInventory);

        if (SceneTransitionMgr.Instance != null)
        {
            SceneTransitionMgr.Instance.LoadScene(nightSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nightSceneName);
        }
    }

    private IEnumerator ShowInitialGuide_co()
    {
        UI_MissionObjective objectiveUI = FindAnyObjectByType<HUDManager>().GetObjective();
        objectiveUI.Show(initialGuidePrompt);
        yield return new WaitForSeconds(initialGuideDuration);

        // 그 사이 이미 미션을 시작했다면 덮어쓰지 않음
        bool anyMissionStarted = false;
        foreach (StageManager mission in requiredMissions)
        {
            if (mission != null && mission.HasStarted) anyMissionStarted = true;
        }

        if (!anyMissionStarted) objectiveUI.Hide();
    }
}