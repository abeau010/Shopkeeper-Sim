using UnityEngine;
using TMPro;

// Round countdown in the top-right corner. Added by GameManager.
public class RoundTimerUI : MonoBehaviour
{
    private const float WarningSeconds = 30f; // Turns red below this
    private const string NormalColor = "#FFFFFF";
    private const string WarningColor = "#E74C3C";

    private GameManager game;
    private TextMeshProUGUI timerText;

    public void Init(GameManager gameManager)
    {
        game = gameManager;
        Transform canvas = RuntimeUI.CreateCanvas("Round Timer Canvas", 45, transform);
        timerText = RuntimeUI.CreateText("Round Timer", canvas, 40, TextAlignmentOptions.TopRight);
        timerText.fontStyle = FontStyles.Bold;
        RuntimeUI.SetRect(timerText.rectTransform, Vector2.one, Vector2.one, new Vector2(-420, -80), new Vector2(-30, -20));
    }

    private void LateUpdate()
    {
        if (game == null || game.Round == null || timerText == null) return;
        float timeLeft = game.Round.TimeLeft;
        string color = timeLeft <= WarningSeconds ? WarningColor : NormalColor;
        timerText.text = $"<color={color}>Time: {Round.FormatTime(timeLeft)}</color>";
    }
}
