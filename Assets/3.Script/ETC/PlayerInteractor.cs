using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{

    [Header("레이캐스트 세팅")]
    [SerializeField] private Camera cam;
    [SerializeField] private float castRadius = 0.5f;
    [SerializeField] private float castDistance = 5f;
    [SerializeField] private LayerMask interactable;

    //debug 변경 InteractableObj -> IInteractable
    private IInteractable currentTarget;
    public IInteractable CurrentTarget
    {
        get
        {
            return currentTarget;
        }
    }

    private void Update()
    {
        DetectObject();
    }

    private void DetectObject()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);

        if (Physics.SphereCast(ray, castRadius, out RaycastHit hit, castDistance, interactable))
        {

            if (hit.collider.TryGetComponent<IInteractable>(out IInteractable hitObj) &&
                hitObj != currentTarget)
            {
                if (currentTarget != null)
                {
                    currentTarget.IsPlayerLook = false;
                }

                currentTarget = hitObj;

                if (currentTarget.IsInteractable)
                {
                    currentTarget.OnLook?.Invoke();

                    
                }
            }
        }
        else
        {
            if (currentTarget != null)
            {
                currentTarget.IsPlayerLook = false;
                currentTarget = null;
            }
        }
    }

    

}
