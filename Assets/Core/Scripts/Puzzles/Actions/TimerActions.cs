using UnityEngine;
using UnityEngine.Events;

public class TimerActions : MonoBehaviour
{
    [Header("Événements Optionnels")]
    [SerializeField] private UnityEvent onActionExecuted;

    /// <summary>
    /// Réinitialise le chrono du [CORE].
    /// </summary>
    public void ResetTimer()
    {
        GetTimer()?.ResetTimer();
        onActionExecuted?.Invoke();
    }

    /// <summary>
    /// Démarre le chrono du [CORE].
    /// </summary>
    public void StartTimer()
    {
        GetTimer()?.StartTimer();
        onActionExecuted?.Invoke();
    }

    /// <summary>
    /// Met en pause le chrono du [CORE].
    /// </summary>
    public void PauseTimer()
    {
        GetTimer()?.PauseTimer();
        onActionExecuted?.Invoke();
    }

    /// <summary>
    /// Réinitialise ET relance immédiatement le chrono.
    /// </summary>
    public void ResetAndStartTimer()
    {
        CountdownTimer timer = GetTimer();
        if (timer != null)
        {
            timer.ResetTimer();
            timer.StartTimer();
            onActionExecuted?.Invoke();
        }
    }

    /// <summary>
    /// Ajoute (ou retire si négatif) du temps au chrono.
    /// </summary>
    public void AddTime(float seconds)
    {
        GetTimer()?.AddTime(seconds);
        onActionExecuted?.Invoke();
    }

    /// <summary>
    /// Récupère de manière sécurisée le CountdownTimer via le PersistentCoreManager.
    /// </summary>
    private CountdownTimer GetTimer()
    {
        if (PersistentCoreManager.Instance != null)
        {
            CountdownTimer timer = PersistentCoreManager.Instance.CountdownTimer;
            if (timer != null) return timer;
        }

        Debug.LogWarning("[TimerActions] Impossible d'accéder au CountdownTimer du PersistentCoreManager.");
        return null;
    }
}