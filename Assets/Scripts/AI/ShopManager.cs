using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Tracks money and customer reputation, and tells the AI what the shop sells
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [SerializeField] private int startingMoney = 0;
    [SerializeField] private TextMeshProUGUI statsText; // Optional - shows money/reputation

    public int Money { get; private set; }
    public float Reputation { get; private set; } = 50f; // Moves with how much each customer's mood changed during their visit
    public event Action OnStatsChanged;

    private Shelf[] shelves;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        Money = startingMoney;
    }

    // Find all shelves in the scene and update stats text on startup
    private void Start()
    {
        shelves = FindObjectsByType<Shelf>();
        RefreshUI();
    }

    // Uses the current price if set, otherwise the base price
    public static int GetPrice(ItemSO item) => item.currentPrice > 0 ? item.currentPrice : item.basePrice;

    // Items on the shelves, with prices and stock, in the format the AI server expects
    public ShopItemData[] GetShopItems()
    {
        var items = new Dictionary<string, ShopItemData>();
        foreach (Shelf shelf in shelves)
        {
            foreach (ShelfSlot slot in shelf.slots)
            {
                if (slot.item == null) continue;
                string itemName = slot.item.itemName;
                if (!items.TryGetValue(itemName, out ShopItemData data))
                {
                    data = new ShopItemData { name = itemName, price = GetPrice(slot.item) };
                    items[itemName] = data;
                }
                data.stock += slot.stock;
            }
        }
        return new List<ShopItemData>(items.Values).ToArray();
    }

    public void AddMoney(int amount)
    {
        Money += amount;
        RefreshUI();
    }

    // Returns false and spends nothing if the player can't afford it
    public bool TrySpendMoney(int amount)
    {
        if (Money < amount) return false;
        Money -= amount;
        RefreshUI();
        return true;
    }

    // Called when a customer leaves for any reason
    public void RecordCustomerExit(int moodChange)
    {
        Reputation = Mathf.Clamp(Reputation + moodChange, 0f, 100f);
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (statsText != null)
        {
            statsText.text = $"${Money}   Reputation: {Reputation:0}%";
        }
        OnStatsChanged?.Invoke();
    }
}
