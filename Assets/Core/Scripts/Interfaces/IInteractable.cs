using UnityEngine;

public interface IInteractable
{
    void OnInteractStart(Transform interactorTransform);
    void OnInteractEnd();
}