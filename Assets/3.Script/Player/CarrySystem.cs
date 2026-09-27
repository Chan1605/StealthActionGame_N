using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class CarrySystem : MonoBehaviour
{
    [Header("Detect")]
    [SerializeField] private LayerMask bodyMask;
    [SerializeField] private float pickUpRange = 1.8f;
    [SerializeField] private float pickUpAngle = 120f;
    [SerializeField] private bool isCrouchRequired = true;

    [Header("Align")]
    [SerializeField] private bool isAlignEnabled = true;
    [SerializeField] private float alignTime = 0.3f;

    [Tooltip("시체 앞 이 거리에 섭니다. 여기서 무릎 꿇었을 때 손이 닿아야 합니다.")]
    [SerializeField] private float alignDistance = 0.55f;

    [Tooltip("현재 거리가 Align Distance와 이만큼 이내로 가까우면 위치는 안 움직이고 방향만 돌립니다.")]
    [SerializeField] private float alignTolerance = 0.2f;

    [Tooltip("정렬한 뒤 옆으로 살짝 비켜설 양. Holding Down 클립이 몸을 좌우로 22cm 옮기는 걸 상쇄합니다.")]
    [SerializeField] private float alignLateral = 0f;

    [Header("Socket")]
    [SerializeField] private Transform carrySocket;

    [Header("Animation")]
    [SerializeField] private string pickUpTrigger = "PickUpBody";
    [SerializeField] private string putDownTrigger = "PutDownBody";
    [SerializeField] private string carrySpeedParameter = "CarrySpeed";
    [Tooltip("Animator의 PickUpBody 상태 Speed를 0.7로 낮췄다면 1.27 / 0.7 = 1.81 을 넣으세요.")]
    [SerializeField] private float pickUpDuration = 1.81f;
    [SerializeField] private float putDownDuration = 1.81f;

    [Header("Lift Trick")]
    [Tooltip("켜면 시체를 잠깐 안 보이게 했다가 품에서 다시 나타나게 합니다. 바닥에서 떠오르는 어색함이 사라집니다.")]
    [SerializeField] private bool isHiddenDuringLift = true;

    [Tooltip("들기 클립의 몇 % 지점에서 시체를 감추고 품 자세로 즉시 옮길지.")]
    [SerializeField] [Range(0f, 1f)] private float hideAtRatio = 0.85f;

    [Tooltip("들기 클립이 끝나고 몇 초 뒤에 시체를 다시 보이게 할지. 일어서는 블렌드 중간이 자연스럽습니다.")]
    [SerializeField] private float showDelay = 0.1f;

    [Tooltip("Lift Trick을 끌 때만 씁니다. 들기 클립의 몇 % 지점부터 시체를 품으로 끌어당길지.")]
    [SerializeField] [Range(0f, 1f)] private float attachStartRatio = 0.55f;

    [Tooltip("내려놓기 클립의 몇 % 지점에서 시체를 놓을지.")]
    [SerializeField] [Range(0f, 1f)] private float detachRatio = 0.5f;

    [Header("Move")]
    [SerializeField] private float carrySpeed = 1.4f;
    [SerializeField] private float carrySpeedDamping = 0.12f;

    [Header("Put Down")]
    [SerializeField] private float putDownForward = 0.7f;
    [SerializeField] private float putDownProbeHeight = 1.2f;
    [SerializeField] private LayerMask groundMask = 1;

    [Header("Debug")]
    [SerializeField] private bool isDebugLog = true;
    [SerializeField] private bool isDebugGizmo = true;

    private CharacterController _controller;
    private PlayerInput _input;
    private PlayerController _movement;
    private PlayerAnimator _playerAnimator;
    private Animator _animator;
    private PlayerInteractionRunner _runner;
    private AssassinationSystem _assassination;
    private PlayerThrow _throw;
    private PlayerWallClimb _wallClimb;

    private int _carrySpeedId;

    public bool isBusy { get; private set; }
    public CarriableBody heldBody { get; private set; }
    private CarriableBody _lastNotifiedBody;
    public bool isCarrying
    {
        get
        {
            return heldBody != null;
        }
    }

    /// <summary>CarriableBody가 에디터에서 자세를 잡을 때 찾아 씁니다.</summary>
    public Transform socket
    {
        get
        {
            return carrySocket;
        }
    }

    /// <summary>운반 중이거나 들기/놓기 모션 중이면 다른 행동을 막아야 합니다.</summary>
    public bool isBlockingOtherActions
    {
        get
        {
            return isBusy || isCarrying;
        }
    }

    public event Action<CarriableBody> OnCarryStarted;
    public event Action<CarriableBody> OnCarryEnded;
    public event Action<CarriableBody> OnCarryTargetAcquired;
    public event Action OnCarryTargetLost;


    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<PlayerInput>();
        _movement = GetComponent<PlayerController>();
        _playerAnimator = GetComponent<PlayerAnimator>();
        _runner = GetComponent<PlayerInteractionRunner>();
        _assassination = GetComponent<AssassinationSystem>();
        _throw = GetComponent<PlayerThrow>();
        _wallClimb = GetComponent<PlayerWallClimb>();

        _animator = GetComponentInChildren<Animator>();
        _carrySpeedId = Animator.StringToHash(carrySpeedParameter);

        if (carrySocket == null)
        {
            Debug.LogWarning("[CarrySystem] Carry Socket이 비어 있습니다. 가슴 높이에 빈 오브젝트를 만들어 연결하세요.", this);
        }
    }

    private void OnEnable()
    {
        _input.OnInteractPressed += HandleInteractPressed;
    }

    private void OnDisable()
    {
        _input.OnInteractPressed -= HandleInteractPressed;
    }

    private void SetPlayerControlEnabled(bool isEnabled)
    {
        if (_movement != null)
        {
            _movement.enabled = isEnabled;
        }
    }

    private void Update()
    {
        UpdateCarrySpeedParameter();
    }

    private void LateUpdate()
    {
        if (isBusy || isCarrying)
        {
            if (_lastNotifiedBody != null)
            {
                _lastNotifiedBody = null;
                OnCarryTargetLost?.Invoke();
            }
            return;
        }

        CarriableBody current = FindBody();
        CarriableBody valid = (current != null && CanCarry(current)) ? current : null;

        if (valid != _lastNotifiedBody)
        {
            _lastNotifiedBody = valid;
            if (valid != null) OnCarryTargetAcquired?.Invoke(valid);
            else OnCarryTargetLost?.Invoke();
        }
    }

    private void UpdateCarrySpeedParameter()
    {
        if (_animator == null)
        {
            return;
        }

        float target = 0f;

        if (isCarrying && _movement != null && carrySpeed > 0.01f)
        {
            target = Mathf.Clamp01(_movement.currentSpeed / carrySpeed);
        }

        _animator.SetFloat(_carrySpeedId, target, carrySpeedDamping, Time.deltaTime);
    }

    private void HandleInteractPressed()
    {
        if (isBusy)
        {
            return;
        }

        if (isCarrying)
        {
            if (_runner != null && _runner.HasReadyAction())
            {
                return;
            }

            TryPutDown();
            return;
        }

        TryPickUp();
    }

    public bool TryPickUp()
    {
        if (isBusy || isCarrying)
        {
            return false;
        }

        if (!IsFree())
        {
            return false;
        }

        if (isCrouchRequired && _movement != null && !_movement.isCrouched)
        {
            Log("앉은 상태에서만 시체를 들 수 있습니다.");
            return false;
        }

        CarriableBody body = FindBody();

        if (body == null)
        {
            return false;
        }

        StartCoroutine(PickUp_co(body));

        return true;
    }

    public bool TryPutDown()
    {
        if (isBusy || !isCarrying)
        {
            return false;
        }

        StartCoroutine(PutDown_co());

        return true;
    }

    public bool CanCarry(CarriableBody body)
    {
        if (body == null || isBusy || isCarrying) return false;
        if (!IsFree()) return false;
        if (isCrouchRequired && _movement != null && !_movement.isCrouched) return false;
        if (!body.canCarry) return false;

        Vector3 flat = body.transform.position - transform.position;
        flat.y = 0f;
        float distance = flat.magnitude;
        if (distance > pickUpRange) return false;

        float angle = distance > 0.2f ? Vector3.Angle(transform.forward, flat) : 0f;
        if (angle > pickUpAngle * 0.5f) return false;

        return true;
    }

    /// <summary>
    /// 드럼통 같은 처리 장치가 호출합니다. 시체의 소유권을 넘겨받고 플레이어는 Idle로 돌아갑니다.
    /// 반환된 CarriableBody의 연출과 Dispose는 호출한 쪽이 책임집니다.
    /// </summary>
    public CarriableBody ReleaseForDispose()
    {
        if (!isCarrying)
        {
            return null;
        }

        CarriableBody body = heldBody;
        heldBody = null;

        if (_movement != null)
        {
            _movement.ClearSpeedOverride();
        }

        if (_playerAnimator != null)
        {
            _playerAnimator.EndAction(pickUpTrigger);
        }

        OnCarryEnded?.Invoke(body);
        Log("시체를 처리 장치로 넘겼습니다.");

        return body;
    }

    /// <summary>
    /// 사망 등으로 즉시 상태를 되돌려야 할 때 호출합니다. 연출 없이 시체를 그 자리에 내려놓습니다.
    /// </summary>
    public void ForceDropBody()
    {
        StopAllCoroutines();

        CarriableBody body = heldBody;

        if (body != null)
        {
            heldBody = null;

            GetPutDownPose(out Vector3 position, out Quaternion rotation);
            body.EndCarry(position, rotation);
            body.SetVisible(true);

            OnCarryEnded?.Invoke(body);
            Log("사망으로 시체를 내려놓았습니다.");
        }

        if (_movement != null)
        {
            _movement.ClearSpeedOverride();
        }

        if (_playerAnimator != null)
        {
            _playerAnimator.EndAction(pickUpTrigger);
            _playerAnimator.EndAction(putDownTrigger);
        }

        _lastNotifiedBody = null;

        SetPlayerControlEnabled(true);

        if (_controller != null)
        {
            _controller.enabled = true;
        }

        isBusy = false;
    }

    private bool IsFree()
    {
        if (_runner != null && _runner.isBusy)
        {
            return false;
        }

        if (_assassination != null && _assassination.isBusy)
        {
            return false;
        }

        if (_throw != null && _throw.isBusy)
        {
            return false;
        }

        if (_wallClimb != null && _wallClimb.isBusy)
        {
            return false;
        }

        return true;
    }

    private CarriableBody FindBody()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Collider[] hits = Physics.OverlapSphere(origin, pickUpRange, bodyMask, QueryTriggerInteraction.Ignore);

        if (hits.Length == 0)
        {
            Log($"반경 {pickUpRange:F1}m 안에 Body Mask에 걸리는 콜라이더가 하나도 없습니다. 시체 레이어와 Body Mask를 확인하세요.");
            return null;
        }

        CarriableBody best = null;
        float bestDistance = float.MaxValue;

        HashSet<CarriableBody> seen = new HashSet<CarriableBody>();
        float halfAngle = pickUpAngle * 0.5f;

        foreach (Collider hit in hits)
        {
            CarriableBody body = hit.GetComponentInParent<CarriableBody>();

            if (body == null)
            {
                continue;
            }

            if (!seen.Add(body))
            {
                continue;
            }

            string blockReason = body.carryBlockReason;

            if (!string.IsNullOrEmpty(blockReason))
            {
                Log($"'{body.name}' 제외 - {blockReason}");
                continue;
            }

            Vector3 flat = body.transform.position - transform.position;
            flat.y = 0f;

            float distance = flat.magnitude;

            if (distance > pickUpRange)
            {
                Log($"'{body.name}' 제외 - 수평거리 {distance:F2}m > Pick Up Range {pickUpRange:F2}m");
                continue;
            }

            // 시체 바로 위에 서 있으면 방향 벡터가 불안정해집니다. 가까우면 각도 검사를 건너뜁니다.
            float angle = distance > 0.2f ? Vector3.Angle(transform.forward, flat) : 0f;

            if (angle > halfAngle)
            {
                Log($"'{body.name}' 제외 - 정면에서 {angle:F0}° 벗어남 (허용 {halfAngle:F0}°)");
                continue;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = body;
            }
        }

        if (best == null && seen.Count == 0)
        {
            Log($"콜라이더 {hits.Length}개를 찾았지만 전부 CarriableBody가 없습니다. 적의 Hips 본에 CarriableBody를 붙였는지 확인하세요. (예: '{hits[0].name}')");
        }

        return best;
    }

    [ContextMenu("Check Carry Setup")]
    public void CheckSetup()
    {
        List<string> problems = new List<string>();

        if (carrySocket == null)
        {
            problems.Add("Carry Socket이 비어 있습니다.");
        }

        if (bodyMask.value == 0)
        {
            problems.Add("Body Mask가 Nothing입니다.");
        }

        Animator animator = GetComponentInChildren<Animator>();

        if (animator == null || animator.runtimeAnimatorController == null)
        {
            problems.Add("Animator 또는 Controller가 없습니다.");
        }
        else
        {
            foreach (string required in new[] { pickUpTrigger, putDownTrigger, carrySpeedParameter })
            {
                bool found = false;

                foreach (AnimatorControllerParameter parameter in animator.parameters)
                {
                    if (parameter.name == required)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    problems.Add($"Animator에 '{required}' 파라미터가 없습니다.");
                }
            }
        }

        CarriableBody[] bodies = FindObjectsByType<CarriableBody>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (bodies.Length == 0)
        {
            problems.Add("씬에 CarriableBody가 하나도 없습니다. 적의 Hips 본에 붙이세요.");
        }

        foreach (CarriableBody body in bodies)
        {
            int layer = body.gameObject.layer;
            bool isInMask = (bodyMask.value & (1 << layer)) != 0;
            string layerName = LayerMask.LayerToName(layer);

            if (!isInMask)
            {
                problems.Add($"'{body.name}'이(가) '{layerName}' 레이어인데 Body Mask에 없습니다.");
            }

            if (body.GetComponentInParent<TakedownVictim>() == null)
            {
                problems.Add($"'{body.name}'의 부모에 TakedownVictim이 없습니다.");
            }

            if (body.GetComponentsInChildren<Rigidbody>(true).Length < 5)
            {
                problems.Add($"'{body.name}' 아래 Rigidbody가 너무 적습니다. Hips 본이 맞는지 확인하세요.");
            }
        }

        if (problems.Count == 0)
        {
            Debug.Log($"[CarrySystem] 세팅 이상 없습니다. (씬의 CarriableBody {bodies.Length}개)", this);
            return;
        }

        Debug.LogWarning($"[CarrySystem] 확인이 필요한 항목 {problems.Count}개\n- {string.Join("\n- ", problems)}", this);
    }

    private IEnumerator PickUp_co(CarriableBody body)
    {
        isBusy = true;

        try
        {
            SetPlayerControlEnabled(false);
            _controller.enabled = false;
            if (isAlignEnabled)
            {
                yield return Align_co(body.transform.position);
            }

            if (_playerAnimator != null)
            {
                _playerAnimator.PlayAction(pickUpTrigger);
            }

            float duration = Mathf.Max(0.05f, pickUpDuration);
            float attachStart = duration * Mathf.Clamp01(isHiddenDuringLift ? hideAtRatio : attachStartRatio);
            float elapsed = 0f;
            bool isAttached = false;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                if (!isAttached && elapsed >= attachStart)
                {
                    isAttached = true;

                    if (isHiddenDuringLift)
                    {
                        // 감추고 곧바로 품 자세로 옮깁니다. 보간하지 않으니 떠오르는 구간이 없습니다.
                        body.SetVisible(false);
                        body.BeginCarry(carrySocket);
                        body.ApplyCarryBlend(1f);
                    }
                    else
                    {
                        body.BeginCarry(carrySocket);
                    }
                }

                if (isAttached && !isHiddenDuringLift)
                {
                    float span = Mathf.Max(0.01f, duration - attachStart);
                    body.ApplyCarryBlend((elapsed - attachStart) / span);
                }

                yield return null;
            }

            if (!isAttached)
            {
                if (isHiddenDuringLift)
                {
                    body.SetVisible(false);
                }

                body.BeginCarry(carrySocket);
            }

            body.ApplyCarryBlend(1f);

            heldBody = body;

            if (_movement != null)
            {
                // Carrying 클립은 선 자세입니다. 앉아서 들었다면 캡슐도 같이 세워야 합니다.
                _movement.ForceStand();
                _movement.SetSpeedOverride(carrySpeed);
            }

            OnCarryStarted?.Invoke(body);
            Log($"{(body.victimRoot != null ? body.victimRoot.name : body.name)} 운반 시작");

            if (isHiddenDuringLift)
            {
                // 일어서는 블렌드가 도는 동안 잠깐 기다렸다가 품에서 나타나게 합니다.
                if (showDelay > 0f)
                {
                    yield return new WaitForSeconds(showDelay);
                }

                body.SetVisible(true);
            }
        }
        finally
        {
            SetPlayerControlEnabled(true);
            _controller.enabled = true;
            if (body != null)
            {
                body.SetVisible(true);
            }

            isBusy = false;
        }
    }

    private IEnumerator PutDown_co()
    {
        isBusy = true;

        CarriableBody body = heldBody;

        try
        {
            SetPlayerControlEnabled(false);
            if (_playerAnimator != null)
            {
                _playerAnimator.PlayAction(putDownTrigger);
            }

            float duration = Mathf.Max(0.05f, putDownDuration);
            float detachAt = duration * Mathf.Clamp01(detachRatio);
            float hideAt = Mathf.Max(0f, detachAt - Mathf.Max(0f, showDelay));
            float elapsed = 0f;
            bool isDetached = false;
            bool isHidden = false;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // 품 → 바닥으로 순간이동하는 찰나만 가립니다. EndCarry가 다시 보이게 합니다.
                if (isHiddenDuringLift && !isHidden && elapsed >= hideAt)
                {
                    isHidden = true;
                    body.SetVisible(false);
                }

                if (!isDetached && elapsed >= detachAt)
                {
                    isDetached = true;

                    GetPutDownPose(out Vector3 position, out Quaternion rotation);

                    body.EndCarry(position, rotation);

                    heldBody = null;

                    if (_movement != null)
                    {
                        _movement.ClearSpeedOverride();
                    }

                    OnCarryEnded?.Invoke(body);
                }

                yield return null;
            }
        }
        finally
        {
            SetPlayerControlEnabled(true);
            if (heldBody != null)
            {
                GetPutDownPose(out Vector3 position, out Quaternion rotation);
                heldBody.EndCarry(position, rotation);
                heldBody = null;

                if (_movement != null)
                {
                    _movement.ClearSpeedOverride();
                }

                OnCarryEnded?.Invoke(body);
            }

            if (_playerAnimator != null)
            {
                _playerAnimator.EndAction(putDownTrigger);
            }

            isBusy = false;
        }
    }

    private void GetPutDownPose(out Vector3 position, out Quaternion rotation)
    {
        Vector3 flatForward = transform.forward;
        flatForward.y = 0f;
        flatForward.Normalize();

        Vector3 target = transform.position + flatForward * putDownForward;
        Vector3 probe = target + Vector3.up * putDownProbeHeight;

        if (Physics.Raycast(probe, Vector3.down, out RaycastHit hit, putDownProbeHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
        {
            target.y = hit.point.y + 0.15f;
        }
        else
        {
            target.y = transform.position.y + 0.15f;
        }

        position = target;
        rotation = Quaternion.LookRotation(flatForward);
    }

    private IEnumerator Align_co(Vector3 bodyPosition)
    {
        Vector3 flat = bodyPosition - transform.position;
        flat.y = 0f;

        if (flat.sqrMagnitude < 0.0001f)
        {
            yield break;
        }

        float distance = flat.magnitude;
        Vector3 direction = flat / distance;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        Vector3 targetPosition = transform.position;

        // 이미 적당한 거리면 몸을 앞으로 끌지 않습니다. 방향만 맞춥니다.
        if (Mathf.Abs(distance - alignDistance) > alignTolerance)
        {
            targetPosition = bodyPosition - direction * alignDistance;
        }

        if (Mathf.Abs(alignLateral) > 0.001f)
        {
            targetPosition += targetRotation * Vector3.right * alignLateral;
        }

        targetPosition.y = transform.position.y;

        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        if (_movement != null)
        {
            _movement.enabled = false;
        }

        _controller.enabled = false;

        float duration = Mathf.Max(0.01f, alignTime);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float k = Mathf.Clamp01(elapsed / duration);

            transform.SetPositionAndRotation(
                Vector3.Lerp(startPosition, targetPosition, k),
                Quaternion.Slerp(startRotation, targetRotation, k));

            yield return null;
        }

        transform.SetPositionAndRotation(targetPosition, targetRotation);
        // 컨트롤 복구는 여기서 하지 않습니다 — PickUp_co()의 finally가 전체 구간을 책임
        //_controller.enabled = true;

        //if (_movement != null)
        //{
        //    _movement.StopVertical();
        //    _movement.enabled = true;
        //}

    }

    private void Log(string message)
    {
        if (isDebugLog)
        {
            Debug.Log($"[CarrySystem] {message}", this);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!isDebugGizmo)
        {
            return;
        }

        Vector3 origin = transform.position + Vector3.up * 0.5f;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawWireSphere(origin, pickUpRange);

        Quaternion left = Quaternion.Euler(0f, -pickUpAngle * 0.5f, 0f);
        Quaternion right = Quaternion.Euler(0f, pickUpAngle * 0.5f, 0f);

        Gizmos.DrawLine(origin, origin + left * transform.forward * pickUpRange);
        Gizmos.DrawLine(origin, origin + right * transform.forward * pickUpRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position + transform.forward * alignDistance, 0.1f);
    }
#endif
}
