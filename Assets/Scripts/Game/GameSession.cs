using UnityEngine;

// Remembers choices across scene reloads: which level was picked, and whether Play Again should skip the main menu
public static class GameSession
{
    public static Difficulty SelectedDifficulty { get; set; } = Difficulty.Easy;
    public static bool SkipMenuOnNextLoad { get; set; }

    // True once if the menu should be skipped, then goes back to false
    public static bool ConsumeSkipMenu()
    {
        bool skip = SkipMenuOnNextLoad;
        SkipMenuOnNextLoad = false;
        return skip;
    }

    // Runs before each Play session, so choices from the last session don't carry over in the editor
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reset()
    {
        SelectedDifficulty = Difficulty.Easy;
        SkipMenuOnNextLoad = false;
    }
}
