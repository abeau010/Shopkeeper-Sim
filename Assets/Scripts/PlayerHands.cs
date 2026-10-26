using UnityEngine;
using System;

public class PlayerHands : MonoBehaviour
{
    // Player's hand slots
    public ItemSO[] hands = new ItemSO[2];
    public event Action OnHandsChanged;

    // Function that checks if the player can hold the object
    public bool CanHold(ItemSO item)
    {
        // Returns False if either hands has an item
        if (item.isLarge) return hands[0] == null && hands[1] == null;
        // Returns True if either hands is empty
        return hands[0] == null || hands[1] == null;
    }

    // Function that places the item in the player's hand(s) if possible
    public bool TryAddItem(ItemSO item)
    {
        // Can't hold the item
        if (!CanHold(item)) return false;
        
        if (item.isLarge) // Takes both slots
        {
            hands[0] = hands[1] = item;
        }
        else if (hands[0] == null) // First slot is empty
        {
            hands[0] = item;
        }
        else // First slot is full
        {
            hands[1] = item;
        }

        UpdateHandUI();
        return true;
    }

    // Function that removes an item from the player's hand(s) and returns the item
    public ItemSO RemoveItem(int hand)
    {
        ItemSO item = hands[hand];
        if (item == null) return null; // No item

        if (item.isLarge) // Large item - remove from both hands
        {
            hands[0] = hands[1] = null;
        }
        else // Remove from given hand
        {
            hands[hand] = null;
        }

        UpdateHandUI();
        return item;
    }

    public void UpdateHandUI()
    {
        /* string h0 = hands[0] != null ? hands[0].itemName : "Empty";
        string h1 = hands[1] != null ? hands[1].itemName : "Empty";
        Debug.Log($"[HANDS] Slot 0: {h0} | Slot 1: {h1}"); */
        // ^^ Use when debugging
        OnHandsChanged?.Invoke();
    }
}