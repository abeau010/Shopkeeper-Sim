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
    public float Reputation => customersServed == 0 ? 50f : totalSatisfaction / (float)customersServed; // Average satisfaction of departed customers
    public event Action OnStatsChanged;

    private Shelf[] shelves;
    private int totalSatisfaction;
    private int customersServed;

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
            if (shelf.itemType == null) continue;
            string itemName = shelf.itemType.itemName;
            if (!items.TryGetValue(itemName, out ShopItemData data))
            {
                data = new ShopItemData { name = itemName, price = GetPrice(shelf.itemType) };
                items[itemName] = data;
            }
            data.stock += shelf.currentStock;
        }
        return new List<ShopItemData>(items.Values).ToArray();
    }

    public void AddMoney(int amount)
    {
        Money += amount;
        RefreshUI();
    }

    // Called when a customer leaves for any reason
    public void RecordCustomerExit(int satisfaction)
    {
        totalSatisfaction += satisfaction;
        customersServed++;
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
