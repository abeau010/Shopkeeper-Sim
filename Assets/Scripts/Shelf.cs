using UnityEngine;
using TMPro;

public class Shelf : MonoBehaviour, IInteractable
{
    public ItemSO itemType;
    public int currentStock = 5;
    public int maxStock = 10;
    [SerializeField] private TextMeshProUGUI shelfText;

    // Try to take stock or restock
    public void Interact(PlayerHands playerHands)
    {
        if (playerHands.hands[0] == null || playerHands.hands[1] == null) // At least one hand empty
        {
            if (currentStock > 0 && playerHands.TryAddItem(itemType)) // Item(s) on the shelf and item fits in hands
            {
                currentStock--;
                UpdateVisuals();
                return;
            }
        }

        for (int i = 0; i < playerHands.hands.Length; i++)
        {
            if (playerHands.hands[i] == itemType && currentStock < maxStock)
            {
                playerHands.RemoveItem(i);
                currentStock++;
                UpdateVisuals();
            }
        }
    }

    public string GetInteractionPrompt() => $"Shelf: {itemType.itemName} ({currentStock}/{maxStock})";

    // Update shelf text on startup and hide text
    private void Start()
    {
        if (shelfText != null)
        {
            shelfText.text = GetInteractionPrompt();
            shelfText.gameObject.SetActive(false);
        }
    }

    // Update text
    private void UpdateVisuals()
    {
        if (shelfText != null)
        {
            shelfText.text = GetInteractionPrompt();
        }
    }

    // When nearby, show text
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (shelfText != null)
        {
            shelfText.gameObject.SetActive(true);
        }
    }

    // When leaving, hide text
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (shelfText != null)
        {
            shelfText.gameObject.SetActive(false);
        }
    }
}
