using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 시체의 Hips 본에 붙입니다. (예: mixamorig:Hips)
/// 래그돌 본들이 전부 Hips의 자식이라, 본 콜라이더에서 GetComponentInParent로 찾을 수 있습니다.
/// </summary>
public class CarriableBody : MonoBehaviour
{
    [Serializable]
    public struct BonePose
    {
        [Tooltip("Hips 기준 상대 경로. 프리팹 안팎에서 안전하게 유지됩니다.")]
        public string bonePath;
        public Vector3 localEuler;
    }

    [Header("Carried Root Pose")]
    [Tooltip("소켓 기준 Hips의 위치")]
    [SerializeField] private Vector3 carriedLocalPosition = Vector3.zero;

    [Tooltip("소켓 기준 Hips의 회전. 몸을 눕히려면 Z나 X를 90도 근처로 주세요.")]
    [SerializeField] private Vector3 carriedLocalEuler = new Vector3(0f, 0f, 90f);

    [Header("Carried Bone Pose")]
    [Tooltip("체크하면 안길 때 아래 본들을 저장된 자세로 맞춥니다. 비어 있으면 죽은 자세 그대로 굳습니다.")]
    [SerializeField] private bool isBonePoseApplied = true;
    [SerializeField] private BonePose[] carriedBonePoses;

    [Header("Debug")]
    [SerializeField] private bool isDebugLog = true;

    [Tooltip("Preview Attach가 자동으로 기록합니다. 비어 있으면 Preview Detach가 거부하니, 그럴 땐 원래 부모 본을 직접 넣으세요.")]
    [SerializeField] private Transform previewOriginalParent;
    [SerializeField] [HideInInspector] private Vector3 previewOriginalLocalPosition;
    [SerializeField] [HideInInspector] private Vector3 previewOriginalLocalEuler;
    [SerializeField] [HideInInspector] private string[] previewOriginalBonePaths;
    [SerializeField] [HideInInspector] private Vector3[] previewOriginalBoneEulers;

    private TakedownVictim _victim;
    private Transform _victimRoot;
    private Transform _originalParent;

    private Rigidbody[] _bodies;
    private bool[] _originalKinematic;
    private RigidbodyInterpolation[] _originalInterpolation;
    private Renderer[] _renderers;

    private Vector3 _blendStartPosition;
    private Quaternion _blendStartRotation;
    private Quaternion[] _blendStartBoneRotations;

    public bool isCarried { get; private set; }
    public bool isDisposed { get; private set; }

    public bool canCarry
    {
        get
        {
            return string.IsNullOrEmpty(carryBlockReason);
        }
    }

    /// <summary>들 수 없는 이유. 들 수 있으면 빈 문자열.</summary>
    public string carryBlockReason
    {
        get
        {
            if (isDisposed)
            {
                return "이미 처리된 시체입니다.";
            }

            if (isCarried)
            {
                return "이미 들려 있습니다.";
            }

            if (_victim == null)
            {
                return "부모에서 TakedownVictim을 못 찾았습니다. Hips 본에 붙였는지 확인하세요.";
            }

            if (!_victim.isDown)
            {
                return "아직 래그돌 상태가 아닙니다 (TakedownVictim.isDown = false).";
            }

            return string.Empty;
        }
    }

    public Transform victimRoot
    {
        get
        {
            return _victimRoot;
        }
    }

    private void Awake()
    {
        _victim = GetComponentInParent<TakedownVictim>();

        if (_victim == null)
        {
            Debug.LogError("[CarriableBody] 부모에서 TakedownVictim을 찾지 못했습니다. Hips 본에 붙였는지 확인하세요.", this);
        }
        else
        {
            _victimRoot = _victim.transform;
        }

        _originalParent = transform.parent;

        _bodies = GetComponentsInChildren<Rigidbody>(true);
        _originalKinematic = new bool[_bodies.Length];
        _originalInterpolation = new RigidbodyInterpolation[_bodies.Length];

        // 스킨드 메시는 Hips가 아니라 루트의 자식입니다 (RL_Face, RL_Hair, RL_Shoes ...).
        _renderers = (_victimRoot != null ? _victimRoot : transform).GetComponentsInChildren<Renderer>(true);

        if (_bodies.Length < 5)
        {
            Debug.LogWarning($"[CarriableBody] 자식 Rigidbody가 {_bodies.Length}개뿐입니다. Hips 본이 맞는지 확인하세요.", this);
        }

        if (isBonePoseApplied && (carriedBonePoses == null || carriedBonePoses.Length == 0))
        {
            Debug.LogWarning("[CarriableBody] 안긴 자세가 저장되어 있지 않습니다. 컴포넌트 우클릭 > Capture Current Pose 로 저장하세요.", this);
        }
    }

