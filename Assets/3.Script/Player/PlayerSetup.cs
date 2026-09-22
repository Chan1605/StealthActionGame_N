using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerInput))]

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerCameraRig))]
[RequireComponent(typeof(PlayerAnimator))]

[RequireComponent(typeof(PlayerInteractor))]
[RequireComponent(typeof(PlayerInteractionRunner))]
[RequireComponent(typeof(PlayerHand))]

[RequireComponent(typeof(PlayerThrow))]
[RequireComponent(typeof(PlayerWallClimb))]
[RequireComponent(typeof(AssassinationSystem))]

[RequireComponent(typeof(PlayerDetectable))]
[RequireComponent(typeof(VisionManager))]
public class PlayerSetup : MonoBehaviour
{
    [Header("Expected")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string playerLayerName = "Player";

    [Header("Check")]
    [SerializeField] private bool isCheckOnStart = true;

    private static readonly string[] RequiredParameters =
    {
        "MoveX", "MoveY", "Speed", "IsCrouched", "IsGrounded",
        "Jump", "Assassinate", "OpenDoor", "CloseDoor",
        "PickUp", "PutDown", "Throw", "Climb"
    };

    private void Start()
    {
        if (isCheckOnStart)
        {
            CheckSetup();
        }
    }

    [ContextMenu("Check Player Setup")]
    public void CheckSetup()
    {
        List<string> problems = new List<string>();

        CheckTagAndLayer(problems);
        CheckAnimator(problems);
        CheckController(problems);
        CheckLinks(problems);

        if (problems.Count == 0)
        {
            Debug.Log("[PlayerSetup] 세팅 이상 없습니다.", this);
            return;
        }

        Debug.LogWarning($"[PlayerSetup] 확인이 필요한 항목 {problems.Count}개\n- {string.Join("\n- ", problems)}", this);
    }

    private void CheckTagAndLayer(List<string> problems)
    {
        if (!string.IsNullOrEmpty(playerTag) && !CompareTag(playerTag))
        {
            problems.Add($"Tag가 '{playerTag}'가 아닙니다. (현재 '{tag}') UI_TargetMarker가 FindWithTag로 찾습니다.");
        }

        if (string.IsNullOrEmpty(playerLayerName))
        {
            return;
        }

        int expected = LayerMask.NameToLayer(playerLayerName);

        if (expected < 0)
        {
            problems.Add($"'{playerLayerName}' 레이어가 프로젝트에 없습니다.");
            return;
        }

        if (gameObject.layer != expected)
        {
            problems.Add($"Layer가 '{playerLayerName}'가 아닙니다. (현재 '{LayerMask.LayerToName(gameObject.layer)}')");
        }
    }

    private void CheckAnimator(List<string> problems)
    {
        Animator animator = GetComponent<Animator>();

        if (animator.runtimeAnimatorController == null)
        {
            problems.Add("Animator에 Controller가 없습니다.");
            return;
        }

        if (animator.avatar == null || !animator.avatar.isHuman)
        {
            problems.Add("Animator의 Avatar가 비었거나 Humanoid가 아닙니다. 손 IK가 동작하지 않습니다.");
        }

        if (animator.applyRootMotion)
        {
            problems.Add("Apply Root Motion이 켜져 있습니다. 이동을 스크립트가 담당하므로 꺼야 합니다.");
        }

        HashSet<string> names = new HashSet<string>();

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            names.Add(parameter.name);
        }

        List<string> missing = new List<string>();

        foreach (string required in RequiredParameters)
        {
            if (!names.Contains(required))
            {
                missing.Add(required);
            }
        }

        if (missing.Count > 0)
        {
            problems.Add($"Animator 파라미터 누락: {string.Join(", ", missing)}");
        }

        if (animator.layerCount < 3)
        {
            problems.Add($"Animator 레이어가 {animator.layerCount}개입니다. Base / Action / UpperBody 3개가 필요합니다.");
        }
    }

    private void CheckController(List<string> problems)
    {
        CharacterController controller = GetComponent<CharacterController>();

        float bottom = controller.center.y - controller.height * 0.5f;

        if (Mathf.Abs(bottom) > 0.25f)
        {
            problems.Add($"CharacterController 캡슐 바닥이 발에서 {bottom:F2}m 떨어져 있습니다. Center Y와 Height를 확인하세요.");
        }

        if (controller.radius * 2f > controller.height)
        {
            problems.Add("CharacterController의 Radius가 Height의 절반보다 큽니다.");
        }
    }

    private void CheckLinks(List<string> problems)
    {
        PlayerHand hand = GetComponent<PlayerHand>();

        if (hand != null && hand.socket == null)
        {
            problems.Add("PlayerHand의 Hand Socket이 비었습니다. 손 본 아래 빈 오브젝트를 연결하세요.");
        }

        if (GetComponentInChildren<ThrowTrajectory>(true) == null)
        {
            problems.Add("자식에 ThrowTrajectory가 없습니다. 던지기 조준선이 표시되지 않습니다.");
        }

        if (Camera.main == null)
        {
            problems.Add("MainCamera 태그가 붙은 카메라가 씬에 없습니다.");
        }
    }
}
