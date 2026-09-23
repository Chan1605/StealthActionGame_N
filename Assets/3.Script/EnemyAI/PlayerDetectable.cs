using UnityEngine;

public class PlayerDetectable : MonoBehaviour, IDetectable
{
    [SerializeField] private CharacterController controller;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private float walkSpeedThreshold = 0.1f;
    [SerializeField] private float runSpeedThreshold = 4f;
    [SerializeField] private float walkSoundScore = 20f;
    [SerializeField] private float runSoundScore = 40f;

    [Header("개발자모드")]
    [SerializeField, Range(0f, 1f)] private float testStealthWeight = 1f;

    [Header("디버그 (읽기 전용)")]
    [SerializeField] private float debugSpeed;
    [SerializeField] private float debugSoundIntensity;

    public Vector3 Position => transform.position;
    public bool IsCrouching => playerController != null && playerController.isCrouched;
    public float StealthWeight => IsDeadOrRespawning ? 0f : testStealthWeight;
    public bool IsDeadOrRespawning { get; set; }

    public float SoundIntensity
    {
        get
        {
            if (IsDeadOrRespawning) return 0f;
            debugSpeed = new Vector3(controller.velocity.x, 0f, controller.velocity.z).magnitude;

            float intensity;
            if (debugSpeed >= runSpeedThreshold) intensity = runSoundScore;
            else if (debugSpeed >= walkSpeedThreshold) intensity = walkSoundScore;
            else intensity = 0f;

            if (IsCrouching) intensity *= 0.5f;

            debugSoundIntensity = intensity;
            return intensity;
        }
    }

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (playerController == null) playerController = GetComponent<PlayerController>();

        if (controller == null)
            Debug.LogError($"{name}: CharacterController를 찾지 못했습니다.");
        if (playerController == null)
            Debug.LogWarning($"{name}: PlayerController를 찾지 못했습니다. IsCrouching이 항상 false로 처리됩니다.");
        KeyInventory inventory = GetComponent<KeyInventory>();
        if (inventory != null && GameSession.Instance != null)
        {
            inventory.RestoreFrom(GameSession.Instance.GetPendingKeys());
        }
    }
}