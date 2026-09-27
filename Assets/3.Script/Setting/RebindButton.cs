using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class RebindButton : MonoBehaviour
{
    [SerializeField] private InputActionReference actionRef;
    [SerializeField] private int bindingIndex;
    [SerializeField] private Button rebindButton;
    [SerializeField] private Text keyLabel;
    [SerializeField] private Text ActionName_t;
    [SerializeField] private string ActionName;

    private InputActionRebindingExtensions.RebindingOperation rebindOp;

    private static RebindButton activeRebindButton;
    public static event Action OnrebindGlobal;

    private void Awake()
    {
        rebindButton.onClick.AddListener(StartRebind);
        UpdateAction();
    }

    private void OnEnable()
    {
        OnrebindGlobal += UpdateLabel;
        UpdateLabel();
    }
    private void OnDisable()
    {
        OnrebindGlobal -= UpdateLabel;
    }
    public void CancelRebind()
    {
        if (rebindOp != null)
        {
            rebindOp.Cancel();
        }
    }
    private void StartRebind()
    {
        if (activeRebindButton != null)
        {
            if (activeRebindButton != this)
            {
                activeRebindButton.CancelRebind();
            }
        }
        activeRebindButton = this;

        rebindButton.interactable = false;
        keyLabel.text = "...";

        actionRef.action.Disable();
        rebindOp = actionRef.action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnComplete(op =>
            {
                actionRef.action.Enable();
                rebindButton.interactable = true;

                ClearDuplicateBindings();

                UpdateLabel();
                OnrebindGlobal?.Invoke();

                if (activeRebindButton == this)
                {
                    rebindButton.Select();
                }

                if (activeRebindButton == this)
                {
                    activeRebindButton = null;
                }

                op.Dispose();
            })
            .OnCancel(op =>
            {
                actionRef.action.Enable();
                rebindButton.interactable = true;

                UpdateLabel();

                rebindButton.Select();

                if (activeRebindButton == this)
                {
                    activeRebindButton = null;
                }
                op.Dispose();
            })
            .Start();
    }

    private void ClearDuplicateBindings()
    {
        string newBindingPath = actionRef.action.bindings[bindingIndex].effectivePath;

        foreach (var action in actionRef.asset)
        {
            for (int i = 0; i < action.bindings.Count;i++)
            {
                if (action == actionRef.action)
                {
                    if (i == bindingIndex)
                    {
                        continue;
                    }
                }

                if (action.bindings[i].effectivePath == newBindingPath)
                {
                    action.ApplyBindingOverride(i, "");
                }
            }
        }
    }



    private void UpdateAction()
    {
        ActionName_t.text = ActionName;
    }
    private void UpdateLabel()
    {
        keyLabel.text = actionRef.action.GetBindingDisplayString(bindingIndex);
    }
    private void OnDestroy()
    {
        rebindOp?.Dispose();
    }
}
