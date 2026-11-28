using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Connects a Round to the scene: feeds it time and shop stats, freezes gameplay when it ends, and restarts the scene.
// Adds itself to any scene with a ShopManager, along with its timer and end-screen UI, so no scene setup is needed.
// Add it to a GameObject by hand to change the settings in the Inspector.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool IsGameOver => Instance != null && Instance.Round != null && Instance.Round.IsOver;

    [SerializeField] private float roundSeconds = 180f;   // Length of the round
    [SerializeField] private int winMoney = 50;           // Total sales must go over this to win
    [SerializeField] private float loseReputation = 20f;  // Reputation below this loses

    public Round Round { get; private set; }
    public event Action<Round> GameEnded; // UI listens to this

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
        Time.timeScale = 1f; // In case the last round ended paused

        Round = new Round(new RoundRules(roundSeconds, winMoney, loseReputation));
        Round.Ended += OnRoundEnded;

        gameObject.AddComponent<RoundTimerUI>().Init(this);
        gameObject.AddComponent<GameOverUI>().Init(this);
    }

    private void Start()
    {
        if (ShopManager.Instance != null) ShopManager.Instance.OnStatsChanged += SendStatsToRound;
        SendStatsToRound();
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

    private void Update() => Round.Tick(Time.deltaTime);

    private void SendStatsToRound()
    {
        ShopManager shop = ShopManager.Instance;
        if (shop != null) Round.UpdateStats(shop.Reputation, shop.TotalEarned);
    }

    private void OnRoundEnded(Round round)
    {
        FreezeGameplay();
        GameEnded?.Invoke(round);
    }

    // Stops the player, customers and timers once the round is over
    private static void FreezeGameplay()
    {
        Time.timeScale = 0f;
        if (PriceSelectUI.Instance != null) PriceSelectUI.Instance.Close();
        foreach (PlayerInteraction player in FindObjectsByType<PlayerInteraction>()) player.enabled = false;
        foreach (CustomerSpawner spawner in FindObjectsByType<CustomerSpawner>()) spawner.enabled = false;
    }

    public void Restart()
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
