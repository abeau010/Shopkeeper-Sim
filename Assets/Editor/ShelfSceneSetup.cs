using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Creates the items and lays out the 5 shelves in StoreOne: Shopkeeper > Set Up Shelves
public static class ShelfSceneSetup
{
    private const string ItemFolder = "Assets/Objects";

    private class ItemDef
    {
        public string name;
        public int price;
        public Color color;
        public bool isLarge;

        public ItemDef(string name, int price, string hexColor, bool isLarge = false)
        {
            this.name = name;
            this.price = price;
            ColorUtility.TryParseHtmlString(hexColor, out color);
            this.isLarge = isLarge;
        }
    }

    private class ShelfDef
    {
        public string name;
        public string hexColor;
        public ItemDef[] items;
    }

    private static readonly ShelfDef[] Shelves =
    {
        new ShelfDef { name = "Fruit", hexColor = "#4169E1", items = new[]
        {
            new ItemDef("Apple", 4, "#E53935"), new ItemDef("Orange", 3, "#FB8C00"), new ItemDef("Banana", 2, "#FDD835"),
        }},
        new ShelfDef { name = "Drinks", hexColor = "#00897B", items = new[]
        {
            new ItemDef("Water Bottle", 2, "#4FC3F7"), new ItemDef("Soda Can", 3, "#D81B60"), new ItemDef("Juice Box", 3, "#7CB342"),
        }},
        new ShelfDef { name = "Snacks", hexColor = "#8E24AA", items = new[]
        {
            new ItemDef("Chip Bag", 3, "#FFB300"), new ItemDef("Cookie", 2, "#A1887F"), new ItemDef("Candy Bar", 2, "#6D4C41"),
        }},
        new ShelfDef { name = "Household", hexColor = "#546E7A", items = new[]
        {
            new ItemDef("Soap Bar", 4, "#80DEEA"), new ItemDef("Candle", 6, "#FFF9C4"), new ItemDef("Towel", 8, "#F48FB1"),
        }},
        new ShelfDef { name = "Large Goods", hexColor = "#FFEB04", items = new[]
        {
            new ItemDef("Box", 15, "#BCAAA4", true), new ItemDef("Crate", 20, "#8D6E63", true), new ItemDef("Barrel", 30, "#5D4037", true),
        }},
    };

