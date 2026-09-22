using System;
using UnityEngine;

public class PrisonScheduleManager : MonoBehaviour
{
    public static PrisonScheduleManager Instance { get; private set; }

    [SerializeField] private bool startAsFreeTime = true;

    public bool IsFreeTime { get; private set; }

    public event Action<bool> OnScheduleChanged;

    private void Awake()
    {
        Instance = this;
        IsFreeTime = startAsFreeTime;
    }

    private void Start()
    {
        OnScheduleChanged?.Invoke(IsFreeTime);
    }

    public void SetFreeTime(bool isFreeTime)
    {
        if (IsFreeTime == isFreeTime) return;
        IsFreeTime = isFreeTime;
        OnScheduleChanged?.Invoke(isFreeTime);
    }
}