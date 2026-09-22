using System;
using System.Collections;
using UnityEngine;

public class AlarmTriggerAction : InteractionAction
{
    [Header("경보 소리")]
    [SerializeField] private float alarmSoundIntensity = 90f;
    [SerializeField] private float alarmSoundRadius = 25f;
    [SerializeField] private float alarmDuration = 6f;
    [SerializeField] private float alarmPulseInterval = 0.5f;

    [Header("연출 (선택)")]
    [SerializeField] private GameObject alarmVisualEffect;
    [SerializeField] private AudioSource alarmAudioSource;

    // 나중에 UI/목표 연동이 필요할 때 구독
    // 지금은 아무도 구독하지 않아도 정상 동작한다
    public static event Action<Vector3> OnAlarmTriggered;

    private bool _isAlarming;

    protected override bool CanExecute(Transform user)
    {
        return !_isAlarming;
    }

    protected override void OnExecute(Transform user)
    {
        StartCoroutine(Alarm_co());
    }

    private IEnumerator Alarm_co()
    {
        _isAlarming = true;


        if (alarmVisualEffect != null)
        {
            alarmVisualEffect.SetActive(true);
        }
        if (alarmAudioSource != null)
        {
            alarmAudioSource.Play();
        }

        // 구독자가 있을 때만 호출 (없으면 null이라 그냥 건너뜀)
        if (OnAlarmTriggered != null)
        {
            OnAlarmTriggered(transform.position);
        }

        // 경보 시작 시점에 한 번만 찾아서 재사용
        EnemyPerception[] enemies = FindObjectsByType<EnemyPerception>(FindObjectsSortMode.None);

        float elapsed = 0f;
        while (elapsed < alarmDuration)
        {
            EmitAlarmSound(enemies);
            yield return new WaitForSeconds(alarmPulseInterval);
            elapsed += alarmPulseInterval;
        }

        if (alarmVisualEffect != null)
        {
            alarmVisualEffect.SetActive(false);
        }
        if (alarmAudioSource != null)
        {
            alarmAudioSource.Stop();
        }

        _isAlarming = false;
    }

    private void EmitAlarmSound(EnemyPerception[] enemies)
    {
        foreach (EnemyPerception enemy in enemies)
        {
            // 경보 도중 파괴된 적이 있을 수 있으니 null 체크
            if (enemy == null)
            {
                continue;
            }
            enemy.RegisterSound(transform.position, alarmSoundIntensity, true, alarmSoundRadius);
        }
    }
}