    [MenuItem("Shopkeeper/Set Up Shelves")]
    public static void SetUp()
    {
        if (EditorSceneManager.GetActiveScene().name != "StoreOne")
        {
            Debug.LogError("[Shelves] Open the StoreOne scene first.");
            return;
        }

        Shelf[] existing = UnityEngine.Object.FindObjectsByType<Shelf>();
        if (existing.Length == 0)
        {
            Debug.LogError("[Shelves] No shelf in the scene to copy.");
            return;
        }
        // Don't overwrite lists that have already been set up or edited
        foreach (Shelf shelf in existing)
        {
            if (shelf.slots.Count > 0)
            {
                Debug.LogWarning("[Shelves] Shelves are already set up, nothing changed.");
                return;
            }
        }

        var orange = AssetDatabase.LoadAssetAtPath<ItemSO>($"{ItemFolder}/Orange.asset");
        if (orange == null)
        {
            Debug.LogError($"[Shelves] Couldn't find {ItemFolder}/Orange.asset, its icon is used for the new items.");
            return;
        }

        Array.Sort(existing, (a, b) => string.CompareOrdinal(a.name, b.name));
        foreach (Shelf shelf in existing) RemoveOldShelfText(shelf);

        // Evenly spaced along the top of the room, the lists fit in the space above
        for (int i = 0; i < Shelves.Length; i++)
        {
            Shelf shelf = i < existing.Length ? existing[i] : CopyShelf(existing[0]);
            Vector3 position = new Vector3(-6f + i * 3f, 1.5f, 0f);
            SetUpShelf(shelf, Shelves[i], position, orange.icon);
        }

        MoveCustomersBelowShelves();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Shelves] 5 shelves set up. Save the scene to keep them.");
    }

    // Copies a shelf's sprite and colliders into a crate in the bottom-left corner
    [MenuItem("Shopkeeper/Add Supply Crate")]
    public static void AddSupplyCrate()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Shelves] Stop the game first, changes made during Play are lost.");
            return;
        }
        if (UnityEngine.Object.FindAnyObjectByType<SupplyCrate>() != null)
        {
            Debug.LogWarning("[Shelves] The scene already has a supply crate, nothing changed.");
            return;
        }
        Shelf template = UnityEngine.Object.FindAnyObjectByType<Shelf>();
        if (template == null)
        {
            Debug.LogError("[Shelves] No shelf in the scene to copy. Run Set Up Shelves first.");
            return;
        }

        Shelf copy = UnityEngine.Object.Instantiate(template);
        GameObject crate = copy.gameObject;
        UnityEngine.Object.DestroyImmediate(copy);
        crate.name = "Supply Crate";
        crate.transform.position = new Vector3(-7.2f, -3.5f, 0f);
        crate.AddComponent<SupplyCrate>();
        if (crate.TryGetComponent(out SpriteRenderer sr) && ColorUtility.TryParseHtmlString("#D7A86E", out Color color))
        {
            sr.color = color;
        }
        Undo.RegisterCreatedObjectUndo(crate, "Add Supply Crate");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Shelves] Supply crate added. Save the scene to keep it.");
    }

    private static Shelf CopyShelf(Shelf template)
    {
        Shelf copy = UnityEngine.Object.Instantiate(template);
        Undo.RegisterCreatedObjectUndo(copy.gameObject, "Add Shelf");
        return copy;
    }

    // The list above the shelf replaces the old text canvas
    private static void RemoveOldShelfText(Shelf shelf)
    {
        for (int i = shelf.transform.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(shelf.transform.GetChild(i).gameObject);
        }
    }

    private static void SetUpShelf(Shelf shelf, ShelfDef def, Vector3 position, Sprite placeholderIcon)
    {
        Undo.RecordObject(shelf, "Set Up Shelf");
        Undo.RecordObject(shelf.gameObject, "Set Up Shelf");
        Undo.RecordObject(shelf.transform, "Set Up Shelf");
        shelf.gameObject.name = $"Shelf - {def.name}";
        shelf.transform.position = position;
        shelf.shelfName = def.name;
        shelf.slots.Clear();
        foreach (ItemDef itemDef in def.items)
        {
            shelf.slots.Add(new ShelfSlot { item = GetOrCreateItem(itemDef, placeholderIcon) });
        }

        if (shelf.TryGetComponent(out SpriteRenderer sr) && ColorUtility.TryParseHtmlString(def.hexColor, out Color color))
        {
            Undo.RecordObject(sr, "Set Up Shelf");
            sr.color = color;
        }
    }

    // Existing items keep their settings and icon, but need a visible colour for the hand UI
    private static ItemSO GetOrCreateItem(ItemDef def, Sprite placeholderIcon)
    {
        string path = $"{ItemFolder}/{def.name}.asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
        if (item != null)
        {
            if (item.iconColor.a == 0f)
            {
                item.iconColor = def.color;
                EditorUtility.SetDirty(item);
            }
            return item;
        }

        item = ScriptableObject.CreateInstance<ItemSO>();
        item.itemID = def.name.Replace(" ", "").ToLowerInvariant();
        item.itemName = def.name;
        item.icon = placeholderIcon;
        item.iconColor = def.color;
        item.isLarge = def.isLarge;
        item.basePrice = def.price;
        item.currentPrice = def.price;
        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    private static void MoveCustomersBelowShelves()
    {
        var spawner = UnityEngine.Object.FindAnyObjectByType<CustomerSpawner>();
        if (spawner == null) return;
        var so = new SerializedObject(spawner);
        so.FindProperty("standAreaCenter").vector2Value = new Vector2(0f, 3.5f);
        so.FindProperty("standAreaSize").vector2Value = new Vector2(8f, 2f);
        so.ApplyModifiedProperties();
    }
}
