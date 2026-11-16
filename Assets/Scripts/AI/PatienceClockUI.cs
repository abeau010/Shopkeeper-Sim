using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Countdown at the top of the screen for the waiting customer who will leave soonest. Builds its own UI at runtime so no scene setup is needed.
public class PatienceClockUI : MonoBehaviour
{
    private const float WarningSeconds = 10f; // Clock turns red below this
    private const string NormalColor = "#FFFFFF";
    private const string WarningColor = "#E74C3C";

    private IReadOnlyList<Customer> customers;
    private TextMeshProUGUI clockText;

    // Called by CustomerSpawner with the list of customers it has spawned
    public void Init(IReadOnlyList<Customer> watched)
    {
        customers = watched;
        BuildUI();
    }

    private void LateUpdate()
    {
        if (clockText == null) return;

        Customer next = null;
        foreach (Customer customer in customers)
        {
            if (customer == null || !customer.CanServe) continue; // Still walking in, or already leaving
            if (next == null || customer.PatienceLeft < next.PatienceLeft) next = customer;
        }
        if (next == null)
        {
            clockText.text = "";
            return;
        }

        int seconds = Mathf.CeilToInt(next.PatienceLeft);
        string color = next.PatienceLeft <= WarningSeconds ? WarningColor : NormalColor;
        string paused = next.IsBeingServed ? " <size=70%>(paused)</size>" : "";
        clockText.text = $"<color={color}>{next.Data.name} leaves in {seconds / 60}:{seconds % 60:00}</color>{paused}";
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("Patience Clock Canvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40; // Below the price picker
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var go = new GameObject("Patience Clock", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(canvasGO.transform, false);
        clockText = go.GetComponent<TextMeshProUGUI>();
        clockText.fontSize = 40;
        clockText.fontStyle = FontStyles.Bold;
        clockText.alignment = TextAlignmentOptions.Top;
        clockText.color = Color.white;
        clockText.raycastTarget = false;
        var rect = clockText.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(-400, -80);
        rect.offsetMax = new Vector2(400, -20);
    }
}
