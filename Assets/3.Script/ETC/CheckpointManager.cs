using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    private Vector3 _position;
    private Quaternion _rotation;
    private bool _hasCheckpoint;
    private EnemySegment _pendingSegment;

    private void Awake()
    {
        Instance = this;
    }

    public void SetCheckpoint(Vector3 position, Quaternion rotation, EnemySegment pendingSegment)
    {
        _position = position;
        _rotation = rotation;
        _hasCheckpoint = true;
        _pendingSegment = pendingSegment;
    }

    public bool TryGetCheckpoint(out Vector3 position, out Quaternion rotation)
    {
        position = _position;
        rotation = _rotation;
        return _hasCheckpoint;
    }

    public void ResetPendingSegment()
    {
        _pendingSegment?.ResetSegment();
    }
}