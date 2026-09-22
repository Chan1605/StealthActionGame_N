using System;
using DG.Tweening;
using UnityEngine;

public class LookAroundCommand : ICommand
{
    private readonly Transform _transform;
    private readonly EnemyMovement _movement;
    private readonly float _duration;
    private readonly float _angle;
    private readonly Vector3? _focusPosition;
    private Tween _tween;

    public LookAroundCommand(Transform transform, EnemyMovement movement, float duration, float angle, Vector3? focusPosition = null)
    {
        _transform = transform;
        _movement = movement;
        _duration = duration;
        _angle = angle;
        _focusPosition = focusPosition;
    }

    public void Start(EnemyStateMachine fsm, Action onComplete)
    {
        _movement.SetAutoRotation(false);

        Vector3 baseEuler = _transform.eulerAngles;
        float turnDuration = 0f;

        if (_focusPosition.HasValue)
        {
            Vector3 dir = _focusPosition.Value - _transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                baseEuler = Quaternion.LookRotation(dir).eulerAngles;
            turnDuration = _duration * 0.2f;
        }

        float remaining = _duration - turnDuration;
        Sequence seq = DOTween.Sequence();

        if (turnDuration > 0f)
            seq.Append(_transform.DORotate(baseEuler, turnDuration).SetEase(Ease.OutSine));

        seq.Append(_transform.DORotate(baseEuler + Vector3.up * _angle, remaining * 0.35f).SetEase(Ease.InOutSine));
        seq.Append(_transform.DORotate(baseEuler - Vector3.up * _angle, remaining * 0.5f).SetEase(Ease.InOutSine));
        seq.Append(_transform.DORotate(baseEuler, remaining * 0.15f).SetEase(Ease.InOutSine));
        seq.OnComplete(() =>
        {
            _movement.SetAutoRotation(true);
            onComplete?.Invoke();
        });
        _tween = seq;
    }

    public void Tick() { }

    public void Cancel()
    {
        _tween?.Kill();
        _movement.SetAutoRotation(true);
    }
}