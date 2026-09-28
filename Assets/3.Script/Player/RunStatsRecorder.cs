using UnityEngine;

// 플레이어에 붙여 둔다. 암살이 성공할 때마다 GameSession에 기록한다.
[RequireComponent(typeof(AssassinationSystem))]
public class RunStatsRecorder : MonoBehaviour
{
    private AssassinationSystem _assassination;

    private void Awake()
    {
        _assassination = GetComponent<AssassinationSystem>();
    }

    private void OnEnable()
    {
        if (_assassination != null)
        {
            _assassination.OnTakedownComplete += HandleTakedownComplete;
        }
    }

    private void OnDisable()
    {
        if (_assassination != null)
        {
            _assassination.OnTakedownComplete -= HandleTakedownComplete;
        }
    }

    private void HandleTakedownComplete(TakedownVictim victim)
    {
        GameSession session = GameSession.Instance;

        if (session != null)
        {
            session.AddAssassination();
        }
    }
}