    /// <summary>
    /// 래그돌을 굳히고 소켓에 붙입니다. 이후 매 프레임 ApplyCarryBlend를 호출하세요.
    /// </summary>
    public void BeginCarry(Transform socket)
    {
        if (isCarried || socket == null)
        {
            return;
        }

        isCarried = true;

        for (int i = 0; i < _bodies.Length; i++)
        {
            Rigidbody body = _bodies[i];

            if (body == null)
            {
                continue;
            }

            _originalKinematic[i] = body.isKinematic;
            _originalInterpolation[i] = body.interpolation;

            // 움직이는 부모에 붙는 동안 Interpolate를 켜두면 부모 트랜스폼과 싸워서
            // 몸이 손이 아닌 엉뚱한 곳에 붙습니다. 반드시 꺼야 합니다.
            body.interpolation = RigidbodyInterpolation.None;
            body.isKinematic = true;
            body.detectCollisions = false;
        }

        transform.SetParent(socket, true);

        _blendStartPosition = transform.localPosition;
        _blendStartRotation = transform.localRotation;

        CacheBoneStartRotations();

        Log("들기 시작");
    }

    private Transform[] _poseBones;

    /// <summary>경로 문자열을 실제 Transform으로 한 번만 풀어둡니다.</summary>
    private void ResolvePoseBones()
    {
        if (_poseBones != null || carriedBonePoses == null)
        {
            return;
        }

        _poseBones = new Transform[carriedBonePoses.Length];

        for (int i = 0; i < carriedBonePoses.Length; i++)
        {
            string path = carriedBonePoses[i].bonePath;

            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            _poseBones[i] = transform.Find(path);

            if (_poseBones[i] == null)
            {
                Debug.LogWarning($"[CarriableBody] 본 경로 '{path}'를 찾지 못했습니다. 자세를 다시 저장하세요.", this);
            }
        }
    }

    private void CacheBoneStartRotations()
    {
        if (!isBonePoseApplied || carriedBonePoses == null || carriedBonePoses.Length == 0)
        {
            _blendStartBoneRotations = null;
            return;
        }

        ResolvePoseBones();

        _blendStartBoneRotations = new Quaternion[carriedBonePoses.Length];

        for (int i = 0; i < carriedBonePoses.Length; i++)
        {
            Transform bone = _poseBones[i];
            _blendStartBoneRotations[i] = bone != null ? bone.localRotation : Quaternion.identity;
        }
    }

    /// <summary>
    /// 시체를 통째로 안 보이게 합니다. 바닥 → 품으로 옮기는 순간을 가려서
    /// 공중에 떠오르는 것처럼 보이는 걸 막습니다.
    /// </summary>
    public void SetVisible(bool isVisible)
    {
        if (_renderers == null)
        {
            return;
        }

        foreach (Renderer renderer in _renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = isVisible;
            }
        }
    }

    /// <summary>k: 0 = 붙잡은 순간의 자세, 1 = 품에 안긴 자세</summary>
    public void ApplyCarryBlend(float k)
    {
        if (!isCarried)
        {
            return;
        }

        float t = Mathf.Clamp01(k);

        transform.localPosition = Vector3.Lerp(_blendStartPosition, carriedLocalPosition, t);
        transform.localRotation = Quaternion.Slerp(_blendStartRotation, Quaternion.Euler(carriedLocalEuler), t);

        if (_blendStartBoneRotations == null)
        {
            return;
        }

        for (int i = 0; i < carriedBonePoses.Length; i++)
        {
            Transform bone = _poseBones[i];

            if (bone == null || bone == transform)
            {
                continue;
            }

            bone.localRotation = Quaternion.Slerp(
                _blendStartBoneRotations[i],
                Quaternion.Euler(carriedBonePoses[i].localEuler),
                t);
        }
    }

    /// <summary>바닥에 내려놓습니다. 래그돌이 다시 살아납니다.</summary>
    public void EndCarry(Vector3 position, Quaternion rotation)
    {
        if (!isCarried)
        {
            return;
        }

        isCarried = false;

        transform.SetParent(_originalParent, true);
        transform.SetPositionAndRotation(position, rotation);

        SetVisible(true);

        for (int i = 0; i < _bodies.Length; i++)
        {
            Rigidbody body = _bodies[i];

            if (body == null)
            {
                continue;
            }

            body.interpolation = _originalInterpolation[i];
            body.isKinematic = _originalKinematic[i];
            body.detectCollisions = true;

#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = Vector3.zero;
#else
            body.velocity = Vector3.zero;
#endif
            body.angularVelocity = Vector3.zero;
        }

        Log("내려놓음");
    }

    /// <summary>드럼통 등에 처리합니다. 계층을 원래대로 돌린 뒤 통째로 비활성화합니다.</summary>
    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isCarried = false;
        isDisposed = true;

        if (_originalParent != null)
        {
            transform.SetParent(_originalParent, true);
        }

        if (_victimRoot != null)
        {
            _victimRoot.gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        Log("처리 완료");
    }

    private void Log(string message)
    {
        if (isDebugLog)
        {
            Debug.Log($"[CarriableBody] {(_victimRoot != null ? _victimRoot.name : name)} - {message}", this);
        }
    }

