using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

[Serializable]
public struct Narration_Data
{
    public string subtitleText;
    public EventReference audioEvent;
    public float duration;

    public bool isObjectSound;
}
