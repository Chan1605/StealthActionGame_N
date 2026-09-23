using System.Collections;
using DG.Tweening;
using UnityEngine;

// 한 씬 안에서 챕터를 넘길 때 쓰는 연출.
// StageManager의 미션이 끝나면 페이드 아웃 -> 플레이어 이동 -> 페이드 인 -> 다음 챕터 시작.
public class ChapterWarpTransition : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private StageManager fromChapter;
    [Tooltip("StageManager가 '미션 완료'를 2초간 띄우므로 그보다 길게 둔다.")]
    [SerializeField] private float startDelay = 2.2f;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float holdDuration = 0.6f;
    [SerializeField] private float fadeInDuration = 1f;

    [Header("Warp")]
    [SerializeField] private Transform destination;
    [SerializeField] private bool isSetCheckpoint = true;
    [SerializeField] private bool isResetEnemies = true;

    [Header("구역 전환 (선택)")]
    [SerializeField] private GameObject[] disableOnWarp;
    [SerializeField] private GameObject[] enableOnWarp;

    [Header("Next Chapter")]
    [SerializeField] private MissionStartTrigger nextChapterTrigger;
    [SerializeField] private StageManager nextChapter;
    [Tooltip("켜면 워프 직후 바로 시작. 끄면 트리거만 켜고 플레이어가 밟을 때 시작.")]
    [SerializeField] private bool isStartNextImmediately = true;

    private bool _isWarping;

    private void Start()
    {
        if (fromChapter == null)
        {
            Debug.LogError("[ChapterWarp] From Chapter가 비었습니다.", this);
            return;
        }

        fromChapter.OnMissionCompleted += HandleChapterCompleted;
    }

    private void OnDestroy()
    {
        if (fromChapter != null)
        {
            fromChapter.OnMissionCompleted -= HandleChapterCompleted;
        }
    }

    private void HandleChapterCompleted()
    {
        if (_isWarping)
        {
            return;
        }

        _isWarping = true;
        StartCoroutine(Warp_co());
    }

    private IEnumerator Warp_co()
    {
        yield return new WaitForSeconds(startDelay);

        GameObject playerObj = GameObject.FindWithTag("Player");

        if (playerObj == null)
        {
            Debug.LogError("[ChapterWarp] Player 태그를 가진 오브젝트가 없습니다.", this);
            yield break;
        }

        PlayerController movement = playerObj.GetComponent<PlayerController>();
        PlayerCameraRig cameraRig = playerObj.GetComponent<PlayerCameraRig>();
        CharacterController controller = playerObj.GetComponent<CharacterController>();
        PlayerDetectable detectable = playerObj.GetComponent<PlayerDetectable>();
        PlayerInteractionRunner runner = playerObj.GetComponent<PlayerInteractionRunner>();

        // 진행 중인 상호작용을 끊고 조작을 잠근다.
        if (runner != null)
        {
            runner.ForceRelease();
        }

        if (detectable != null)
        {
            detectable.IsDeadOrRespawning = true;
        }

        SetControlEnabled(movement, cameraRig, false);

        yield return Fade_co(0f, 1f, fadeOutDuration);

        if (destination != null)
        {
            // CharacterController를 끄지 않으면 위치 변경이 무시된다.
            if (controller != null)
            {
                controller.enabled = false;
            }

            playerObj.transform.SetPositionAndRotation(destination.position, destination.rotation);

            if (controller != null)
            {
                controller.enabled = true;
            }

            if (isSetCheckpoint && CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.SetCheckpoint(destination.position, destination.rotation, null);
            }
        }
        else
        {
            Debug.LogWarning("[ChapterWarp] Destination이 비어 있어 위치를 옮기지 않았습니다.", this);
        }

        if (movement != null)
        {
            movement.StopVertical();
        }

        if (cameraRig != null)
        {
            // 플레이어 회전을 바꾼 뒤에 불러야 목적지 방향을 본다.
            cameraRig.ResetLook();
            cameraRig.ResetZoom();
        }

        SetActiveAll(disableOnWarp, false);
        SetActiveAll(enableOnWarp, true);

        if (isResetEnemies)
        {
            EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);

            foreach (EnemyAI enemy in enemies)
            {
                if (enemy != null)
                {
                    enemy.ForceForgetPlayer();
                }
            }
        }

        yield return new WaitForSeconds(holdDuration);
        yield return Fade_co(1f, 0f, fadeInDuration);

        SetControlEnabled(movement, cameraRig, true);

        if (detectable != null)
        {
            detectable.IsDeadOrRespawning = false;
        }

        yield return StartNextChapter_co();
    }

    private IEnumerator StartNextChapter_co()
    {
        if (nextChapterTrigger != null)
        {
            nextChapterTrigger.gameObject.SetActive(true);
            yield return null;   // 트리거의 Start()가 돌 시간을 준다.

            if (isStartNextImmediately)
            {
                nextChapterTrigger.StartMission();
            }

            yield break;
        }

        if (nextChapter != null)
        {
            nextChapter.InitializeQueue();
        }
    }

    private IEnumerator Fade_co(float from, float to, float duration)
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }

        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.alpha = from;
        fadeCanvasGroup.blocksRaycasts = true;

        yield return fadeCanvasGroup.DOFade(to, duration).SetUpdate(true).WaitForCompletion();

        fadeCanvasGroup.alpha = to;
        fadeCanvasGroup.blocksRaycasts = to > 0.5f;

        if (to <= 0f)
        {
            fadeCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void SetActiveAll(GameObject[] objects, bool isActive)
    {
        if (objects == null)
        {
            return;
        }

        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(isActive);
            }
        }
    }

    private void SetControlEnabled(PlayerController movement, PlayerCameraRig cameraRig, bool isEnabled)
    {
        if (movement != null)
        {
            movement.enabled = isEnabled;
        }

        if (cameraRig != null)
        {
            cameraRig.enabled = isEnabled;
        }
    }
}
