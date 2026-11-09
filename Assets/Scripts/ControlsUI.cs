using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Controls reminder in the bottom-right corner, with room above it for short messages. Builds its own UI at runtime so no scene setup is needed.
public class ControlsUI : MonoBehaviour
{
    private const float MessageSeconds = 3f;

    private const string Controls =
        "<b>Controls</b>\n" +
        "<b>WASD / Arrows</b>  Move\n" +
        "<b>E</b>  Interact / confirm\n" +
        "<b>Up / Down</b>  Pick in list\n" +
        "<b>Esc</b>  Cancel\n" +
        "<b>Q</b>  Return held item";

    private TextMeshProUGUI messageText;

    private void Awake()
    {
        BuildUI();
    }

    public void ShowMessage(string message)
    {
        StopAllCoroutines();
        StartCoroutine(ShowMessageRoutine(message));
    }

    private IEnumerator ShowMessageRoutine(string message)
    {
        messageText.text = message;
        yield return new WaitForSeconds(MessageSeconds);
        messageText.text = "";
    }

    private void BuildUI()
    {
        var canvasGO = new GameObject("Controls Canvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40; // Below the price picker
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = new GameObject("Controls Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGO.transform, false);
        panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.75f);
        panel.GetComponent<Image>().raycastTarget = false;
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(1, 0);
        panelRect.offsetMin = new Vector2(-350, 20);
        panelRect.offsetMax = new Vector2(-20, 230);

        var controls = CreateText("Controls", panel.transform, 24, TextAlignmentOptions.TopLeft);
        controls.text = Controls;
        var controlsRect = controls.rectTransform;
        controlsRect.anchorMin = Vector2.zero;
        controlsRect.anchorMax = Vector2.one;
        controlsRect.offsetMin = new Vector2(14, 8);
        controlsRect.offsetMax = new Vector2(-14, -8);

        messageText = CreateText("Message", canvasGO.transform, 28, TextAlignmentOptions.BottomRight);
        messageText.color = new Color(1f, 0.7f, 0.63f);
        var messageRect = messageText.rectTransform;
        messageRect.anchorMin = messageRect.anchorMax = new Vector2(1, 0);
        messageRect.offsetMin = new Vector2(-600, 240);
        messageRect.offsetMax = new Vector2(-20, 320);
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, float size, TextAlignmentOptions alignment)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
