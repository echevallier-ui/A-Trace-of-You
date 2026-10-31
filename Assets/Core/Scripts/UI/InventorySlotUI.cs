using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    public void SetItem(ItemData item)
    {
        if (item != null && item.icon != null)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true; // Affiche l'icône
        }
        else
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        iconImage.sprite = null;
        iconImage.enabled = false; // Masque l'icône si la case est vide
    }
}