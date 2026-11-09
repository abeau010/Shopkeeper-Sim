using System.Collections.Generic;
using UnityEngine;

// Sells every item the shelves stock, the player carries it to its shelf and puts it back to restock
public class SupplyCrate : MonoBehaviour, IInteractable
{
    private const int VisibleRows = 6;

    private ListMenu menu;

    private void Awake()
    {
        menu = gameObject.AddComponent<ListMenu>();
    }

    // Wait for Start so the list can read the shelves
    private void Start()
    {
        menu.Init("Supply Crate", BuildRows, VisibleRows, false);
    }

    public void Interact(PlayerHands playerHands)
    {
        menu.Open(playerHands);
    }

    public string GetInteractionPrompt() => "Supply Crate";

    // Half the shelf price, rounded up, so selling at the shelf price makes a profit
    public static int GetCost(ItemSO item) => Mathf.CeilToInt(ShopManager.GetPrice(item) / 2f);

    private string Buy(ItemSO item, PlayerHands playerHands)
    {
        if (!playerHands.CanHold(item)) return item.isLarge ? $"{item.itemName} needs both hands" : "Your hands are full";
        if (ShopManager.Instance == null || !ShopManager.Instance.TrySpendMoney(GetCost(item))) return "Not enough money";
        playerHands.TryAddItem(item);
        return null;
    }

    // Reads the shelves left to right so new shelf items appear here automatically
    private List<ListMenuRow> BuildRows(PlayerHands playerHands)
    {
        Shelf[] shelves = FindObjectsByType<Shelf>();
        System.Array.Sort(shelves, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        var rows = new List<ListMenuRow>();
        var added = new HashSet<ItemSO>();
        foreach (Shelf shelf in shelves)
        {
            foreach (ShelfSlot slot in shelf.slots)
            {
                ItemSO item = slot.item;
                if (item == null || !added.Add(item)) continue;
                rows.Add(new ListMenuRow
                {
                    text = $"{item.itemName} ${GetCost(item)}",
                    confirm = () => Buy(item, playerHands),
                });
            }
        }
        return rows;
    }
}
