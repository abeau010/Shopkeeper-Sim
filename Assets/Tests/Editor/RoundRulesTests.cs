using NUnit.Framework;

// Win/lose rules: 3 minute round, win by earning OVER $50, lose if reputation drops BELOW 20% or time runs out.
// Run from Window > General > Test Runner > EditMode.
public class RoundRulesTests
{
    private const float RoundSeconds = 180f;
    private RoundRules rules;

    [SetUp]
    public void SetUp() => rules = new RoundRules(RoundSeconds, 50, 20f);

    private RoundState Evaluate(float timeLeft, float reputation, int earned) =>
        rules.Evaluate(timeLeft, reputation, earned, out _);

    [Test]
    public void StartOfRound_KeepsPlaying() =>
        Assert.AreEqual(RoundState.Playing, Evaluate(RoundSeconds, 50f, 0));

    [Test]
    public void MidRound_BelowTargetWithReputation_KeepsPlaying() =>
        Assert.AreEqual(RoundState.Playing, Evaluate(90f, 30f, 40));

    // ---------- Win ----------

    [Test]
    public void EarningExactly50_DoesNotWinYet() =>
        Assert.AreEqual(RoundState.Playing, Evaluate(90f, 50f, 50));

    [Test]
    public void Earning51_Wins() =>
        Assert.AreEqual(RoundState.Won, Evaluate(90f, 50f, 51));

    [Test]
    public void EarningWellOverTarget_Wins() =>
        Assert.AreEqual(RoundState.Won, Evaluate(90f, 50f, 120));

    [Test]
    public void GoingOverTargetOnLastFrame_StillWins() =>
        Assert.AreEqual(RoundState.Won, Evaluate(0f, 50f, 51));

    [Test]
    public void WinningWithReputationExactly20_Wins() =>
        Assert.AreEqual(RoundState.Won, Evaluate(90f, 20f, 51));

    [Test]
    public void Win_GivesReason()
    {
        rules.Evaluate(90f, 50f, 51, out string reason);
        StringAssert.Contains("$50", reason);
    }

    // ---------- Lose: reputation ----------

    [Test]
    public void ReputationExactly20_KeepsPlaying() =>
        Assert.AreEqual(RoundState.Playing, Evaluate(90f, 20f, 0));

    [Test]
    public void ReputationJustUnder20_Loses() =>
        Assert.AreEqual(RoundState.Lost, Evaluate(90f, 19.9f, 0));

    [Test]
    public void ReputationAtZero_Loses() =>
        Assert.AreEqual(RoundState.Lost, Evaluate(90f, 0f, 0));

    [Test]
    public void LowReputation_LosesEvenWithTargetMet() =>
        Assert.AreEqual(RoundState.Lost, Evaluate(90f, 10f, 60));

    [Test]
    public void ReputationLoss_GivesReason()
    {
        rules.Evaluate(90f, 10f, 0, out string reason);
        StringAssert.Contains("20%", reason);
    }

    // ---------- Lose: time ----------

    [Test]
    public void TimeRunsOutAtExactly50_Loses() =>
        Assert.AreEqual(RoundState.Lost, Evaluate(0f, 50f, 50));

    [Test]
    public void TimeRunsOutWithNothingEarned_Loses() =>
        Assert.AreEqual(RoundState.Lost, Evaluate(0f, 50f, 0));

    [Test]
    public void NegativeTimeLeft_Loses() =>
        Assert.AreEqual(RoundState.Lost, Evaluate(-1f, 50f, 0));

    [Test]
    public void TimeOut_GivesReason()
    {
        rules.Evaluate(0f, 50f, 0, out string reason);
        StringAssert.Contains("Time", reason);
    }

    [Test]
    public void StillPlaying_HasNoReason()
    {
        rules.Evaluate(90f, 50f, 0, out string reason);
        Assert.IsNull(reason);
    }

    // ---------- Settings ----------

    [Test]
    public void Constructor_StoresSettings()
    {
        Assert.AreEqual(180f, rules.RoundSeconds);
        Assert.AreEqual(50, rules.WinMoney);
        Assert.AreEqual(20f, rules.LoseReputation);
    }

    [Test]
    public void CustomSettings_AreUsed()
    {
        var custom = new RoundRules(30f, 10, 40f);
        Assert.AreEqual(RoundState.Won, custom.Evaluate(15f, 50f, 11, out _));
        Assert.AreEqual(RoundState.Playing, custom.Evaluate(15f, 50f, 10, out _));
        Assert.AreEqual(RoundState.Lost, custom.Evaluate(15f, 39f, 0, out _));
    }
}
