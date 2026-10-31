using UnityEngine;
using UnityEngine.Events;

public class OnDelayEvent : MonoBehaviour
{
    [SerializeField] private float delayInSeconds = 2f;
    [SerializeField] private UnityEvent onDelayedEvent;

    public void TriggerWithDelay()
    {
        Invoke(nameof(ExecuteEvent), delayInSeconds);
    }

    private void ExecuteEvent()
    {
        onDelayedEvent.Invoke();
    }
}