using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerDeath : MonoBehaviour
{
    [Header("연출")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float deathClipLength = 3f;
    [SerializeField] private float postDeathHold = 0.5f;

    [Header("그레이스케일 (죽을 때 서서히 짙어짐)")]
    [SerializeField] private Volume deathGrayscaleVolume;
    [SerializeField] private float grayscaleFadeOutDuration = 5f;   // 죽을 때: 서서히 짙어짐
    [SerializeField] private float grayscaleFadeInDuration = 0.6f;  // 부활 시: 빠르게 원복

    [Header("슬로우모션")]
    [SerializeField] private bool isSlowMoEnabled = true;
    [SerializeField] private float slowMoScale = 0.4f;
    [SerializeField] private float slowMoDuration = 0.5f;

    [Header("카메라")]
    [SerializeField] private float deathZoomDistance = 1.5f;

    [Header("부활 암전 (짧은 블랙아웃)")]
    [SerializeField] private float blackFlashOutDuration = 0.3f;
    [SerializeField] private float blackFlashInDuration = 0.4f;

    private ColorAdjustments _colorAdjustments;
    [SerializeField] private UI_PlayerHealth healthUI;

    private CharacterController _controller;
    private PlayerController _movement;
    private PlayerCameraRig _cameraRig;
    private PlayerAnimator _playerAnimator;
    private DummyDamageable _health;
    private PlayerDetectable _detectable;

    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;
    private bool _isDying;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _movement = GetComponent<PlayerController>();
        _cameraRig = GetComponent<PlayerCameraRig>();
        _playerAnimator = GetComponent<PlayerAnimator>();
        _health = GetComponent<DummyDamageable>();
        _detectable = GetComponent<PlayerDetectable>();
        if (healthUI == null) healthUI = FindAnyObjectByType<UI_PlayerHealth>();

        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;

        if (deathGrayscaleVolume != null && deathGrayscaleVolume.profile.TryGet(out _colorAdjustments))
        {
            deathGrayscaleVolume.weight = 0f;
        }
    }

    private void OnEnable()
    {
        if (_health != null) _health.OnDied += HandleDied;
    }

    private void OnDisable()
    {
        if (_health != null) _health.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        if (_isDying) return;
        StartCoroutine(Die_co());
    }

    private IEnumerator Die_co()
    {
        if (_isDying) yield break;
        _isDying = true;
        if (_detectable != null) _detectable.IsDeadOrRespawning = true;
        SetControlEnabled(false);
        healthUI?.ForceHideDangerVolume(grayscaleFadeOutDuration);
        _controller.enabled = false;

        _playerAnimator?.PlayDie();
        _cameraRig?.ZoomTo(deathZoomDistance);

        EnemyAI[] enemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
        foreach (EnemyAI enemy in enemies)
        {
            enemy.ForceForgetPlayer();
        }

        if (isSlowMoEnabled)
        {
            Time.timeScale = slowMoScale;
            yield return new WaitForSecondsRealtime(slowMoDuration);
            Time.timeScale = 1f;
        }

        if (deathGrayscaleVolume != null)
        {
            DOTween.Kill(deathGrayscaleVolume);
            deathGrayscaleVolume.weight = 0f;
            DOTween.To(() => deathGrayscaleVolume.weight, x => deathGrayscaleVolume.weight = x, 1f, grayscaleFadeOutDuration)
                .SetTarget(deathGrayscaleVolume);
        }

        float remaining = deathClipLength - (isSlowMoEnabled ? slowMoDuration : 0f);
        if (remaining > 0f) yield return new WaitForSeconds(remaining);

        _playerAnimator?.ResetDie();

        yield return new WaitForSeconds(postDeathHold);

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            fadeCanvasGroup.alpha = 0f;
            yield return fadeCanvasGroup.DOFade(1f, blackFlashOutDuration).WaitForCompletion();
        }

        Respawn();
        SetControlEnabled(true);
        _isDying = false;

        if (deathGrayscaleVolume != null)
        {
            DOTween.Kill(deathGrayscaleVolume);
            DOTween.To(() => deathGrayscaleVolume.weight, x => deathGrayscaleVolume.weight = x, 0f, grayscaleFadeInDuration)
                .SetTarget(deathGrayscaleVolume);
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.DOFade(0f, blackFlashInDuration)
                .OnComplete(() => fadeCanvasGroup.gameObject.SetActive(false));
        }

    }

    private void Respawn()
    {
        Vector3 pos = _spawnPosition;
        Quaternion rot = _spawnRotation;

        if (CheckpointManager.Instance != null
            && CheckpointManager.Instance.TryGetCheckpoint(out Vector3 checkpointPos, out Quaternion checkpointRot))
        {
            pos = checkpointPos;
            rot = checkpointRot;
        }
        CheckpointManager.Instance?.ResetPendingSegment();
        _controller.enabled = false;
        transform.SetPositionAndRotation(pos, rot);
        _controller.enabled = true;

        _movement?.StopVertical();
        _cameraRig?.ResetZoom();
        _cameraRig?.ResetLook();

        _health?.ResetHealth();
        if (_detectable != null) _detectable.IsDeadOrRespawning = false;


    }

    private void SetControlEnabled(bool isEnabled)
    {
        if (_movement != null) _movement.enabled = isEnabled;
        if (_cameraRig != null) _cameraRig.enabled = isEnabled;
    }
}