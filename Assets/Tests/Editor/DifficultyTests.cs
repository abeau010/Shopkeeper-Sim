using System;
using NUnit.Framework;

// Level settings: EASY = 3:00, over $50, normal pace, 1 customer at a time.
// HARD = 2:00, over $75, customers 3x faster, up to 3 at once.
// Run from Window > General > Test Runner > EditMode.
public class DifficultyTests
{
    // ---------- EASY ----------

    [Test]
    public void Easy_IsThreeMinutes() => Assert.AreEqual(180f, Difficulty.Easy.RoundSeconds);

    [Test]
    public void Easy_NeedsOver50() => Assert.AreEqual(50, Difficulty.Easy.WinMoney);

    [Test]
    public void Easy_LosesBelow20Reputation() => Assert.AreEqual(20f, Difficulty.Easy.LoseReputation);

    [Test]
    public void Easy_HasNormalSpawnRate() => Assert.AreEqual(1f, Difficulty.Easy.SpawnRateMultiplier);

    // ---------- HARD ----------

    [Test]
    public void Hard_IsTwoMinutes() => Assert.AreEqual(120f, Difficulty.Hard.RoundSeconds);

    [Test]
    public void Hard_IsShorterThanEasy() => Assert.Less(Difficulty.Hard.RoundSeconds, Difficulty.Easy.RoundSeconds);

    [Test]
    public void Hard_NeedsOver75() => Assert.AreEqual(75, Difficulty.Hard.WinMoney);

    [Test]
    public void Hard_LosesBelow20Reputation() => Assert.AreEqual(20f, Difficulty.Hard.LoseReputation);

    [Test]
    public void Hard_SpawnsThreeTimesAsFastAsEasy() =>
        Assert.AreEqual(3f, Difficulty.Hard.SpawnRateMultiplier / Difficulty.Easy.SpawnRateMultiplier);

    [Test]
    public void Hard_NeedsMoreMoneyThanEasy() =>
        Assert.Greater(Difficulty.Hard.WinMoney, Difficulty.Easy.WinMoney);

    // ---------- Level list ----------

    [Test]
    public void All_IsEasyThenHard() =>
        CollectionAssert.AreEqual(new[] { Difficulty.Easy, Difficulty.Hard }, Difficulty.All);

    [Test]
    public void Names_AreEasyAndHard()
    {
        Assert.AreEqual("EASY", Difficulty.Easy.Name);
        Assert.AreEqual("HARD", Difficulty.Hard.Name);
    }

    // ---------- Rules from a level ----------

    [Test]
    public void CreateRules_CopiesSettings()
    {
        RoundRules rules = Difficulty.Hard.CreateRules();
        Assert.AreEqual(120f, rules.RoundSeconds);
        Assert.AreEqual(75, rules.WinMoney);
        Assert.AreEqual(20f, rules.LoseReputation);
    }

    [Test]
    public void Easy_51DollarsWins() =>
        Assert.AreEqual(RoundState.Won, Difficulty.Easy.CreateRules().Evaluate(60f, 50f, 51, out _));

    [Test]
    public void Hard_51DollarsIsNotEnough() =>
        Assert.AreEqual(RoundState.Playing, Difficulty.Hard.CreateRules().Evaluate(60f, 50f, 51, out _));

    [Test]
    public void Hard_Exactly75IsNotEnough() =>
        Assert.AreEqual(RoundState.Playing, Difficulty.Hard.CreateRules().Evaluate(60f, 50f, 75, out _));

    [Test]
    public void Hard_76DollarsWins() =>
        Assert.AreEqual(RoundState.Won, Difficulty.Hard.CreateRules().Evaluate(60f, 50f, 76, out _));

    [Test]
    public void Hard_TimeOutAt75Loses() =>
        Assert.AreEqual(RoundState.Lost, Difficulty.Hard.CreateRules().Evaluate(0f, 50f, 75, out _));

    [Test]
    public void Hard_FullRoundWin()
    {
        var round = new Round(Difficulty.Hard.CreateRules(), reputation: 50f);
        round.Tick(50f); round.UpdateStats(45f, 40);
        round.Tick(50f); round.UpdateStats(40f, 80);
        Assert.AreEqual(RoundState.Won, round.State);
        Assert.AreEqual(20f, round.TimeLeft, 0.001f);
    }

    [Test]
    public void Hard_RunsOutAfterTwoMinutes()
    {
        var round = new Round(Difficulty.Hard.CreateRules(), reputation: 50f);
        round.Tick(119f);
        Assert.AreEqual(RoundState.Playing, round.State);
        round.Tick(1f);
        Assert.AreEqual(RoundState.Lost, round.State);
    }

    // ---------- Customers at once ----------

    [Test]
    public void Easy_OneCustomerAtATime() => Assert.AreEqual(1, Difficulty.Easy.MaxCustomers);

    [Test]
    public void Hard_LetsSeveralCustomersIn() => Assert.Greater(Difficulty.Hard.MaxCustomers, 1);

    [Test]
    public void Hard_UpToThreeCustomers() => Assert.AreEqual(3, Difficulty.Hard.MaxCustomers);

    // ---------- Bad settings ----------

    [TestCase(0f)]
    [TestCase(-1f)]
    public void SpawnRateMustBeAboveZero(float multiplier) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Difficulty("BAD", 180f, 50, 20f, multiplier, 1));

    [TestCase(0)]
    [TestCase(-1)]
    public void MaxCustomersMustBeAtLeastOne(int max) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Difficulty("BAD", 180f, 50, 20f, 1f, max));
}
