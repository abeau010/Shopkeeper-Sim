using NUnit.Framework;

// The chosen level and the "skip menu on Play Again" flag, which both survive a scene reload.
// Run from Window > General > Test Runner > EditMode.
public class GameSessionTests
{
    [SetUp]
    public void SetUp() => GameSession.Reset();

    [TearDown]
    public void TearDown() => GameSession.Reset();

    [Test]
    public void DefaultsToEasy() => Assert.AreSame(Difficulty.Easy, GameSession.SelectedDifficulty);

    [Test]
    public void DefaultsToShowingMenu() => Assert.IsFalse(GameSession.ConsumeSkipMenu());

    [Test]
    public void RemembersSelectedLevel()
    {
        GameSession.SelectedDifficulty = Difficulty.Hard;
        Assert.AreSame(Difficulty.Hard, GameSession.SelectedDifficulty);
    }

    [Test]
    public void SkipMenu_OnlyAppliesOnce()
    {
        GameSession.SkipMenuOnNextLoad = true;
        Assert.IsTrue(GameSession.ConsumeSkipMenu());  // Play Again skips the menu
        Assert.IsFalse(GameSession.ConsumeSkipMenu()); // The next load shows it again
    }

    [Test]
    public void Reset_GoesBackToDefaults()
    {
        GameSession.SelectedDifficulty = Difficulty.Hard;
        GameSession.SkipMenuOnNextLoad = true;
        GameSession.Reset();
        Assert.AreSame(Difficulty.Easy, GameSession.SelectedDifficulty);
        Assert.IsFalse(GameSession.SkipMenuOnNextLoad);
    }
}
