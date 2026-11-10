using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopHUD : MonoBehaviour
{
    public static ShopHUD Instance { get; private set; }
    [Header("Customer Satisfaction")]
    [SerializeField] private Image satisfactionFill;
    [SerializeField] private TextMeshProUGUI satisfactionText;
    [SerializeField] private float maxSatisfaction = 100f;

    [Header("Day Timer")]
    [SerializeField] private Image clockFill;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private float dayDurationSeconds = 180f;

    [Header("Money")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private float startMoney = 0f;

    public event Action OnDayEnded;

    private float currentSatisfaction;
    private float timeRemaining;
    private bool isTimerRunning = true;
    private float currentMoney;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        currentSatisfaction = maxSatisfaction / 2;
        timeRemaining = dayDurationSeconds;
        currentMoney = startMoney;
        UpdateSatisfactionUI();
        UpdateTimerUI();
        UpdateMoneyUI();
    }

    private void Update()
    {
        if (!isTimerRunning) return;
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();
        }
        else
        {
            timeRemaining = 0;
            isTimerRunning = false;
            UpdateTimerUI();
            OnDayEnded?.Invoke();
        }
    }

    public void ModifySatisfaction(float amount)
    {
        currentSatisfaction = Mathf.Clamp(currentSatisfaction + amount, 0f, maxSatisfaction);
        UpdateSatisfactionUI();
    }

    private void UpdateSatisfactionUI()
    {
        float ratio = currentSatisfaction / maxSatisfaction;
        if (satisfactionFill != null)
        {
            satisfactionFill.fillAmount = ratio;
        }
        if (satisfactionText != null)
        {
            satisfactionText.text = $"{ratio * 100}%";
        }
    }

    private void UpdateTimerUI()
    {
        float elapsedTime = dayDurationSeconds - timeRemaining;
        if (clockFill != null)
        {
            clockFill.fillAmount = Mathf.Clamp01(elapsedTime / dayDurationSeconds);
        }
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    public void ModifyMoney(float amount)
    {
        currentMoney = Mathf.Max(currentMoney + amount, 0f);
        UpdateMoneyUI();
    }

    public float GetMoney()
    {
        return currentMoney;
    }

    private void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = $"{currentMoney}$";
        }
    }
}