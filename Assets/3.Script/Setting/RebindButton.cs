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

    private void StartRebind()
    {
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

                rebindButton.Select();

                op.Dispose();
            })
            .OnCancel(op =>
            {
                actionRef.action.Enable();
                rebindButton.interactable = true;

                UpdateLabel();

                rebindButton.Select();

                op.Dispose();
            })
            .Start();
    }

    private void ClearDuplicateBindings()
    {
        string newBindingPath = actionRef.action.bindings[bindingIndex].effectivePath;

        foreach (var action in actionRef.asset)
        {
            if ( action == actionRef.action)
            {
                continue;
            }

            for (int i = 0; i < action.bindings.Count;i++)
            {
                if(action.bindings[i].effectivePath == newBindingPath)
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
