using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHandsUI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerHands playerHands;

    [Header("Slot UI Elements")]
    [SerializeField] private Image slot0Icon;
    [SerializeField] private Image slot1Icon;
    [SerializeField] private TextMeshProUGUI slot0Text;
    [SerializeField] private TextMeshProUGUI slot1Text;

    private void OnEnable()
    {
        if (playerHands != null)
        {
            playerHands.OnHandsChanged += RefreshUI;
        }
    }

    private void OnDisable()
    {
        if (playerHands != null)
        {
            playerHands.OnHandsChanged -= RefreshUI;
        }
    }

    private void Start()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        UpdateSlotGraphic(slot0Icon, slot0Text, playerHands.hands[0]);
        UpdateSlotGraphic(slot1Icon, slot1Text, playerHands.hands[1]);
    }

    private void UpdateSlotGraphic(Image iconImage, TextMeshProUGUI itemText, ItemSO item)
    {
        if (item != null && item.icon != null)
        {
            iconImage.sprite = item.icon;
            iconImage.color = item.iconColor;
            iconImage.enabled = true;
            itemText.text = $"{item.name}";
            itemText.gameObject.SetActive(true);
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
            itemText.gameObject.SetActive(false);
        }
    }
}