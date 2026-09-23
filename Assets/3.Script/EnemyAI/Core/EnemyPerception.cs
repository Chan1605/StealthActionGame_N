using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
public class EnemyPerception : MonoBehaviour
{
    [SerializeField] private Transform eyeOrigin;
    [SerializeField] private LayerMask obstacleMask = ~0;

    private EnemyAIData _data;
    private IDetectable _target;
    public IDetectable Target => _target;

    public float VisionScore { get; private set; }
    public float HearingScore { get; private set; }
    public float MaxScore => Mathf.Max(VisionScore, HearingScore);
    public Vector3 LastKnownPosition { get; private set; }
    public bool IsCurrentlySensing { get; private set; }
    public Vector3 SoundMemoryPosition { get; private set; }
    private float _soundMemoryIntensity;
    private float _soundLockTimer;
    private bool _soundRegisteredThisFrame;
    private bool _isTargetVisible;
    private readonly HashSet<TakedownVictim> _alertedCorpses = new HashSet<TakedownVictim>();
    public bool IsAlwaysAlert { get; set; }

    private int _suspiciousActionCount;
    public int SuspiciousActionCount => _suspiciousActionCount;
    private float _lastSuspiciousActionTime = -999f;
    public bool IsAlarmSource { get; private set; }

    private float GetDecayPerSec()
    {
        if (_isTargetVisible)
        {
            return _data.scoreDecayPerSec;
        }

        if (_data.isHiddenRequiresCrouch && (_target == null || !_target.IsCrouching))
        {
            return _data.scoreDecayPerSec;
        }

        return _data.scoreDecayPerSec * Mathf.Max(1f, _data.hiddenDecayMultiplier);
    }

    public void Initialize(EnemyAIData data, IDetectable target)
    {
        _data = data;
        _target = target;
        if (eyeOrigin == null) eyeOrigin = transform;
    }

    public void Tick(float deltaTime)
    {
        if (_target == null) return;

        if (!IsAlwaysAlert && PrisonScheduleManager.Instance != null && PrisonScheduleManager.Instance.IsFreeTime)
        {
            VisionScore = Mathf.Max(0f, VisionScore - _data.scoreDecayPerSec * deltaTime);
            HearingScore = Mathf.Max(0f, HearingScore - _data.scoreDecayPerSec * deltaTime);
            IsCurrentlySensing = false;
            return;
        }

        TickVision(deltaTime);
        TickHearing(deltaTime);
    }

    private void TickVision(float deltaTime)
    {
        float distance = Vector3.Distance(eyeOrigin.position, _target.Position);
        Vector3 toTarget = _target.Position - eyeOrigin.position;
        float instantAngle = Vector3.Angle(eyeOrigin.forward, toTarget);

        if (_target.StealthWeight >= 1f
            && distance <= _data.instantDetectRange
            && instantAngle <= _data.instantDetectAngle * 0.5f
            && HasLineOfSight(distance))
        {
            VisionScore = _data.maxScore;
            LastKnownPosition = _target.Position;
            IsCurrentlySensing = true;
            _isTargetVisible = true;
            return;
        }

        bool canSee = instantAngle <= _data.viewAngle * 0.5f && HasLineOfSight(distance);
        _isTargetVisible = canSee;

        float baseRate = 0f;
        if (canSee)
        {
            if (distance <= _data.viewRangeShort) baseRate = _data.viewScorePerSecShort;
            else if (distance <= _data.viewRangeMid) baseRate = _data.viewScorePerSecMid;
            else if (distance <= _data.viewRangeLong) baseRate = _data.viewScorePerSecLong;
        }

        float postureWeight = _target.IsCrouching ? _data.crouchViewWeight : 1f;
        float rate = baseRate * postureWeight * Mathf.Clamp01(_target.StealthWeight);

        if (rate > 0f)
        {
            VisionScore = Mathf.Min(_data.maxScore, VisionScore + rate * deltaTime);
            LastKnownPosition = _target.Position;
            IsCurrentlySensing = true;
        }
        else
        {
            VisionScore = Mathf.Max(0f, VisionScore - GetDecayPerSec() * deltaTime);
            IsCurrentlySensing = false;
        }
    }

    private void TickHearing(float deltaTime)
    {
        _soundLockTimer = Mathf.Max(0f, _soundLockTimer - deltaTime);
        _soundRegisteredThisFrame = false;

        if (_target != null)
        {
            float intensity = _target.SoundIntensity * Mathf.Clamp01(_target.StealthWeight);
            RegisterSound(_target.Position, intensity, false);
        }

        if (!_soundRegisteredThisFrame)
            HearingScore = Mathf.Max(0f, HearingScore - GetDecayPerSec() * deltaTime);
    }

    private bool HasLineOfSight(float distance)
    {
        Vector3 dir = (_target.Position - eyeOrigin.position).normalized;
        RaycastHit[] hits = Physics.RaycastAll(eyeOrigin.position, dir, distance, obstacleMask);

        foreach (var hit in hits)
        {
            if (Vector3.Distance(hit.point, _target.Position) > 0.5f)
                return false;
        }
        return true;
    }

    public bool IsSoundDominant => HearingScore > VisionScore;

