using UnityEngine;
using UnityEngine.Events;
using TMPro; // Utiliser TextMeshPro (standard Unity)

public class CountdownTimer : MonoBehaviour
{
    [Header("Réglages")]
    [SerializeField] private float durationInSeconds = 60f;
    [SerializeField] private bool autoStart = false;
    [SerializeField] private bool formatAsMinutes = true; // mm:ss ou simples secondes

    [Header("Affichage (Optionnel)")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Événements")]
    [SerializeField] private UnityEvent onTimerStart;
    [SerializeField] private UnityEvent onTimerEnd;

    private float currentRemainingTime;
    private bool isRunning = false;

    private void Start()
    {
        currentRemainingTime = durationInSeconds;
        UpdateUI();

        if (autoStart)
        {
            StartTimer();
        }
    }

    private void Update()
    {
        if (!isRunning) return;

        currentRemainingTime -= Time.deltaTime;

        if (currentRemainingTime <= 0f)
        {
            currentRemainingTime = 0f;
            isRunning = false;
            UpdateUI();
            onTimerEnd.Invoke();
        }
        else
        {
            UpdateUI();
        }
    }

    // --- Contrôles pilotables par UnityEvents ---

    public void StartTimer()
    {
        if (currentRemainingTime <= 0f) currentRemainingTime = durationInSeconds;
        isRunning = true;
        onTimerStart.Invoke();
    }

    public void PauseTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        
        
        currentRemainingTime = durationInSeconds;
        UpdateUI();
    }

    public void AddTime(float seconds)
    {
        currentRemainingTime += seconds;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (timerText == null) return;

        if (formatAsMinutes)
        {
            int minutes = Mathf.FloorToInt(currentRemainingTime / 60f);
            int seconds = Mathf.FloorToInt(currentRemainingTime % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        else
        {
            timerText.text = Mathf.CeilToInt(currentRemainingTime).ToString();
        }
    }
}