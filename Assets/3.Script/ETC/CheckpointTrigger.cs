using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;

    [Tooltip("이 체크포인트 통과 후 다음 체크포인트까지의 적 구간. 레벨 디자인 전이면 비워둬도 됩니다.")]
    [SerializeField] private EnemySegment nextSegment;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Vector3 position = respawnPoint != null ? respawnPoint.position : transform.position;
        Quaternion rotation = respawnPoint != null ? respawnPoint.rotation : transform.rotation;

        CheckpointManager.Instance?.SetCheckpoint(position, rotation, nextSegment);
    }
}