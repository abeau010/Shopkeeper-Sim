using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

// Price picker shown when the player hands a customer an item. Builds its own UI at runtime so no scene setup is needed.
public class PriceSelectUI : MonoBehaviour
{
    public static PriceSelectUI Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.CurrentCustomer != null; // Used to lock player movement/interaction

    [SerializeField] private bool showStatsHud = true;     // Money/reputation in the top-left corner
    [SerializeField] private float closeDelay = 2.5f;      // How long the reaction stays up after a sale/exit
    [SerializeField] private int maxPrice = 999;

    public Customer CurrentCustomer { get; private set; }

    private PlayerHands playerHands;
    private ItemSO offeredItem;
    private int price;
    private bool waitingForReply;
    private int openedFrame;

    private GameObject panel;
    private TextMeshProUGUI headerText;
    private TextMeshProUGUI infoText;
    private TextMeshProUGUI priceText;
    private TextMeshProUGUI reactionText;
    private TextMeshProUGUI hintText;
    private TextMeshProUGUI statsText;
    private Button[] buttons;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        BuildUI();
    }

    private void Start()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnStatsChanged += RefreshStats;
        }
        RefreshStats();
    }

    private void OnDestroy()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.OnStatsChanged -= RefreshStats;
        }
    }

    private void Update()
    {
        if (CurrentCustomer == null)
        {
            if (panel.activeSelf) Close(); // Customer was destroyed while open
            return;
        }

        var kb = Keyboard.current;
        if (kb == null || Time.frameCount == openedFrame) return; // Skip the frame E was pressed to open
        if (kb.escapeKey.wasPressedThisFrame) { Close(); return; }
        if (waitingForReply) return;

        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) ChangePrice(-1);
        if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) ChangePrice(1);
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) ChangePrice(-5);
        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) ChangePrice(5);
        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Offer();
        if (kb.bKey.wasPressedThisFrame) Ban();
    }

    public void Open(Customer customer, PlayerHands hands, ItemSO item)
    {
        if (CurrentCustomer != null) return;
        CurrentCustomer = customer;
        playerHands = hands;
        offeredItem = item;
        price = Mathf.Max(1, ShopManager.GetPrice(item));
        openedFrame = Time.frameCount;

        panel.SetActive(true);
        headerText.text = $"Selling <b>{item.itemName}</b> to <b>{customer.Data.name}</b>";
        reactionText.text = $"<b>{customer.Data.name}:</b> \"{customer.Data.say}\"";
        hintText.text = "Arrows/WASD: change price   Enter: offer   B: ban   Esc: cancel";
        RefreshInfo();
        RefreshPrice();
        SetButtonsEnabled(true);
    }

    public void Close()
    {
        StopAllCoroutines();
        CurrentCustomer = null;
        waitingForReply = false;
        panel.SetActive(false);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    private void ChangePrice(int amount)
    {
        if (waitingForReply) return;
        price = Mathf.Clamp(price + amount, 1, maxPrice);
        RefreshPrice();
    }

    private void Offer()
    {
        if (waitingForReply || CurrentCustomer == null) return;

        Customer customer = CurrentCustomer;
        PlayerHands hands = playerHands;
        ItemSO item = offeredItem;
        int offeredPrice = price;
        reactionText.text = $"<b>{customer.Data.name}:</b> <i>thinking...</i>";
        SetButtonsEnabled(false);

        ShopItemData[] items = ShopManager.Instance != null ? ShopManager.Instance.GetShopItems() : null;
        AIServerClient.Instance.MakeOffer(customer.Data.session_id, item.itemName, offeredPrice, items,
            reply =>
            {
                ApplyReply(customer, hands, item, reply);
                if (CurrentCustomer != customer) return; // Closed while waiting

                reactionText.text = $"<b>{customer.Data.name}:</b> \"{reply.say}\"";
                hintText.text = GetHint(reply, item);
                RefreshInfo();
                if (reply.outcome == "counter")
                {
                    price = reply.counter_offer; // Press Enter to accept their counter
                    RefreshPrice();
                }
                if (reply.leaving || reply.outcome == "wrong_item") StartCoroutine(CloseAfterDelay());
                else SetButtonsEnabled(true);
            },
            error =>
            {
                Debug.LogWarning($"[AI] {error}");
                if (CurrentCustomer != customer) return;
                reactionText.text = "<i>(Couldn't reach the AI server. Is it running?)</i>";
                SetButtonsEnabled(true);
            });
    }

    // Sale/satisfaction results apply even if the panel was closed mid-request
    private void ApplyReply(Customer customer, PlayerHands hands, ItemSO item, OfferResponseData reply)
    {
        if (reply.sale)
        {
            for (int i = 0; i < hands.hands.Length; i++)
            {
                if (hands.hands[i] == item)
                {
                    hands.RemoveItem(i);
                    break;
                }
            }
            if (ShopManager.Instance != null) ShopManager.Instance.AddMoney(reply.sale_price);
        }

        if (customer == null) return;
        customer.UpdateData(reply);
        if (reply.leaving) customer.Leave();
    }

    private void Ban()
    {
        if (waitingForReply || CurrentCustomer == null) return;
        Customer customer = CurrentCustomer;
        SetButtonsEnabled(false);

        AIServerClient.Instance.BanCustomer(customer.Data.session_id,
            data =>
            {
                if (customer == null) return;
                customer.UpdateData(data);
                customer.Leave();
                if (CurrentCustomer != customer) return;
                reactionText.text = $"<b>{customer.Data.name}:</b> \"{data.say}\"";
                hintText.text = $"<color=#E74C3C>You banned {customer.Data.name}.</color>";
                RefreshInfo();
                StartCoroutine(CloseAfterDelay());
            },
            error =>
            {
                Debug.LogWarning($"[AI] {error}");
                if (CurrentCustomer == customer) SetButtonsEnabled(true);
            });
    }

    private static string GetHint(OfferResponseData reply, ItemSO item)
    {
        string warning = reply.ai_online ? "" : "  <color=#E74C3C>(Ollama offline - using backup lines)</color>";
        switch (reply.outcome)
        {
            case "accept": return $"<color=#2ECC71>SOLD {reply.sale_item} for ${reply.sale_price}!</color>{warning}";
            case "wrong_item": return $"<color=#F1C40F>{reply.name} wants a {reply.wants}, not a {item.itemName}.</color>{warning}";
        }
        if (reply.leaving) return $"<color=#E74C3C>{reply.name} gave up and is leaving.</color>{warning}";
        if (reply.outcome == "counter") return $"Counter-offer: ${reply.counter_offer}. Press Enter to accept or pick another price.{warning}";
        return $"Too expensive. Try a lower price.{warning}";
    }

    private void RefreshInfo()
    {
        CustomerData data = CurrentCustomer.Data;
        string moodColor = data.satisfaction >= 60 ? "#2ECC71" : data.satisfaction >= 30 ? "#F1C40F" : "#E74C3C";
        infoText.text = $"Shelf price: ${ShopManager.GetPrice(offeredItem)}   Mood: <color={moodColor}>{data.satisfaction}/100</color>   Offers left: {data.offers_left}";
    }

    private void RefreshPrice() => priceText.text = $"${price}";

    private void RefreshStats()
    {
        if (statsText == null || ShopManager.Instance == null) return;
        statsText.text = $"${ShopManager.Instance.Money}   Reputation: {ShopManager.Instance.Reputation:0}%";
    }

    private void SetButtonsEnabled(bool enabled)
    {
        waitingForReply = !enabled;
        foreach (Button b in buttons) b.interactable = enabled;
    }

    private IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSeconds(closeDelay);
        Close();
    }

    // ---------- Runtime UI construction ----------

    private void BuildUI()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        var canvasGO = new GameObject("Price Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        if (showStatsHud)
        {
            statsText = CreateText("Stats", canvasGO.transform, 32, TextAlignmentOptions.TopLeft);
            SetRect(statsText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -80), new Vector2(700, -20));
        }

        panel = new GameObject("Price Panel", typeof(RectTransform), typeof(Image));
        panel.SetActive(false);
        panel.transform.SetParent(canvasGO.transform, false);
        panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.92f);
        SetRect(panel.GetComponent<RectTransform>(), new Vector2(0.2f, 0), new Vector2(0.8f, 0), new Vector2(0, 20), new Vector2(0, 420));

        Vector2 top = new Vector2(0, 1), topRight = new Vector2(1, 1), topCenter = new Vector2(0.5f, 1);

        headerText = CreateText("Header", panel.transform, 32, TextAlignmentOptions.TopLeft);
        SetRect(headerText.rectTransform, top, topRight, new Vector2(24, -60), new Vector2(-24, -14));

        infoText = CreateText("Info", panel.transform, 24, TextAlignmentOptions.TopLeft);
        SetRect(infoText.rectTransform, top, topRight, new Vector2(24, -100), new Vector2(-24, -64));

        // Price row: [-5] [-1]  $N  [+1] [+5]
        priceText = CreateText("Price", panel.transform, 64, TextAlignmentOptions.Center);
        SetRect(priceText.rectTransform, topCenter, topCenter, new Vector2(-100, -200), new Vector2(100, -110));
        var minus5 = CreateButton("-5", panel.transform, new Color(0.3f, 0.3f, 0.38f), () => ChangePrice(-5));
        SetRect((RectTransform)minus5.transform, topCenter, topCenter, new Vector2(-300, -190), new Vector2(-210, -120));
        var minus1 = CreateButton("-1", panel.transform, new Color(0.3f, 0.3f, 0.38f), () => ChangePrice(-1));
        SetRect((RectTransform)minus1.transform, topCenter, topCenter, new Vector2(-200, -190), new Vector2(-110, -120));
        var plus1 = CreateButton("+1", panel.transform, new Color(0.3f, 0.3f, 0.38f), () => ChangePrice(1));
        SetRect((RectTransform)plus1.transform, topCenter, topCenter, new Vector2(110, -190), new Vector2(200, -120));
        var plus5 = CreateButton("+5", panel.transform, new Color(0.3f, 0.3f, 0.38f), () => ChangePrice(5));
        SetRect((RectTransform)plus5.transform, topCenter, topCenter, new Vector2(210, -190), new Vector2(300, -120));

        reactionText = CreateText("Reaction", panel.transform, 26, TextAlignmentOptions.TopLeft);
        SetRect(reactionText.rectTransform, Vector2.zero, Vector2.one, new Vector2(24, 96), new Vector2(-24, -215));

        hintText = CreateText("Hint", panel.transform, 22, TextAlignmentOptions.MidlineLeft);
        hintText.color = new Color(0.75f, 0.75f, 0.8f);
        SetRect(hintText.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(24, 20), new Vector2(-330, 80));

        var offerButton = CreateButton("Offer", panel.transform, new Color(0.18f, 0.55f, 0.34f), Offer);
        SetRect((RectTransform)offerButton.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-316, 20), new Vector2(-174, 80));
        var banButton = CreateButton("Ban", panel.transform, new Color(0.7f, 0.2f, 0.2f), Ban);
        SetRect((RectTransform)banButton.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-166, 20), new Vector2(-24, 80));

        buttons = new[] { minus5, minus1, plus1, plus5, offerButton, banButton };
    }

    private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, float size, TextAlignmentOptions alignment)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string label, Transform parent, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        var button = go.GetComponent<Button>();
        button.onClick.AddListener(onClick);
        // Deselect after clicking so Enter/Space don't also "press" the last clicked button
        button.onClick.AddListener(() =>
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        });

        var text = CreateText("Label", go.transform, 28, TextAlignmentOptions.Center);
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        text.text = label;
        return button;
    }
}
