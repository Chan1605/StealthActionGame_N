using System.Collections.Generic;
using UnityEngine;

public enum PatrolMode
{
    Loop,       // 마지막 지점 이후 처음으로 순환
    PingPong,   // 마지막 지점 도달 후 역순으로 왕복
    SinglePoint // 단일 지점 대기형 (Waypoints[0]만 사용)
}

/// <summary>
/// 적 하나가 순찰할 경로를 정의한다. 씬에 배치된 빈 오브젝트(Transform)들을
/// 인스펙터에서 순서대로 등록해서 사용한다.
/// </summary>
public class WaypointGroup : MonoBehaviour
{
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField] private PatrolMode mode = PatrolMode.Loop;

    private int _direction = 1; // PingPong 모드에서 진행 방향

    public int Count => waypoints.Count;
    public bool IsSinglePoint => mode == PatrolMode.SinglePoint || waypoints.Count <= 1;

    public Vector3 GetPosition(int index)
    {
        if (waypoints.Count == 0)
        {
            Debug.LogWarning($"{name}: WaypointGroup에 등록된 지점이 없습니다.");
            return transform.position;
        }
        index = Mathf.Clamp(index, 0, waypoints.Count - 1);
        return waypoints[index].position;
    }

    /// <summary>
    /// 현재 인덱스를 기준으로 다음 순찰 인덱스를 계산한다.
    /// SinglePoint 모드에서는 항상 0을 반환한다.
    /// </summary>
    public int GetNextIndex(int currentIndex)
    {
        if (IsSinglePoint || waypoints.Count == 0) return 0;

        switch (mode)
        {
            case PatrolMode.Loop:
                return (currentIndex + 1) % waypoints.Count;

            case PatrolMode.PingPong:
                int next = currentIndex + _direction;
                if (next >= waypoints.Count || next < 0)
                {
                    _direction *= -1;
                    next = currentIndex + _direction;
                }
                return Mathf.Clamp(next, 0, waypoints.Count - 1);

            default:
                return currentIndex;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);
            if (i < waypoints.Count - 1 && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
        if (mode == PatrolMode.Loop && waypoints.Count > 1 && waypoints[0] != null && waypoints[waypoints.Count - 1] != null)
            Gizmos.DrawLine(waypoints[waypoints.Count - 1].position, waypoints[0].position);
    }
#endif
}
