using System;
using System.Collections.Generic;
using UnityEngine;

// One item type on a shelf, with its own stock
[Serializable]
public class ShelfSlot
{
    public ItemSO item;
    public int stock = 3;
    public int maxStock = 10;
}

// Holds a list of items, edit the slots in the Inspector to change what it sells
public class Shelf : MonoBehaviour, IInteractable
{
    public string shelfName = "Shelf";
    public List<ShelfSlot> slots = new List<ShelfSlot>();

    private ListMenu menu;

    // The list above the shelf builds itself at runtime so no scene setup is needed
    private void Awake()
    {
        menu = gameObject.AddComponent<ListMenu>();
        menu.Init(shelfName, BuildRows);
    }

    private void Start() 
    {
        if (menu != null)
        {
            menu.SetLabelVisible(false);
        }
    }

    // Opens the list so the player can pick an item
    public void Interact(PlayerHands playerHands)
    {
        menu.Open(playerHands);
    }

    public string GetInteractionPrompt() => shelfName;

    // Moves one item into the player's hands if it's in stock and fits
    public bool TryTake(ShelfSlot slot, PlayerHands playerHands)
    {
        if (slot.stock <= 0 || !playerHands.TryAddItem(slot.item)) return false;
        slot.stock--;
        menu.Refresh();
        return true;
    }

    // Moves one held item back onto the shelf if there's room
    public bool TryReturn(ShelfSlot slot, PlayerHands playerHands)
    {
        if (slot.stock >= slot.maxStock) return false;
        for (int i = 0; i < playerHands.hands.Length; i++)
        {
            if (playerHands.hands[i] == slot.item)
            {
                playerHands.RemoveItem(i);
                slot.stock++;
                menu.Refresh();
                return true;
            }
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<PlayerHands>() != null)
        {
            if (menu != null)
            {
                menu.SetLabelVisible(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<PlayerHands>() != null)
        {
            if (menu != null)
            {
                menu.Close();
                menu.SetLabelVisible(false);
            }
        }
    }

    // One row per item, then a "Put back" row for each held item this shelf stocks
    private List<ListMenuRow> BuildRows(PlayerHands playerHands)
    {
        var rows = new List<ListMenuRow>();
        foreach (ShelfSlot slot in slots)
        {
            if (slot.item == null) continue;
            rows.Add(new ListMenuRow
            {
                text = $"{slot.item.itemName} x{slot.stock}",
                canSelect = slot.stock > 0,
                confirm = () =>
                {
                    if (TryTake(slot, playerHands)) return null;
                    return slot.item.isLarge ? $"{slot.item.itemName} needs both hands" : "Your hands are full";
                },
            });
        }
        if (playerHands == null) return rows;

        foreach (ShelfSlot slot in slots)
        {
            if (slot.item == null || Array.IndexOf(playerHands.hands, slot.item) < 0) continue;
            rows.Add(new ListMenuRow
            {
                text = $"Put back {slot.item.itemName}",
                canSelect = slot.stock < slot.maxStock,
                confirm = () => TryReturn(slot, playerHands) ? null : "No room on the shelf",
            });
        }
        return rows;
    }
}
