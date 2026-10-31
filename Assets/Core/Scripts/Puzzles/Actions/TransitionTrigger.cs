using UnityEngine;

public class TransitionTrigger : MonoBehaviour
{
    [Header("Destination")]
    [SerializeField] private string sceneToLoad;
    [Tooltip("L'ID du SceneSpawnPoint où le joueur doit apparaître. Laisser 'Default' s'il n'y en a qu'un.")]
    [SerializeField] private string targetSpawnPointID = "Default";

    public void ExecuteTransition()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning($"[TransitionTrigger] Aucune scène spécifiée sur {gameObject.name}");
            return;
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(sceneToLoad, targetSpawnPointID);
        }
        else
        {
            Debug.LogError("[TransitionTrigger] SceneTransitionManager introuvable dans [CORE] !");
        }
    }
}