using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// Main menu shown before each round: how to play, how to win and lose, and a button per level.
// Added by GameManager. Builds one button for every entry in Difficulty.All.
public class MainMenuUI : MonoBehaviour
{
    private static readonly Color[] ButtonColors =
    {
        new Color(0.18f, 0.55f, 0.34f), // First level - green
        new Color(0.70f, 0.20f, 0.20f), // Second level - red
        new Color(0.30f, 0.30f, 0.38f), // Any others - grey
    };

    private GameManager game;
    private GameObject screen;

    public void Init(GameManager gameManager)
    {
        game = gameManager;
        game.RoundStarted += Hide;
        BuildUI();
    }

    private void OnDestroy()
    {
        if (game != null) game.RoundStarted -= Hide;
    }

    public void Show() => screen.SetActive(true);
    private void Hide() => screen.SetActive(false);

    // Number keys pick a level too: 1 = first level, 2 = second, ...
    private void Update()
    {
        var kb = Keyboard.current;
        if (!screen.activeSelf || kb == null) return;
        for (int i = 0; i < Difficulty.All.Count && i < 9; i++)
        {
            if (kb[Key.Digit1 + i].wasPressedThisFrame || kb[Key.Numpad1 + i].wasPressedThisFrame)
            {
                game.StartRound(Difficulty.All[i]);
                return;
            }
        }
    }

    public static string RulesText()
    {
        Difficulty sample = Difficulty.Easy;
        return
            "<b>How to play</b>\n" +
            "• Customers walk in and ask for an item.\n" +
            "• Grab it from a shelf (<b>E</b>), walk up to the customer and press <b>E</b>.\n" +
            "• Pick a price and offer it. Too high and they'll haggle or walk away.\n" +
            "• Restock from the supply crate when shelves run low.\n" +
            "• Customers who wait too long or leave unhappy hurt your reputation.\n\n" +
            $"<color=#2ECC71><b>Win:</b></color> earn more than your level's target before the timer runs out.\n" +
            $"<color=#E74C3C><b>Lose:</b></color> reputation drops below {sample.LoseReputation:0}%, or time runs out first.";
    }

    public static string LevelSummary(Difficulty level, Difficulty baseline)
    {
        string pace = level.SpawnRateMultiplier == baseline.SpawnRateMultiplier
            ? "Normal customer pace"
            : $"Customers arrive {level.SpawnRateMultiplier / baseline.SpawnRateMultiplier:0.#}x faster";
        string crowd = level.MaxCustomers == 1 ? "One customer at a time" : $"Up to {level.MaxCustomers} customers at once";
        return $"{Round.FormatTime(level.RoundSeconds)} timer\n" +
               $"Earn over ${level.WinMoney}\n" +
               $"Keep reputation at {level.LoseReputation:0}%+\n" +
               $"{pace}\n" +
               crowd;
    }

    private void BuildUI()
    {
        Transform canvas = RuntimeUI.CreateCanvas("Main Menu Canvas", 110, transform); // Above everything, even the end screen

        screen = RuntimeUI.CreatePanel("Main Menu", canvas, new Color(0.03f, 0.03f, 0.06f, 0.92f));
        RuntimeUI.SetRect((RectTransform)screen.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        GameObject box = RuntimeUI.CreatePanel("Panel", screen.transform, new Color(0.08f, 0.08f, 0.12f, 0.98f));
        Vector2 center = new Vector2(0.5f, 0.5f);
        RuntimeUI.SetRect((RectTransform)box.transform, center, center, new Vector2(-600, -450), new Vector2(600, 450));

        Vector2 top = new Vector2(0, 1), bottomCenter = new Vector2(0.5f, 0);

        TextMeshProUGUI title = RuntimeUI.CreateText("Title", box.transform, 72, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        title.text = "Shopkeeper Sim";
        RuntimeUI.SetRect(title.rectTransform, top, Vector2.one, new Vector2(40, -130), new Vector2(-40, -30));

        TextMeshProUGUI rules = RuntimeUI.CreateText("Rules", box.transform, 30, TextAlignmentOptions.TopLeft);
        rules.text = RulesText();
        RuntimeUI.SetRect(rules.rectTransform, top, Vector2.one, new Vector2(70, -560), new Vector2(-70, -150));

        TextMeshProUGUI choose = RuntimeUI.CreateText("Choose", box.transform, 34, TextAlignmentOptions.Center);
        choose.fontStyle = FontStyles.Bold;
        choose.text = "Choose a level";
        RuntimeUI.SetRect(choose.rectTransform, top, Vector2.one, new Vector2(40, -620), new Vector2(-40, -570));

        // One button per level, side by side, with its settings underneath
        const float buttonWidth = 440f, gap = 60f;
        int count = Difficulty.All.Count;
        float left = -(count * buttonWidth + (count - 1) * gap) / 2f;
        for (int i = 0; i < count; i++)
        {
            Difficulty level = Difficulty.All[i];
            float x = left + i * (buttonWidth + gap);

            Button button = RuntimeUI.CreateButton($"{level.Name}  ({i + 1})", box.transform,
                ButtonColors[Mathf.Min(i, ButtonColors.Length - 1)], () => game.StartRound(level));
            RuntimeUI.SetRect((RectTransform)button.transform, bottomCenter, bottomCenter, new Vector2(x, 180), new Vector2(x + buttonWidth, 280));

            TextMeshProUGUI summary = RuntimeUI.CreateText($"{level.Name} Summary", box.transform, 22, TextAlignmentOptions.Top);
            summary.color = new Color(0.8f, 0.8f, 0.85f);
            summary.text = LevelSummary(level, Difficulty.Easy);
            RuntimeUI.SetRect(summary.rectTransform, bottomCenter, bottomCenter, new Vector2(x, 15), new Vector2(x + buttonWidth, 172));
        }

        screen.SetActive(false);
    }
}
