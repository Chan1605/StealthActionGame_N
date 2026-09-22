using System.Collections.Generic;
using UnityEngine;

public class EnemySegment : MonoBehaviour
{
    [Tooltip("이 구간을 지키는 적들. 레벨 디자인 완료 후 여기에 등록하면 됩니다.")]
    [SerializeField] private List<EnemyAI> enemies = new List<EnemyAI>();

    public void ResetSegment()
    {
        foreach (EnemyAI enemy in enemies)
        {
            if (enemy == null) continue;
            enemy.ReviveForCheckpoint();
        }
    }
}