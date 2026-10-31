using UnityEngine;
using UnityEngine.EventSystems; // Requis pour les interfaces
using UnityEngine.Events;

public class OnClickEvent : MonoBehaviour, IInteractable
{    
    [SerializeField] private UnityEvent onClickEvent;
      
    public void OnInteractStart(Transform interactorTransform)
    {
        onClickEvent.Invoke();
    }

    public void OnInteractEnd()
    {
        
    }
}