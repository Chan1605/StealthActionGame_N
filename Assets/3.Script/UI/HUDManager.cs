using System.Collections.Generic;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [Header("UI 시스템")]
    [SerializeField] private UI_ObjKeyPanal KeyPanal_Prefab;
    [SerializeField] private UI_TargetMarker TargetMarker_Prefab;
    [SerializeField] private Canvas canvas;


    private UI_ObjKeyPanal KeyPanalUI;
    private readonly Dictionary<string, UI_TargetMarker> _markers = new Dictionary<string, UI_TargetMarker>();

    [SerializeField] private UI_MissionObjective ObjectiveUI_Scene; // 씬에 미리 배치한 것을 직접 연결

    public UI_MissionObjective GetObjective()
    {
        return ObjectiveUI_Scene;
    }
    public UI_ObjKeyPanal GetKeyPanal()
    {
        if (KeyPanalUI == null)
        {
            KeyPanalUI = Instantiate(KeyPanal_Prefab, canvas.transform);
        }

        return KeyPanalUI;
    }

    public UI_TargetMarker GetTargetMarker(string missionId = "default")
    {
        if (!_markers.TryGetValue(missionId, out UI_TargetMarker marker) || marker == null)
        {
            marker = Instantiate(TargetMarker_Prefab, canvas.transform);
            _markers[missionId] = marker;
        }

        return marker;
    }


}