    public Vector3 GetSuspectedPosition() => IsSoundDominant ? SoundMemoryPosition : LastKnownPosition;

    public void ReduceScoreSharply()
    {
        VisionScore = Mathf.Max(0f, VisionScore - _data.scoreDropOnFailedCheck);
        HearingScore = Mathf.Max(0f, HearingScore - _data.scoreDropOnFailedCheck);
        _soundLockTimer = 0f;
    }

    public void TryWitness(Vector3 eventPosition, float witnessRadius, float witnessAngle, LayerMask obstacleMask)
    {
        float distance = Vector3.Distance(eyeOrigin.position, eventPosition);
        if (distance > witnessRadius) return;

        Vector3 toEvent = eventPosition - eyeOrigin.position;
        float angle = Vector3.Angle(eyeOrigin.forward, toEvent);
        if (angle > witnessAngle * 0.5f) return;

        if (!HasClearLineToPoint(eventPosition, distance, obstacleMask)) return;

        VisionScore = _data.maxScore;
        LastKnownPosition = eventPosition;
    }

    public bool TryDetectCorpse(out Vector3 corpsePosition, LayerMask corpseLayer, float radius)
    {
        corpsePosition = Vector3.zero;
        Collider[] hits = Physics.OverlapSphere(eyeOrigin.position, radius, corpseLayer);
        if (hits.Length == 0) return false;

        float bestDist = float.MaxValue;
        bool found = false;

        foreach (Collider hit in hits)
        {
            TakedownVictim victim = hit.GetComponentInParent<TakedownVictim>();
            if (victim == null || _alertedCorpses.Contains(victim)) continue;

            Vector3 point = hit.transform.position;
            float dist = Vector3.Distance(eyeOrigin.position, point);
            if (dist >= bestDist) continue;
            if (!HasClearLineToPoint(point, dist, obstacleMask)) continue;

            bestDist = dist;
            corpsePosition = point;
            found = true;

            _alertedCorpses.Add(victim);
        }

        return found;
    }

    private bool HasClearLineToPoint(Vector3 point, float distance, LayerMask mask)
    {
        Vector3 dir = (point - eyeOrigin.position).normalized;
        RaycastHit[] hits = Physics.RaycastAll(eyeOrigin.position, dir, distance, mask);
        foreach (var hit in hits)
        {
            if (Vector3.Distance(hit.point, point) > 0.5f) return false;
        }
        return true;
    }

    public void ForceReset()
    {
        VisionScore = 0f;
        HearingScore = 0f;
        IsCurrentlySensing = false;
        _soundLockTimer = 0f;
        _alertedCorpses.Clear();
        _suspiciousActionCount = 0;
    }

    public void RegisterSound(Vector3 sourcePosition, float sourceIntensity, bool isInstant, float radius = -1f, bool isEnvironmental = false)
    {
        if (sourceIntensity <= 0f) return;

        float effectiveRadius = radius > 0f ? radius : _data.hearingRadius;
        float distance = Vector3.Distance(eyeOrigin.position, sourcePosition);
        float attenuated = sourceIntensity * Mathf.Clamp01(1f - distance / effectiveRadius);

        if (attenuated <= 0f) return;

        bool lockActive = _soundLockTimer > 0f;
        if (!lockActive || attenuated >= _soundMemoryIntensity)
        {
            SoundMemoryPosition = sourcePosition;
            _soundMemoryIntensity = attenuated;
            _soundLockTimer = _data.soundMemoryLockDuration;
            IsAlarmSource = isEnvironmental;
        }

        float gain;
        if (isInstant && attenuated >= _data.strongSuspicionThreshold)
        {
            gain = _data.maxScore;
        }
        else
        {
            gain = isInstant ? attenuated : attenuated * Time.deltaTime;
        }
        HearingScore = Mathf.Min(_data.maxScore, HearingScore + gain);
        _soundRegisteredThisFrame = true;
    }

    public void RegisterSuspiciousAction(float cooldown)
    {
        if (Time.time - _lastSuspiciousActionTime < cooldown)
        {
            return; // 쿨다운 중이면 이번 프레임은 카운트하지 않음
        }

        _lastSuspiciousActionTime = Time.time;
        _suspiciousActionCount++;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_data == null) return;
        Transform eye = eyeOrigin != null ? eyeOrigin : transform;

        Handles.color = new Color(1f, 0.6f, 0f, 0.15f);
        Vector3 leftEdge = Quaternion.AngleAxis(-_data.viewAngle * 0.5f, Vector3.up) * eye.forward;
        Handles.DrawSolidArc(eye.position, Vector3.up, leftEdge, _data.viewAngle, _data.viewRangeLong);

        Gizmos.color = Color.yellow;
        DrawViewBoundary(eye, -_data.viewAngle * 0.5f);
        DrawViewBoundary(eye, _data.viewAngle * 0.5f);

        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(eye.position, _data.hearingRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(eye.position, _data.instantDetectRange);
    }

    private void DrawViewBoundary(Transform eye, float angleOffset)
    {
        Vector3 dir = Quaternion.AngleAxis(angleOffset, Vector3.up) * eye.forward;
        Gizmos.DrawLine(eye.position, eye.position + dir * _data.viewRangeLong);
    }
#endif
}
