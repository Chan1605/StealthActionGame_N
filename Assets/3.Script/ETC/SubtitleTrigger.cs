using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubtitleTrigger : MonoBehaviour
{
    [Header("자막 데이터")]
    [SerializeField] private List<Narration_Data> lookSubtitles;
    [SerializeField] private List<Narration_Data> endSubtitles;

    private IInteractable target;
    private UI_Subtitle subtitleUI;

    private bool hasPlayerLook = false;

    private void Awake()
    {
        target = GetComponent<IInteractable>();
    }
    private void Start()
    {
        subtitleUI = FindAnyObjectByType<UI_Subtitle>();

        if (lookSubtitles != null && lookSubtitles.Count > 0)
        {
            target.OnLook += HandleLook;
        }

        if (endSubtitles != null && endSubtitles.Count > 0)
        {
            target.OnTargetCompleted += HandleComplete;
        }
    }
    private void HandleLook()
    {
        if (!hasPlayerLook && lookSubtitles.Count > 0)
        {
            subtitleUI.PlaySequence(lookSubtitles, transform.position);
            hasPlayerLook = true;
        }
    }

    private void HandleComplete()
    {
        if(endSubtitles.Count > 0)
        {
            subtitleUI.PlaySequence(endSubtitles, transform.position);
        }
    }

    private void OnDestroy()
    {
        if(target!=null)
        {
            target.OnLook -= HandleLook;
            target.OnTargetCompleted -= HandleComplete;
        }
    }


}
