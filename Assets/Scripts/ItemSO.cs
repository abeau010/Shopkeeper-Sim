using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Shop/Item")]
public class ItemSO : ScriptableObject
{
    public string itemID;
    public string itemName;
    public Sprite icon;
    public bool isLarge;
    public int basePrice;
    public int currentPrice;
}
