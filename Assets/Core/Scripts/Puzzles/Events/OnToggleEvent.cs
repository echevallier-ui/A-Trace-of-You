using UnityEngine;
using UnityEngine.Events;

public class OnToggleEvent : MonoBehaviour, ISaveable
{
    [SerializeField] private UnityEvent onTurnOn;
    [SerializeField] private UnityEvent onTurnOff;

    private bool isOn = false;

    public bool IsOn => isOn;

    public void Toggle()
    {
        isOn = !isOn;

        if (isOn)
            onTurnOn.Invoke();
        else
            onTurnOff.Invoke();
    }

    public void TurnOn()
    {
        if (isOn) return;
        isOn = true;
        onTurnOn?.Invoke();
    }

    public void TurnOff()
    {
        if (!isOn) return;
        isOn = false;
        onTurnOff?.Invoke();
    }

    public void ResetToggle()
    {
        isOn = false;
    }

    // ---------- CONTRAT ISAVEABLE ----------

    public object SaveState()
    {
        return isOn;
    }

    public void LoadState(object state)
    {
        if (state is bool savedIsOn)
        {
            isOn = savedIsOn;
        }
        else if (state is string strState && strState == "DESTROYED")
        {
            Destroy(gameObject);
        }
    }
}