#if UNITY_EDITOR

    private static string GetRelativePath(Transform root, Transform bone)
    {
        if (bone == root)
        {
            return string.Empty;
        }

        string path = bone.name;
        Transform cursor = bone.parent;

        while (cursor != null && cursor != root)
        {
            path = cursor.name + "/" + path;
            cursor = cursor.parent;
        }

        return cursor == root ? path : null;
    }

    /// <summary>씬에서 플레이어의 CarrySocket을 찾습니다. 참조를 저장하지 않으므로 프리팹에서도 안전합니다.</summary>
    private Transform FindPlayerSocket()
    {
        CarrySystem carry = FindAnyObjectByType<CarrySystem>(FindObjectsInactive.Include);

        if (carry == null)
        {
            Debug.LogWarning("[CarriableBody] 씬에서 CarrySystem을 못 찾았습니다. 플레이어가 씬에 있어야 합니다.", this);
            return null;
        }

        if (carry.socket == null)
        {
            Debug.LogWarning("[CarriableBody] CarrySystem의 Carry Socket이 비어 있습니다.", this);
            return null;
        }

        return carry.socket;
    }

    /// <summary>씬 뷰에서 본을 직접 돌려 자세를 잡은 뒤 이걸 눌러 저장합니다.</summary>
    [ContextMenu("Capture Current Pose")]
    private void CaptureCurrentPose()
    {
        Rigidbody[] bones = GetComponentsInChildren<Rigidbody>(true);
        List<BonePose> poses = new List<BonePose>();

        foreach (Rigidbody bone in bones)
        {
            if (bone.transform == transform)
            {
                continue;
            }

            string path = GetRelativePath(transform, bone.transform);

            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            poses.Add(new BonePose
            {
                bonePath = path,
                localEuler = bone.transform.localEulerAngles
            });
        }

        UnityEditor.Undo.RecordObject(this, "Capture Carry Pose");

        carriedBonePoses = poses.ToArray();
        _poseBones = null;

        Transform socket = transform.parent;
        bool isOnSocket = socket != null && socket.GetComponentInParent<CarrySystem>() != null;

        if (isOnSocket)
        {
            carriedLocalPosition = transform.localPosition;
            carriedLocalEuler = transform.localEulerAngles;
        }

        UnityEditor.EditorUtility.SetDirty(this);

        Debug.Log($"[CarriableBody] 본 {poses.Count}개의 자세를 저장했습니다." +
                  (isOnSocket
                      ? " 소켓 기준 위치/회전도 함께 저장했습니다."
                      : " (Preview Attach 로 소켓에 붙인 상태에서 저장해야 위치/회전도 같이 저장됩니다.)"), this);
    }

    /// <summary>저장된 자세를 지금 적용해 확인합니다.</summary>
    [ContextMenu("Apply Captured Pose")]
    private void ApplyCapturedPose()
    {
        if (carriedBonePoses == null || carriedBonePoses.Length == 0)
        {
            Debug.LogWarning("[CarriableBody] 저장된 자세가 없습니다.", this);
            return;
        }

        int applied = 0;

        foreach (BonePose pose in carriedBonePoses)
        {
            if (string.IsNullOrEmpty(pose.bonePath))
            {
                continue;
            }

            Transform bone = transform.Find(pose.bonePath);

            if (bone == null)
            {
                Debug.LogWarning($"[CarriableBody] 본 경로 '{pose.bonePath}'를 찾지 못했습니다.", this);
                continue;
            }

            UnityEditor.Undo.RecordObject(bone, "Apply Carry Pose");
            bone.localEulerAngles = pose.localEuler;
            applied++;
        }

        Debug.Log($"[CarriableBody] 본 {applied}개에 저장된 자세를 적용했습니다.", this);
    }

    /// <summary>에디터에서 플레이어 소켓에 붙여 어떻게 안기는지 확인합니다.</summary>
    [ContextMenu("Preview Attach To Socket")]
    private void PreviewAttach()
    {
        Transform socket = FindPlayerSocket();

        if (socket == null)
        {
            return;
        }

        // 이미 소켓(또는 플레이어) 밑에 있으면 그걸 "원래 부모"로 덮어쓰면 안 됩니다.
        bool isAlreadyOnPlayer = transform.parent != null
            && transform.parent.GetComponentInParent<CarrySystem>() != null;

        if (!isAlreadyOnPlayer)
        {
            UnityEditor.Undo.RecordObject(this, "Preview Carry");

            previewOriginalParent = transform.parent;
            previewOriginalLocalPosition = transform.localPosition;
            previewOriginalLocalEuler = transform.localEulerAngles;

            Rigidbody[] bones = GetComponentsInChildren<Rigidbody>(true);
            List<string> paths = new List<string>();
            List<Vector3> eulers = new List<Vector3>();

            foreach (Rigidbody bone in bones)
            {
                if (bone.transform == transform)
                {
                    continue;
                }

                string path = GetRelativePath(transform, bone.transform);

                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                paths.Add(path);
                eulers.Add(bone.transform.localEulerAngles);
            }

            previewOriginalBonePaths = paths.ToArray();
            previewOriginalBoneEulers = eulers.ToArray();

            UnityEditor.EditorUtility.SetDirty(this);
        }

        UnityEditor.Undo.SetTransformParent(transform, socket, "Preview Carry");

        transform.localPosition = carriedLocalPosition;
        transform.localEulerAngles = carriedLocalEuler;

        if (carriedBonePoses != null && carriedBonePoses.Length > 0)
        {
            ApplyCapturedPose();
        }

        Debug.Log($"[CarriableBody] '{socket.name}'에 붙였습니다. 자세를 조정한 뒤 Capture Current Pose, 끝나면 Preview Detach 를 누르세요.", this);
    }

    /// <summary>미리보기를 풀고, 자세 잡느라 돌려놓은 본을 전부 원래대로 되돌립니다.</summary>
    [ContextMenu("Preview Detach")]
    private void PreviewDetach()
    {
        if (previewOriginalParent == null)
        {
            Debug.LogError(
                "[CarriableBody] 원래 부모가 기록되어 있지 않아 되돌릴 수 없습니다.\n" +
                $"Hierarchy에서 '{name}'을(를) 원래 본(예: BoneRoot) 아래로 직접 드래그하거나, " +
                "인스펙터의 Preview Original Parent에 그 본을 넣고 다시 누르세요.", this);
            return;
        }

        if (previewOriginalParent.GetComponentInParent<CarrySystem>() != null)
        {
            Debug.LogError(
                "[CarriableBody] 기록된 원래 부모가 플레이어 쪽입니다. 잘못 기록된 값이라 되돌리지 않습니다.\n" +
                "Preview Original Parent에 시체의 본을 직접 넣어주세요.", this);
            return;
        }

        UnityEditor.Undo.SetTransformParent(transform, previewOriginalParent, "Preview Carry Detach");

        int restored = 0;

        if (previewOriginalBonePaths != null && previewOriginalBoneEulers != null)
        {
            int count = Mathf.Min(previewOriginalBonePaths.Length, previewOriginalBoneEulers.Length);

            for (int i = 0; i < count; i++)
            {
                Transform bone = transform.Find(previewOriginalBonePaths[i]);

                if (bone == null)
                {
                    continue;
                }

                UnityEditor.Undo.RecordObject(bone, "Preview Carry Detach");
                bone.localEulerAngles = previewOriginalBoneEulers[i];
                restored++;
            }
        }

        UnityEditor.Undo.RecordObject(transform, "Preview Carry Detach");
        transform.localPosition = previewOriginalLocalPosition;
        transform.localEulerAngles = previewOriginalLocalEuler;

        Debug.Log($"[CarriableBody] 미리보기 해제. 본 {restored}개를 원래 자세로 되돌렸습니다. 저장된 안긴 자세는 그대로입니다.", this);
    }

#endif
}
