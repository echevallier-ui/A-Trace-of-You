using UnityEngine;
using UnityEngine.Events;

public class Locker : MonoBehaviour, ISaveable
{
    [SerializeField] private ItemData requiredKey;
    [SerializeField] private bool consumeKeyOnUse = false;
    [SerializeField] private UnityEvent completeEvent;
    [SerializeField] private UnityEvent onFailedEvent;

    private bool isUnlocked = false;

    public bool IsUnlocked => isUnlocked;

    public void TryOpen()
    {
        if (isUnlocked)
        {
            completeEvent?.Invoke();
            return;
        }

        if (PlayerInventory.Instance != null && PlayerInventory.Instance.HasItem(requiredKey))
        {
            isUnlocked = true;
            completeEvent?.Invoke();

            if (consumeKeyOnUse)
            {
                PlayerInventory.Instance.RemoveItem(requiredKey);
            }
        }
        else
        {
            onFailedEvent?.Invoke();
        }
    }

    // ---------- CONTRAT ISAVEABLE ----------

    public object SaveState()
    {
        return isUnlocked;
    }

    public void LoadState(object state)
    {
        if (state is bool savedUnlocked)
        {
            isUnlocked = savedUnlocked;
        }
        else if (state is string strState && strState == "DESTROYED")
        {
            Destroy(gameObject);
        }
    }
}