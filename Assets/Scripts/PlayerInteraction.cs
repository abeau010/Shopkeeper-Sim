using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactRadius = 1.5f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private PlayerHands playerHands;
    [SerializeField] private InputAction interactAction;
    private void OnEnable() => interactAction.Enable();
    private void OnDisable() => interactAction.Disable();

    private void Update()
    {
        if (interactAction.WasPressedThisFrame())
        {
            TryInteract();
        }
    }

    private void TryInteract() {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactRadius, interactableLayer);
        if (hit != null && hit.TryGetComponent(out IInteractable interactable))
        {
            interactable.Interact(playerHands);
            Debug.Log($"Interacted with target. Current status: {interactable.GetInteractionPrompt()}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}