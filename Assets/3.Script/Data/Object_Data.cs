using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

[CreateAssetMenu(fileName ="ObjectData", menuName ="ScriptableObject/Object Data")]
public class Object_Data : ScriptableObject
{
    [Header("UI 표시 정보")]
    [SerializeField] public Enum_ActionName ActionName;
    [SerializeField] public string Behaviour;
    [SerializeField] public string Caution;

    [Header("사운드")]
    [SerializeField] public EventReference SoundEvent;
}
