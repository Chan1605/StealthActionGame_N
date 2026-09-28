using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_StartButton : MonoBehaviour
{
    public void StartLoading()
    {
        GameSession.Instance.ResetRun();   // 이전 판의 기록(암살 수/플레이 타임/키) 초기화
        GameManager.Instance.LoadWithLoadingScreen("DayScene");
    }
}
