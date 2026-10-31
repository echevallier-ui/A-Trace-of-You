using UnityEngine;
using UnityEngine.Events;

public class OnTriggerEvent : MonoBehaviour
{
    [Header("Filtre")]
    [SerializeField] private bool useTagFilter = true;
    [SerializeField] private string targetTag = "Player";

    [Header("Parentage")]
    [Tooltip("Si vrai, l'objet entrant devient enfant de cet objet et perd son parent à la sortie.")]
    [SerializeField] private bool parentOnTrigger = false;

    [Header("Destruction")]
    [Tooltip("Si vrai, l'objet est détruit quand TriggerEnter")]
    [SerializeField] private bool DestroyOnTrigger = false;

    [Header("Events")]
    [SerializeField] private UnityEvent onTriggerEnterEvent;
    [SerializeField] private UnityEvent onTriggerStayEvent;
    [SerializeField] private UnityEvent onTriggerExitEvent;

    private bool IsValid(Collider other)
    {
        if (!useTagFilter || string.IsNullOrEmpty(targetTag)) return true;
        return other.CompareTag(targetTag);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsValid(other))
        {
            if (parentOnTrigger)
            {
                other.transform.SetParent(transform);
            }

            if(DestroyOnTrigger)
            {
                Destroy(gameObject);
            }

            onTriggerEnterEvent.Invoke();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsValid(other))
        {
            onTriggerStayEvent.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsValid(other))
        {
            if (parentOnTrigger && other.transform.parent == transform)
            {
                other.transform.SetParent(null);
            }

            onTriggerExitEvent.Invoke();
        }
    }
}