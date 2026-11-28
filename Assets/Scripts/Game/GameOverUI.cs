using UnityEngine;
using UnityEngine.UI;
using TMPro;

// "You Win" / "You Lose" screen with Play Again (same level) and Main Menu buttons. Added by GameManager, shown when the round ends.
public class GameOverUI : MonoBehaviour
{
    private GameManager game;
    private GameObject screen;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI detailText;

    public void Init(GameManager gameManager)
    {
        game = gameManager;
        game.GameEnded += Show;
        BuildUI();
    }

    private void OnDestroy()
    {
        if (game != null) game.GameEnded -= Show;
    }

    private void Show(Round round)
    {
        titleText.text = round.State == RoundState.Won
            ? "<color=#2ECC71>You Win!</color>"
            : "<color=#E74C3C>You Lose</color>";
        string level = game.Difficulty != null ? game.Difficulty.Name : "";
        detailText.text = $"{round.EndReason}\n\n" +
                          $"Level: {level}\n" +
                          $"Earned: ${round.TotalEarned} (need over ${round.Rules.WinMoney})\n" +
                          $"Reputation: {round.Reputation:0}% (need {round.Rules.LoseReputation:0}%+)\n" +
                          $"Time left: {Round.FormatTime(round.TimeLeft)}";
        screen.SetActive(true);
    }

    private void BuildUI()
    {
        Transform canvas = RuntimeUI.CreateCanvas("Game Over Canvas", 100, transform); // Above every other UI

        // Dark full-screen backdrop - also blocks clicks on the UI underneath
        screen = RuntimeUI.CreatePanel("Game Over Screen", canvas, new Color(0f, 0f, 0f, 0.7f));
        RuntimeUI.SetRect((RectTransform)screen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject box = RuntimeUI.CreatePanel("Panel", screen.transform, new Color(0.08f, 0.08f, 0.12f, 0.95f));
        Vector2 center = new Vector2(0.5f, 0.5f);
        RuntimeUI.SetRect((RectTransform)box.transform, center, center, new Vector2(-350, -250), new Vector2(350, 250));

        Vector2 top = new Vector2(0, 1);
        titleText = RuntimeUI.CreateText("Title", box.transform, 80, TextAlignmentOptions.Center);
        titleText.fontStyle = FontStyles.Bold;
        RuntimeUI.SetRect(titleText.rectTransform, top, Vector2.one, new Vector2(20, -140), new Vector2(-20, -30));

        detailText = RuntimeUI.CreateText("Details", box.transform, 28, TextAlignmentOptions.Top);
        RuntimeUI.SetRect(detailText.rectTransform, Vector2.zero, Vector2.one, new Vector2(30, 115), new Vector2(-30, -145));

        Vector2 bottomCenter = new Vector2(0.5f, 0);
        Button playAgain = RuntimeUI.CreateButton("Play Again", box.transform, new Color(0.18f, 0.55f, 0.34f), () => game.Restart());
        RuntimeUI.SetRect((RectTransform)playAgain.transform, bottomCenter, bottomCenter, new Vector2(-250, 30), new Vector2(-10, 100));
        Button mainMenu = RuntimeUI.CreateButton("Main Menu", box.transform, new Color(0.3f, 0.3f, 0.38f), () => game.ReturnToMenu());
        RuntimeUI.SetRect((RectTransform)mainMenu.transform, bottomCenter, bottomCenter, new Vector2(10, 30), new Vector2(250, 100));

        screen.SetActive(false);
    }
}
