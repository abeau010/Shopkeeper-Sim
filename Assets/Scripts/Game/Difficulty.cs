using System;
using System.Collections.Generic;

// Settings for one level. Plain C# with no Unity code, so it can be unit tested on its own.
// To add a level, add a new static field and put it in All - the main menu shows everything in All.
public class Difficulty
{
    public static readonly Difficulty Easy = new Difficulty("EASY", roundSeconds: 180f, winMoney: 50, loseReputation: 20f, spawnRateMultiplier: 1f, maxCustomers: 1);
    public static readonly Difficulty Hard = new Difficulty("HARD", roundSeconds: 120f, winMoney: 75, loseReputation: 20f, spawnRateMultiplier: 3f, maxCustomers: 3);
    public static IReadOnlyList<Difficulty> All { get; } = new[] { Easy, Hard };

    public string Name { get; }
    public float RoundSeconds { get; }
    public int WinMoney { get; }               // Total sales must go OVER this to win
    public float LoseReputation { get; }       // Reputation BELOW this loses
    public float SpawnRateMultiplier { get; }  // 3 = customers arrive 3x as often
    public int MaxCustomers { get; }           // How many customers can be in the shop at once

    public Difficulty(string name, float roundSeconds, int winMoney, float loseReputation, float spawnRateMultiplier, int maxCustomers)
    {
        if (spawnRateMultiplier <= 0f) throw new ArgumentOutOfRangeException(nameof(spawnRateMultiplier), "Must be above 0.");
        if (maxCustomers < 1) throw new ArgumentOutOfRangeException(nameof(maxCustomers), "Must be at least 1.");
        MaxCustomers = maxCustomers;
        Name = name;
        RoundSeconds = roundSeconds;
        WinMoney = winMoney;
        LoseReputation = loseReputation;
        SpawnRateMultiplier = spawnRateMultiplier;
    }

    public RoundRules CreateRules() => new RoundRules(RoundSeconds, WinMoney, LoseReputation);
}
