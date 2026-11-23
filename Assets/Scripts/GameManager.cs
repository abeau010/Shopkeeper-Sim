using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Runs one timed round. Win by earning winMoney before time runs out, lose if reputation hits 0 or the timer ends first.
// Adds itself to any scene with a ShopManager and builds its own UI, so no scene setup is needed.
// Add it to a GameObject by hand to change the settings in the Inspector.
public class GameManager : MonoBehaviour
{
    public enum State { Playing, Won, Lost }

    public static GameManager Instance { get; private set; }
    public static bool IsGameOver => Instance != null && Instance.CurrentState != State.Playing;

    private const float WarningSeconds = 30f; // Timer turns red below this
    private const string NormalColor = "#FFFFFF";
    private const string WarningColor = "#E74C3C";

    [SerializeField] private float roundSeconds = 300f; // Length of the round
    [SerializeField] private int winMoney = 100;        // Total sales needed to win

    public State CurrentState { get; private set; } = State.Playing;
    public float TimeLeft { get; private set; }

    private TextMeshProUGUI timerText;
    private GameObject endPanel;
    private TextMeshProUGUI endTitleText;
    private TextMeshProUGUI endDetailText;

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
        TimeLeft = roundSeconds;
        BuildUI();
    }

    private void Start()
    {
        if (ShopManager.Instance != null) ShopManager.Instance.OnStatsChanged += CheckStats;
        CheckStats();
    }

    private void OnDestroy()
    {
        if (ShopManager.Instance != null) ShopManager.Instance.OnStatsChanged -= CheckStats;
        if (Instance == this)
        {
            Instance = null;
            Time.timeScale = 1f;
        }
    }

    private void Update()
    {
        if (CurrentState != State.Playing) return;

        TimeLeft = Mathf.Max(0f, TimeLeft - Time.deltaTime);
        RefreshTimer();
        if (TimeLeft <= 0f) EndGame(State.Lost, "Time's up!");
    }

    // Runs whenever money or reputation changes
    private void CheckStats()
    {
        if (CurrentState != State.Playing || ShopManager.Instance == null) return;

        if (ShopManager.Instance.Reputation <= 0f) EndGame(State.Lost, "Your reputation hit 0%.");
        else if (ShopManager.Instance.TotalEarned >= winMoney) EndGame(State.Won, $"You made ${winMoney}!");
    }

    private void EndGame(State result, string reason)
    {
        CurrentState = result;
        Time.timeScale = 0f; // Freezes movement, customers and timers

        // Stop the player and customers from doing anything else
        if (PriceSelectUI.Instance != null) PriceSelectUI.Instance.Close();
        foreach (PlayerInteraction player in FindObjectsByType<PlayerInteraction>()) player.enabled = false;
        foreach (CustomerSpawner spawner in FindObjectsByType<CustomerSpawner>()) spawner.enabled = false;

        int earned = ShopManager.Instance != null ? ShopManager.Instance.TotalEarned : 0;
        float reputation = ShopManager.Instance != null ? ShopManager.Instance.Reputation : 0f;
        int secondsLeft = Mathf.CeilToInt(TimeLeft);

        endTitleText.text = result == State.Won ? "<color=#2ECC71>You Win!</color>" : "<color=#E74C3C>You Lose</color>";
        endDetailText.text = $"{reason}\n\nEarned: ${earned} / ${winMoney}\nReputation: {reputation:0}%\n" +
                             $"Time left: {secondsLeft / 60}:{secondsLeft % 60:00}";
        endPanel.SetActive(true);
        RefreshTimer();
    }

    private void Restart()
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

    private void RefreshTimer()
    {
        int seconds = Mathf.CeilToInt(TimeLeft);
        string color = TimeLeft <= WarningSeconds ? WarningColor : NormalColor;
        timerText.text = $"<color={color}>Time: {seconds / 60}:{seconds % 60:00}</color>";
    }

    // ---------- Runtime UI construction ----------

    private void BuildUI()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        var canvasGO = new GameObject("Game Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Above every other UI so the end screen covers it
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Round timer, top-right
        timerText = CreateText("Round Timer", canvasGO.transform, 40, TextAlignmentOptions.TopRight);
        timerText.fontStyle = FontStyles.Bold;
        SetRect(timerText.rectTransform, Vector2.one, Vector2.one, new Vector2(-420, -80), new Vector2(-30, -20));
        RefreshTimer();

        // End screen: dark full-screen backdrop (also blocks clicks on the UI underneath) with a centred panel
        endPanel = new GameObject("End Screen", typeof(RectTransform), typeof(Image));
        endPanel.transform.SetParent(canvasGO.transform, false);
        endPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
        SetRect(endPanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var box = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(endPanel.transform, false);
        box.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        Vector2 center = new Vector2(0.5f, 0.5f);
        SetRect(box.GetComponent<RectTransform>(), center, center, new Vector2(-350, -250), new Vector2(350, 250));

        Vector2 top = new Vector2(0, 1), topRight = Vector2.one;
        endTitleText = CreateText("Title", box.transform, 80, TextAlignmentOptions.Center);
        endTitleText.fontStyle = FontStyles.Bold;
        SetRect(endTitleText.rectTransform, top, topRight, new Vector2(20, -140), new Vector2(-20, -30));

        endDetailText = CreateText("Details", box.transform, 32, TextAlignmentOptions.Top);
        SetRect(endDetailText.rectTransform, Vector2.zero, Vector2.one, new Vector2(30, 120), new Vector2(-30, -160));

        var restart = new GameObject("Restart Button", typeof(RectTransform), typeof(Image), typeof(Button));
        restart.transform.SetParent(box.transform, false);
        restart.GetComponent<Image>().color = new Color(0.18f, 0.55f, 0.34f);
        restart.GetComponent<Button>().onClick.AddListener(Restart);
        Vector2 bottomCenter = new Vector2(0.5f, 0);
        SetRect((RectTransform)restart.transform, bottomCenter, bottomCenter, new Vector2(-120, 30), new Vector2(120, 100));
        var label = CreateText("Label", restart.transform, 34, TextAlignmentOptions.Center);
        SetRect(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        label.text = "Play Again";

        endPanel.SetActive(false);
    }

    private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, float size, TextAlignmentOptions alignment)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }
}
