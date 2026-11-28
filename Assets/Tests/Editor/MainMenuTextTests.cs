using NUnit.Framework;

// The text on the main menu matches the real level settings.
// Run from Window > General > Test Runner > EditMode.
public class MainMenuTextTests
{
    [Test]
    public void Rules_ExplainWinAndLose()
    {
        string rules = MainMenuUI.RulesText();
        StringAssert.Contains("Win:", rules);
        StringAssert.Contains("Lose:", rules);
        StringAssert.Contains("20%", rules);
        StringAssert.Contains("time runs out", rules);
    }

    [Test]
    public void Rules_ExplainHowToSell()
    {
        string rules = MainMenuUI.RulesText();
        StringAssert.Contains("shelf", rules);
        StringAssert.Contains("price", rules);
    }

    [Test]
    public void EasySummary_ShowsSettings()
    {
        string summary = MainMenuUI.LevelSummary(Difficulty.Easy, Difficulty.Easy);
        StringAssert.Contains("3:00", summary);
        StringAssert.Contains("$50", summary);
        StringAssert.Contains("Normal customer pace", summary);
        StringAssert.Contains("One customer at a time", summary);
    }

    [Test]
    public void HardSummary_ShowsSettings()
    {
        string summary = MainMenuUI.LevelSummary(Difficulty.Hard, Difficulty.Easy);
        StringAssert.Contains("2:00", summary);
        StringAssert.Contains("$75", summary);
        StringAssert.Contains("3x faster", summary);
        StringAssert.Contains("Up to 3 customers at once", summary);
    }
}
