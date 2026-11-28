using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Connects a Round to the scene: holds gameplay on the main menu, starts the round for the chosen level,
// feeds it time and shop stats, freezes gameplay when it ends, and reloads the scene.
// Adds itself to any scene with a ShopManager, along with its menu, timer and end-screen UI, so no scene setup is needed.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool IsGameOver => Instance != null && Instance.Round != null && Instance.Round.IsOver;

    public Difficulty Difficulty { get; private set; }
    public Round Round { get; private set; } // Null until a level is picked
    public event Action RoundStarted;
    public event Action<Round> GameEnded;

    private bool skipMenu;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        SceneManager.sceneLoaded += (_, _) => CreateIfNeeded();
        CreateIfNeeded();
    }

    private static void CreateIfNeeded()
    {
        if (FindAnyObjectByType<GameManager>() != null || FindAnyObjectByType<ShopManager>() == null) return;
        new GameObject("Game Manager").AddComponent<GameManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        gameObject.AddComponent<RoundTimerUI>().Init(this);
        gameObject.AddComponent<GameOverUI>().Init(this);
        MainMenuUI menu = gameObject.AddComponent<MainMenuUI>();
        menu.Init(this);

        // Hold everything until a level is picked. Done in Awake so customers don't start arriving behind the menu.
        SetGameplayActive(false);
        skipMenu = GameSession.ConsumeSkipMenu();
        if (!skipMenu) menu.Show();
    }

    private void Start()
    {
        if (ShopManager.Instance != null) ShopManager.Instance.OnStatsChanged += SendStatsToRound;
        if (skipMenu) StartRound(GameSession.SelectedDifficulty); // Play Again goes straight back in on the same level
    }

    private void OnDestroy()
    {
        if (ShopManager.Instance != null) ShopManager.Instance.OnStatsChanged -= SendStatsToRound;
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f;
        }
    }

    private void Update() => Round?.Tick(Time.deltaTime);

    // Called by the main menu
    public void StartRound(Difficulty difficulty)
    {
        if (Round != null) return;

        Difficulty = difficulty;
        GameSession.SelectedDifficulty = difficulty;
        Round = new Round(difficulty.CreateRules());
        Round.Ended += OnRoundEnded;
        SendStatsToRound();

        foreach (CustomerSpawner spawner in FindObjectsByType<CustomerSpawner>())
        {
            spawner.SetSpawnRateMultiplier(difficulty.SpawnRateMultiplier);
            spawner.SetMaxCustomers(difficulty.MaxCustomers);
        }
        SetGameplayActive(true);
        RoundStarted?.Invoke();
    }

    private void SendStatsToRound()
    {
        ShopManager shop = ShopManager.Instance;
        if (Round != null && shop != null) Round.UpdateStats(shop.Reputation, shop.TotalEarned);
    }

    private void OnRoundEnded(Round round)
    {
        SetGameplayActive(false);
        GameEnded?.Invoke(round);
    }

    // Pauses or resumes the player, customers and timers
    private static void SetGameplayActive(bool active)
    {
        Time.timeScale = active ? 1f : 0f;
        if (!active && PriceSelectUI.Instance != null) PriceSelectUI.Instance.Close();
        foreach (PlayerInteraction player in FindObjectsByType<PlayerInteraction>()) player.enabled = active;
        foreach (CustomerSpawner spawner in FindObjectsByType<CustomerSpawner>()) spawner.enabled = active;
    }

    // Same level again, skipping the menu
    public void Restart()
    {
        GameSession.SkipMenuOnNextLoad = true;
        ReloadScene();
    }

    public void ReturnToMenu()
    {
        GameSession.SkipMenuOnNextLoad = false;
        ReloadScene();
    }

    private static void ReloadScene()
    {
        Time.timeScale = 1f;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0)
        {
            SceneManager.LoadScene(scene.buildIndex);
            return;
        }
#if UNITY_EDITOR
        // Scene isn't in Build Settings - the editor can still reload it by path
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        Debug.LogError($"[Game] Add {scene.name} to Build Settings so it can be restarted.");
#endif
    }
}
