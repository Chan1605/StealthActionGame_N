using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionSelector : MonoBehaviour
{
    public static MissionSelector Instance { get; private set; }

    // 마지막 미션 자동 시작까지의 대기 시간.
    // StageManager의 "미션 완료" 문구 표시 시간(2초)보다 길어야 UI가 안 겹친다.
    [SerializeField] private float autoStartDelay = 2.5f;

    private readonly List<MissionStartTrigger> _allTriggers = new List<MissionStartTrigger>();
    private MissionStartTrigger _activeMission;

    private void Awake()
    {
        Instance = this;
    }

    public void Register(MissionStartTrigger trigger)
    {
        _allTriggers.Add(trigger);
    }

    public bool CanStart(MissionStartTrigger trigger)
    {
        return _activeMission == null || _activeMission == trigger;
    }

    public void SelectMission(MissionStartTrigger chosen)
    {
        _activeMission = chosen;

        foreach (MissionStartTrigger trigger in _allTriggers)
        {
            if (trigger != chosen)
            {
                trigger.LockEntrance();
            }
        }
    }

    public void ReleaseMission()
    {
        _activeMission = null;

        // 아직 완료되지 않은 미션이 몇 개인지 센다.
        int remainingCount = 0;
        MissionStartTrigger lastTrigger = null;

        foreach (MissionStartTrigger trigger in _allTriggers)
        {
            if (trigger.IsCompleted)
            {
                continue;
            }

            remainingCount++;
            lastTrigger = trigger;
        }

        // 남은 미션이 딱 1개면 트리거를 밟지 않아도 자동으로 시작한다.
        // 이 경우 UnlockEntrance는 부르지 않는다. (입구를 열 필요가 없고, 마커는 StageManager가 첫 타겟으로 세팅한다.)
        if (remainingCount == 1)
        {
            StartCoroutine(AutoStart_co(lastTrigger));
            return;
        }

        // 2개 이상 남았으면 기존처럼 다시 선택할 수 있게 입구를 연다.
        foreach (MissionStartTrigger trigger in _allTriggers)
        {
            trigger.UnlockEntrance();
        }
    }

    private IEnumerator AutoStart_co(MissionStartTrigger trigger)
    {
        // 이전 미션의 "미션 완료" 문구가 끝날 때까지 기다린다.
        yield return new WaitForSeconds(autoStartDelay);
        trigger.StartMission();
    }
}