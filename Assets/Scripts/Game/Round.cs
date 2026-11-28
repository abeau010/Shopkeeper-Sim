using System;

public enum RoundState { Playing, Won, Lost }

// One timed round: counts the clock down, tracks the latest stats, and ends exactly once using RoundRules.
// Plain C# with no Unity code - GameManager feeds it the frame time and the shop's stats.
public class Round
{
    public RoundRules Rules { get; }
    public RoundState State { get; private set; } = RoundState.Playing;
    public bool IsOver => State != RoundState.Playing;
    public float TimeLeft { get; private set; }
    public float Reputation { get; private set; }
    public int TotalEarned { get; private set; }
    public string EndReason { get; private set; }

    public event Action<Round> Ended; // Fires once, when the round is won or lost

    public Round(RoundRules rules, float reputation = 100f, int totalEarned = 0)
    {
        Rules = rules;
        TimeLeft = rules.RoundSeconds;
        Reputation = reputation;
        TotalEarned = totalEarned;
    }

    // Counts the clock down. Does nothing once the round is over.
    public void Tick(float deltaTime)
    {
        if (IsOver) return;
        TimeLeft = Math.Max(0f, TimeLeft - Math.Max(0f, deltaTime));
        CheckForEnd();
    }

    // Call whenever money or reputation changes. Ignored once the round is over.
    public void UpdateStats(float reputation, int totalEarned)
    {
        if (IsOver) return;
        Reputation = reputation;
        TotalEarned = totalEarned;
        CheckForEnd();
    }

    private void CheckForEnd()
    {
        RoundState result = Rules.Evaluate(TimeLeft, Reputation, TotalEarned, out string reason);
        if (result == RoundState.Playing) return;

        State = result;
        EndReason = reason;
        Ended?.Invoke(this);
    }

    // Seconds as m:ss, rounded up so the clock only shows 0:00 when time is really up
    public static string FormatTime(float seconds)
    {
        int total = (int)Math.Ceiling(Math.Max(0f, seconds));
        return $"{total / 60}:{total % 60:00}";
    }
}
