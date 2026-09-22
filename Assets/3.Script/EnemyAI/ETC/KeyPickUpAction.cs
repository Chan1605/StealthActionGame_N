using UnityEngine;

public class KeyPickUpAction : InteractionAction
{
    [Header("Key")]
    [SerializeField] private string keyId;

    public string id => keyId;

    protected override bool CanExecute(Transform user)
    {
        return true;
    }

    protected override void OnExecute(Transform user)
    {
        KeyInventory inventory = user.GetComponentInChildren<KeyInventory>();
        inventory?.AddKey(keyId);
        Debug.Log($"[KeyPickUp] 저장 대상 KeyInventory 오브젝트명: {inventory?.gameObject.name}, InstanceID: {inventory?.GetInstanceID()}");
        if (TryGetComponent(out GeneralObject generalObject))
        {
            generalObject.KeyPanal_Off();
        }

        HUDManager hud = FindAnyObjectByType<HUDManager>();
        hud?.GetTargetMarker()?.SetMarker_Off();

        gameObject.SetActive(false);
    }
}