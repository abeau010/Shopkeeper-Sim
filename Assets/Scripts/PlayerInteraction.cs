using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactRadius = 0.5f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private PlayerHands playerHands;
    [SerializeField] private InputAction interactAction;
    private ControlsUI controlsUI;
    private void OnEnable() => interactAction.Enable();
    private void OnDisable() => interactAction.Disable();

    private void Awake()
    {
        controlsUI = gameObject.AddComponent<ControlsUI>();
    }

    private void Update()
    {
        // Ignore E and Q while the price picker or a list is open
        if (PriceSelectUI.IsOpen || ListMenu.IsOpen) return;

        if (interactAction.WasPressedThisFrame())
        {
            TryInteract();
        }
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
        {
            ReturnNewestItem();
        }
    }

    // Sends the most recently picked up item back to its shelf from anywhere in the shop
    private void ReturnNewestItem()
    {
        int hand = playerHands.GetNewestHand();
        if (hand < 0) return;

        ItemSO item = playerHands.hands[hand];
        foreach (Shelf shelf in FindObjectsByType<Shelf>())
        {
            foreach (ShelfSlot slot in shelf.slots)
            {
                if (slot.item == item && shelf.TryReturn(slot, playerHands)) return;
            }
        }
        controlsUI.ShowMessage($"No room on the shelf for {item.itemName}");
    }

    private void TryInteract() {
        IInteractable interactable = FindNearestInteractable();
        if (interactable != null)
        {
            interactable.Interact(playerHands);
            // Debug.Log($"Interacted with target. Current status: {interactable.GetInteractionPrompt()}");
            // ^^ Use when debugging
        }
    }

    // Picks the closest target when more than one is in range
    private IInteractable FindNearestInteractable()
    {
        IInteractable nearest = null;
        float nearestDistance = float.MaxValue;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, interactRadius, interactableLayer))
        {
            float distance = (hit.transform.position - transform.position).sqrMagnitude;
            if (distance < nearestDistance && hit.TryGetComponent(out IInteractable interactable))
            {
                nearest = interactable;
                nearestDistance = distance;
            }
        }
        return nearest;
    }

    // Used for debugging
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
