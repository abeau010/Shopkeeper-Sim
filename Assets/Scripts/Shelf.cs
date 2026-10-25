using UnityEngine;

public class Shelf : MonoBehaviour, IInteractable
{
    public ItemSO itemType;
    public int currentStock = 5;
    public int maxStock = 10;

    // Try to take stock or restock
    public void Interact(PlayerHands playerHands)
    {
        if (playerHands.hands[0] == null || playerHands.hands[1] == null) // At least one hand empty
        {
            if (currentStock > 0 && playerHands.TryAddItem(itemType)) // Item(s) on the shelf and item fits in hands
            {
                currentStock--;
                // UpdateVisuals();
                return;
            }
        }

        for (int i = 0; i < playerHands.hands.Length; i++)
        {
            if (playerHands.hands[i] == itemType && currentStock < maxStock)
            {
                playerHands.RemoveItem(i);
                currentStock++;
                // UpdateVisuals;
            }
        }
    }

    public string GetInteractionPrompt() => $"{itemType.itemName} ({currentStock/maxStock})";
}