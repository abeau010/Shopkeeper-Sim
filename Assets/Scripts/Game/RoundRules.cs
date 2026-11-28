// The win/lose rules for one round. Plain C# with no Unity code, so it can be unit tested on its own.
public class RoundRules
{
    public float RoundSeconds { get; }
    public int WinMoney { get; }          // Total sales must go OVER this to win
    public float LoseReputation { get; }  // Reputation BELOW this loses

    public RoundRules(float roundSeconds, int winMoney, float loseReputation)
    {
        RoundSeconds = roundSeconds;
        WinMoney = winMoney;
        LoseReputation = loseReputation;
    }

    // Decides the round's result from the current stats. Reputation is checked first, then money, then the clock,
    // so a sale that goes over the target on the last frame still counts as a win.
    public RoundState Evaluate(float timeLeft, float reputation, int totalEarned, out string reason)
    {
        if (reputation < LoseReputation)
        {
            reason = $"Your reputation dropped below {LoseReputation:0}%.";
            return RoundState.Lost;
        }
        if (totalEarned > WinMoney)
        {
            reason = $"You made over ${WinMoney}!";
            return RoundState.Won;
        }
        if (timeLeft <= 0f)
        {
            reason = "Time's up!";
            return RoundState.Lost;
        }
        reason = null;
        return RoundState.Playing;
    }
}
