using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteractable
{
    Transform ObjectTransform { get; }
    Action OnLook { get; set; }
    Action OnUse { get; set; }

    event Action OnTargetCompleted;

    bool IsInteractable {get;}
    bool IsPlayerLook { get; set; }

    public void EnableInteraction();
    public void DisableInteraction();
}
