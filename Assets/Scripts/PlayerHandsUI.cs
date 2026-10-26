using UnityEngine;
using UnityEngine.UI;

public class PlayerHandsUI : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerHands playerHands;

    [Header("Slot UI Elements")]
    [SerializeField] private Image slot0Icon;
    [SerializeField] private Image slot1Icon;

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
        UpdateSlotGraphic(slot0Icon, playerHands.hands[0]);
        UpdateSlotGraphic(slot1Icon, playerHands.hands[1]);
    }

    private void UpdateSlotGraphic(Image iconImage, ItemSO item)
    {
        if (item != null && item.icon != null)
        {
            iconImage.sprite = item.icon;
            iconImage.color = item.iconColor;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }
}