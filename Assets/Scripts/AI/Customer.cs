using UnityEngine;
using TMPro;

// AI-controlled customer: walks in, asks for an item, reacts to the price offered, then walks out
public class Customer : MonoBehaviour, IInteractable
{
    private enum State { WalkingIn, Waiting, WalkingOut }

    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float patienceSeconds = 120f; // Leaves unhappy if not served this long
    [SerializeField] private TextMeshPro speechLabel;      // Optional - created automatically if empty

    public CustomerData Data { get; private set; }
    public bool IsLeaving => state == State.WalkingOut;
    public bool CanServe => state == State.Waiting;

    private State state;
    private Vector3 standPosition;
    private Vector3 exitPosition;
    private float waitTimer;
    private bool exitRecorded;

    // Called by CustomerSpawner right after the server creates this customer
    public void Init(CustomerData data, Vector3 standPos, Vector3 exitPos)
    {
        Data = data;
        standPosition = standPos;
        exitPosition = exitPos;
        state = State.WalkingIn;
        name = $"Customer - {data.name}";

        if (TryGetComponent(out SpriteRenderer sr) && ColorUtility.TryParseHtmlString(data.color, out Color c))
        {
            sr.color = c;
        }
        if (speechLabel == null) speechLabel = CreateLabel();
        UpdateLabel();
    }

    // Holding an item opens the price picker, empty hands just repeat the request
    public void Interact(PlayerHands playerHands)
    {
        if (!CanServe || PriceSelectUI.Instance == null) return;

        ItemSO offered = GetOfferedItem(playerHands);
        if (offered == null)
        {
            Say($"I'm looking for a {Data.wants}. Grab one off the shelf!");
            return;
        }
        PriceSelectUI.Instance.Open(this, playerHands, offered);
    }

    public string GetInteractionPrompt() => Data != null ? $"Sell to {Data.name}" : "";

    // Updates local copy of the customer after each server response
    public void UpdateData(CustomerData newData)
    {
        Data.satisfaction = newData.satisfaction;
        Data.status = newData.status;
        Data.offers_left = newData.offers_left;
        Say(newData.say);
    }

    public void Say(string line)
    {
        Data.say = line;
        UpdateLabel();
    }

    // Walk out of the shop and record how happy they were
    public void Leave()
    {
        if (state == State.WalkingOut) return;
        state = State.WalkingOut;
        if (!exitRecorded && ShopManager.Instance != null)
        {
            ShopManager.Instance.RecordCustomerExit(Data.satisfaction);
            exitRecorded = true;
        }
        UpdateLabel();
    }

    private void Update()
    {
        if (Data == null) return;

        switch (state)
        {
            case State.WalkingIn:
                if (MoveTowards(standPosition)) state = State.Waiting;
                break;
            case State.Waiting:
                // Patience only runs out while the player isn't picking a price for them
                if (PriceSelectUI.Instance != null && PriceSelectUI.Instance.CurrentCustomer == this) break;
                waitTimer += Time.deltaTime;
                if (waitTimer >= patienceSeconds)
                {
                    Data.satisfaction = Mathf.Max(0, Data.satisfaction - 30);
                    Say("Forget it, I've waited long enough!");
                    Leave();
                }
                break;
            case State.WalkingOut:
                if (MoveTowards(exitPosition)) Destroy(gameObject);
                break;
        }
    }

    // Prefers the item the customer asked for if the player is holding it
    private ItemSO GetOfferedItem(PlayerHands playerHands)
    {
        ItemSO fallback = null;
        foreach (ItemSO item in playerHands.hands)
        {
            if (item == null) continue;
            if (string.Equals(item.itemName, Data.wants, System.StringComparison.OrdinalIgnoreCase)) return item;
            if (fallback == null) fallback = item;
        }
        return fallback;
    }

    // Returns true once the target is reached
    private bool MoveTowards(Vector3 target)
    {
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        return Vector3.Distance(transform.position, target) < 0.01f;
    }

    // Tell the server to forget this customer once they're gone
    private void OnDestroy()
    {
        if (Data != null && AIServerClient.Instance != null)
        {
            AIServerClient.Instance.RemoveCustomer(Data.session_id);
        }
    }

    private void UpdateLabel()
    {
        if (speechLabel == null || Data == null) return;
        string header = IsLeaving ? $"<b>{Data.name}</b>" : $"<b>{Data.name}</b> <size=70%>(wants {Data.wants})</size>";
        speechLabel.text = string.IsNullOrEmpty(Data.say) ? header : $"<size=60%>\"{Data.say}\"</size>\n{header}";
    }

    // Speech text above the customer's head, grows upwards as lines get longer
    private TextMeshPro CreateLabel()
    {
        var go = new GameObject("Speech Label");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        var label = go.AddComponent<TextMeshPro>();
        label.fontSize = 3f;
        label.alignment = TextAlignmentOptions.Bottom;
        label.rectTransform.pivot = new Vector2(0.5f, 0f);
        label.rectTransform.sizeDelta = new Vector2(5f, 3f);
        label.sortingOrder = 100;
        return label;
    }
}
