using UnityEngine;

public class DummyDetectable : MonoBehaviour, IDetectable
{
    [Header("테스트용 토글 (인스펙터에서 조작)")]
    [SerializeField, Range(0f, 1f)] private float stealthWeight = 1f;
    [SerializeField] private bool isCrouching;
    [SerializeField] private bool isMoving;
    [SerializeField] private float testSoundIntensity;

    public Vector3 Position => transform.position;
    public float StealthWeight => stealthWeight;
    public bool IsCrouching => isCrouching;
    public bool IsMoving => isMoving;
    public float SoundIntensity => testSoundIntensity;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.Lerp(new Color(0.3f, 0.3f, 0.3f, 0.6f), Color.white, stealthWeight);
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
#endif
}
