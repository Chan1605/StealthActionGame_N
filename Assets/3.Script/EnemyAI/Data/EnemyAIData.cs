using UnityEngine;

[CreateAssetMenu(menuName = "EnemyAI/EnemyAIData", fileName = "New EnemyAIData")]
public class EnemyAIData : ScriptableObject
{
    [Header("시야")]
    public float viewAngle = 90f;
    public float viewRangeShort = 5f;
    public float instantDetectRange = 1.5f;
    public float instantDetectAngle = 270f;
    public float viewRangeMid = 12f;
    public float viewRangeLong = 20f;
    public float viewScorePerSecShort = 30f;
    public float viewScorePerSecMid = 10f;
    public float viewScorePerSecLong = 3f;
    public float crouchViewWeight = 0.5f;

    [Header("청각")]
    public float hearingRadius = 15f;
    public float soundMemoryLockDuration = 3f;

    [Header("의심 임계치")]
    public float weakSuspicionThreshold = 30f;
    public float strongSuspicionThreshold = 70f;
    public float maxScore = 100f;

    [Header("점수 감쇠")]
    public float scoreDecayPerSec = 5f;
    public float scoreDropOnFailedCheck = 40f;
    public float hiddenDecayMultiplier = 4f;
    public bool isHiddenRequiresCrouch = true;

    [Header("이동")]
    public float patrolSpeed = 2f;
    public float investigateSpeed = 3.5f;
    public float stoppingDistance = 0.15f;
    [Header("추적 (발견 상태)")]
    public float chaseSpeed = 4f;
    public float chaseRepathInterval = 0.2f;
    public float unreachableGiveUpTime = 2.5f;
    [Header("공격 (발견 상태)")]
    public float attackRange = 1.8f;
    public float attackDamage = 10f;
    public float attackWindupTime = 0.4f;
    public float attackRecoveryTime = 0.6f;
    [Header("확인 행동")]
    public float investigateCheckDuration = 2.5f;
    public float soundInvestigateCheckDuration = 7f;
    public float investigateCatchRadius = 5f; //알람을 울리고 플레이어가 어느정도에 있으면 걸리도록 확인하는 거리

    [Header("배치 연출")]
    public float spawnMoveSpeed = 2.5f;
    [Header("발견 복귀")]
    public float detectedLoseTime = 4f;
    [Header("특수 행동")]
    public float patrolIdleChance = 0.4f;
    public float patrolIdleMinDuration = 1.5f;
    public float patrolIdleMaxDuration = 3f;
    public float lookAroundAngle = 45f;
    public float singlePointIdleInterval = 4f;
    public float soundGlanceDuration = 1.5f;
    [Header("시체 발견")]
    public LayerMask corpseLayer;
    public float corpseDetectRadius = 12f;
    public float corpseAlertDuration = 8f;
    [Header("구역 시스템 (죄수 허용 구역)")]
    public int suspiciousActionLimit = 2;
    public float suspiciousActionCooldown = 1.5f;

}
