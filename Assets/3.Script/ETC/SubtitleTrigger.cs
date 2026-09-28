using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubtitleTrigger : MonoBehaviour
{
    [Header("자막 데이터")]
    [SerializeField] private List<Narration_Data> lookSubtitles;
    [SerializeField] private List<Narration_Data> endSubtitles;

    public List<Narration_Data> LookSubtitles => lookSubtitles;
    public List<Narration_Data> EndSubtitles => endSubtitles;

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

        if (subtitleUI == null)
        {
            Debug.LogWarning($"[SubtitleTrigger] '{name}' 씬에서 UI_Subtitle을 찾지 못했습니다. 자막이 재생되지 않습니다.", this);
        }

        if (lookSubtitles != null && lookSubtitles.Count > 0)
        {
            target.OnLook += HandleLook;
        }

        if (endSubtitles != null && endSubtitles.Count > 0)
        {
            target.OnUse += HandleComplete;
        }
    }
    private void HandleLook()
    {
        if (subtitleUI == null) return;

        if (!hasPlayerLook && lookSubtitles.Count > 0)
        {
            subtitleUI.PlaySequence(lookSubtitles, transform.position);
            hasPlayerLook = true;
        }
    }

    private void HandleComplete()
    {
        if (subtitleUI == null) return;

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
            target.OnUse -= HandleComplete;
        }
    }


}
