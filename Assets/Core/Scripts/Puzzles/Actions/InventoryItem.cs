using UnityEngine;

public class InventoryItem : MonoBehaviour, ISaveable
{
    [SerializeField] private ItemData itemData;

    public void Collect()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.AddItem(itemData);
            Destroy(gameObject);
        }
    }

    // --- CONTRAT ISAVEABLE ---
    public object SaveState()
    {
        return false; // Est-il détruit ? Non, puisqu'il est encore là au moment de sauvegarder.
    }

    public void LoadState(object state)
    {
        // Si les données indiquent qu'il a été détruit/ramassé lors d'une visite précédente
        if (state is string strState && strState == "DESTROYED")
        {
            Destroy(gameObject);
        }
    }
}