using UnityEngine;
using UnityEngine.Events;

public class TagChecker : MonoBehaviour
{
    [SerializeField] private string requiredItemID = "Key";
    [SerializeField] private Transform snapPoint; // Point où la clé vient se fixer
    [SerializeField] private UnityEvent onKeyInserted; // Événement Unity (ex: ouvrir porte, allumer lumière)

    private bool isSolved = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isSolved) return;

        // On cherche le composant KeyIdentity sur l'objet ou ses parents
        ItemTag item = other.GetComponentInParent<ItemTag>();

        if (item != null && item.ItemID == requiredItemID)
        {
            AcceptItem(item);
        }
    }

    private void AcceptItem(ItemTag item)
    {
        isSolved = true;

        // 1. Force le relâchement si le joueur tenait l'objet
        if (item.TryGetComponent(out IInteractable interactable))
        {
            interactable.OnInteractEnd();
        }

        // 2. Bloque la physique et les interactions futures
        if (item.TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true;
        }

        if (item.TryGetComponent(out InteractableObject grabbable))
        {
            grabbable.enabled = false; // L'objet ne peut plus être attrapé
        }

        // 3. Magnetise / Snap la clé sur le socle
        item.transform.position = snapPoint.position;
        item.transform.rotation = snapPoint.rotation;
        item.transform.SetParent(snapPoint);

        // 4. Déclenche la suite du puzzle (configurable dans l'Inspector)
        onKeyInserted?.Invoke();
    }
}