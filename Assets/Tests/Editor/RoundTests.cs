using NUnit.Framework;

// A whole round: the clock, stat updates, and ending exactly once.
// Run from Window > General > Test Runner > EditMode.
public class RoundTests
{
    private Round round;
    private int endedCount;

    [SetUp]
    public void SetUp()
    {
        round = new Round(new RoundRules(180f, 50, 20f), reputation: 50f);
        endedCount = 0;
        round.Ended += _ => endedCount++;
    }

    // ---------- Starting state ----------

    [Test]
    public void Starts_PlayingWithFullTime()
    {
        Assert.AreEqual(RoundState.Playing, round.State);
        Assert.IsFalse(round.IsOver);
        Assert.AreEqual(180f, round.TimeLeft);
        Assert.IsNull(round.EndReason);
    }

    [Test]
    public void Starts_WithGivenStats()
    {
        Assert.AreEqual(50f, round.Reputation);
        Assert.AreEqual(0, round.TotalEarned);
    }

    // ---------- Clock ----------

    [Test]
    public void Tick_CountsDown()
    {
        round.Tick(30f);
        Assert.AreEqual(150f, round.TimeLeft, 0.001f);
        Assert.AreEqual(RoundState.Playing, round.State);
    }

    [Test]
    public void ManySmallTicks_AddUp()
    {
        for (int i = 0; i < 60; i++) round.Tick(1f);
        Assert.AreEqual(120f, round.TimeLeft, 0.01f);
    }

    [Test]
    public void NegativeTick_IsIgnored()
    {
        round.Tick(-10f);
        Assert.AreEqual(180f, round.TimeLeft);
    }

    [Test]
    public void TickingPastZero_StopsAtZero()
    {
        round.Tick(500f);
        Assert.AreEqual(0f, round.TimeLeft);
    }

    [Test]
    public void ClockRunningOut_LosesWithTimeReason()
    {
        round.Tick(180f);
        Assert.AreEqual(RoundState.Lost, round.State);
        StringAssert.Contains("Time", round.EndReason);
    }

    [Test]
    public void OneSecondLeft_StillPlaying()
    {
        round.Tick(179f);
        Assert.AreEqual(RoundState.Playing, round.State);
    }

    // ---------- Stats ----------

    [Test]
    public void UpdateStats_StoresLatestValues()
    {
        round.UpdateStats(35f, 20);
        Assert.AreEqual(35f, round.Reputation);
        Assert.AreEqual(20, round.TotalEarned);
    }

    [Test]
    public void GoingOver50_Wins()
    {
        round.UpdateStats(50f, 51);
        Assert.AreEqual(RoundState.Won, round.State);
        Assert.IsTrue(round.IsOver);
    }

    [Test]
    public void ReputationBelow20_Loses()
    {
        round.UpdateStats(15f, 0);
        Assert.AreEqual(RoundState.Lost, round.State);
        StringAssert.Contains("reputation", round.EndReason);
    }

    // ---------- Ending once ----------

    [Test]
    public void Ended_FiresOnceOnWin()
    {
        round.UpdateStats(50f, 60);
        round.UpdateStats(50f, 80);
        round.Tick(500f);
        Assert.AreEqual(1, endedCount);
    }

    [Test]
    public void Ended_FiresOnceOnTimeOut()
    {
        round.Tick(180f);
        round.Tick(10f);
        round.UpdateStats(50f, 100);
        Assert.AreEqual(1, endedCount);
    }

    [Test]
    public void Ended_PassesTheRound()
    {
        Round received = null;
        round.Ended += r => received = r;
        round.UpdateStats(50f, 51);
        Assert.AreSame(round, received);
    }

    [Test]
    public void Ended_DoesNotFireWhileStillPlaying()
    {
        round.Tick(60f);
        round.UpdateStats(40f, 30);
        Assert.AreEqual(0, endedCount);
    }

    [Test]
    public void AfterWinning_ClockStops()
    {
        round.Tick(60f);
        round.UpdateStats(50f, 51);
        round.Tick(30f);
        Assert.AreEqual(120f, round.TimeLeft, 0.001f);
    }

    [Test]
    public void AfterWinning_TimeRunningOutDoesNotChangeResult()
    {
        round.UpdateStats(50f, 51);
        round.Tick(500f);
        Assert.AreEqual(RoundState.Won, round.State);
    }

    [Test]
    public void AfterLosing_LaterSalesDoNotChangeResult()
    {
        round.UpdateStats(10f, 0);
        round.UpdateStats(50f, 100);
        Assert.AreEqual(RoundState.Lost, round.State);
        Assert.AreEqual(0, round.TotalEarned); // Stats freeze at the moment the round ended
    }

    // ---------- Full rounds ----------

    [Test]
    public void TypicalWinningRound()
    {
        round.Tick(40f); round.UpdateStats(55f, 15);
        round.Tick(40f); round.UpdateStats(48f, 35);
        round.Tick(40f); round.UpdateStats(52f, 58);
        Assert.AreEqual(RoundState.Won, round.State);
        Assert.AreEqual(60f, round.TimeLeft, 0.001f);
    }

    [Test]
    public void SlowRound_RunsOutOfTime()
    {
        round.Tick(90f); round.UpdateStats(50f, 20);
        round.Tick(90f);
        Assert.AreEqual(RoundState.Lost, round.State);
        StringAssert.Contains("Time", round.EndReason);
    }

    [Test]
    public void AngryCustomers_LoseBeforeTimeIsUp()
    {
        round.Tick(30f); round.UpdateStats(35f, 10);
        round.Tick(30f); round.UpdateStats(18f, 10);
        Assert.AreEqual(RoundState.Lost, round.State);
        Assert.AreEqual(120f, round.TimeLeft, 0.001f);
    }

    // ---------- Time formatting ----------

    [TestCase(180f, "3:00")]
    [TestCase(61f, "1:01")]
    [TestCase(60f, "1:00")]
    [TestCase(59.2f, "1:00")] // Rounds up
    [TestCase(9f, "0:09")]
    [TestCase(0.1f, "0:01")]  // Only shows 0:00 when time is really up
    [TestCase(0f, "0:00")]
    [TestCase(-5f, "0:00")]
    public void FormatTime_ShowsMinutesAndSeconds(float seconds, string expected) =>
        Assert.AreEqual(expected, Round.FormatTime(seconds));
}
