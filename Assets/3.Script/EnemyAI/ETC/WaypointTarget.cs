using System;
using UnityEngine;

public class WaypointTarget : MonoBehaviour, IInteractable
{
    public Transform ObjectTransform => transform;
    public Action OnUse { get; set; }
    public Action OnLook { get; set; }
    public event Action OnTargetCompleted;

    public bool IsPlayerLook { get; set; }
    public bool IsInteractable { get; private set; } = true;
    private bool _isTracking; // StageManager°¡ ¼ø¹ø ¾Æ´Ò ¶§ ²û

    public void EnableInteraction() { _isTracking = true; }
    public void DisableInteraction() { _isTracking = false; }
    [SerializeField] private float completeRadius = 1.5f;
    private Transform _player;
    private bool _isCompleted;

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
    }

    private void Update()
    {
        if (_isCompleted || _player == null || !_isTracking) return;

        if (Vector3.Distance(_player.position, transform.position) <= completeRadius)
        {
            _isCompleted = true;
            OnTargetCompleted?.Invoke();
        }
    }
}