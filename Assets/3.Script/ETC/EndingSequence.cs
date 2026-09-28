using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 차량 상호작용으로 시작하는 엔딩 화면.
// 조작은 막지 않고 UI만 띄운 뒤, Enter 또는 버튼으로 엔딩 크레딧 씬으로 넘어간다.
public class EndingSequence : MonoBehaviour
{
    public static EndingSequence Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text killCountText;
    [SerializeField] private Text playTimeText;
    [SerializeField] private Text guideText;

    [Header("문구")]
    [SerializeField] private string titleMessage = "탈출 성공";
    [SerializeField] private string killCountFormat = "제압한 적   {0}명";
    [SerializeField] private string playTimeFormat = "플레이 타임   {0}";
    [SerializeField] private string guideMessage = "Enter 를 눌러 계속";

    [Header("연출")]
    [Tooltip("StageManager의 '미션 완료' 문구(2초)가 지난 뒤 뜨도록 여유를 둔다.")]
    [SerializeField] private float showDelay = 2.2f;
    [SerializeField] private float fadeDuration = 0.8f;
    [Tooltip("뜨자마자 입력이 먹히는 것을 막는다.")]
    [SerializeField] private float inputBlockTime = 0.6f;
    [Tooltip("버튼을 누를 수 있도록 마우스 커서를 보이게 한다.")]
    [SerializeField] private bool isShowCursor = true;

    [Header("Scene")]
    [Tooltip("팀원이 만드는 엔딩 크레딧 씬 이름. Build Settings에 등록되어 있어야 한다.")]
    [SerializeField] private string endingSceneName = "CreditScene";

    [Header("적 처리")]
    [Tooltip("엔딩 화면이 뜨는 동안 적이 플레이어를 감지하지 못하게 한다.")]
    [SerializeField] private bool isDisablePlayerDetection = true;
    [Tooltip("이미 쫓아오던 적들의 추적 기억도 지운다.")]
    [SerializeField] private bool isResetEnemies = true;

    private bool _isShown;
    private bool _isLoading;
    private float _shownTime;

    private void Awake()
    {
        Instance = this;

        if (panel != null)
        {
            panel.alpha = 0f;
            panel.blocksRaycasts = false;
            panel.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>CarEscapeAction이 호출한다.</summary>
    public void Play()
    {
        if (_isShown || _isLoading)
        {
            return;
        }

        StartCoroutine(Show_co());
    }

    private IEnumerator Show_co()
    {
        if (showDelay > 0f)
        {
            yield return new WaitForSeconds(showDelay);
        }

        GameSession session = GameSession.Instance;

        if (session != null)
        {
            session.StopTimer();
        }

        DisableDetection();

        if (titleText != null)
        {
            titleText.text = titleMessage;
        }

        if (killCountText != null)
        {
            killCountText.text = string.Format(killCountFormat, session != null ? session.assassinationTotal : 0);
        }

        if (playTimeText != null)
        {
            playTimeText.text = string.Format(playTimeFormat, session != null ? session.GetPlayTimeText() : "00:00");
        }

        if (guideText != null)
        {
            guideText.text = guideMessage;
        }

        if (panel != null)
        {
            panel.gameObject.SetActive(true);
            panel.alpha = 0f;
            panel.blocksRaycasts = true;
            panel.DOFade(1f, fadeDuration).SetUpdate(true);
        }

        if (isShowCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        _isShown = true;
        _shownTime = Time.unscaledTime;
    }

    private void Update()
    {
        if (!_isShown || _isLoading)
        {
            return;
        }

        if (Time.unscaledTime - _shownTime < inputBlockTime)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            Continue();
        }
    }

    // 엔딩 화면 동안 적이 플레이어를 감지하지 못하게 한다.
    private void DisableDetection()
    {
        if (isDisablePlayerDetection)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");

            if (playerObj != null && playerObj.TryGetComponent(out PlayerDetectable detectable))
            {
                detectable.IsUndetectable = true;
            }
            else
            {
                Debug.LogWarning("[Ending] PlayerDetectable을 찾지 못해 감지 차단을 건너뜁니다.", this);
            }
        }

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
    }

    /// <summary>버튼의 OnClick에도 연결한다.</summary>
    public void Continue()
    {
        if (_isLoading)
        {
            return;
        }

        if (string.IsNullOrEmpty(endingSceneName))
        {
            Debug.LogWarning("[Ending] Ending Scene Name이 비어 있습니다. 엔딩 크레딧 씬 이름을 넣어주세요.", this);
            return;
        }

        _isLoading = true;
        Time.timeScale = 1f;

        if (SceneTransitionMgr.Instance != null)
        {
            SceneTransitionMgr.Instance.LoadScene(endingSceneName);
        }
        else
        {
            SceneManager.LoadScene(endingSceneName);
        }
    }
}
