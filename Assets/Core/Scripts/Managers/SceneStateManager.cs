using System.Collections.Generic;
using UnityEngine;

public class SceneStateManager : MonoBehaviour
{
    public static SceneStateManager Instance { get; private set; }

    // Dictionnaire : NomDeLaScene -> (ClefObjet -> DonneesDeSauvegarde)
    private Dictionary<string, Dictionary<string, object>> sceneStates = new Dictionary<string, Dictionary<string, object>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Enregistre l'état personnalisé d'un objet pour une scène donnée.
    /// </summary>
    public void SaveState(string sceneName, string objectKey, object state)
    {
        if (!sceneStates.ContainsKey(sceneName))
        {
            sceneStates[sceneName] = new Dictionary<string, object>();
        }

        sceneStates[sceneName][objectKey] = state;
    }

    /// <summary>
    /// Tente de récupérer l'état sauvegardé d'un objet.
    /// </summary>
    public bool TryGetState(string sceneName, string objectKey, out object state)
    {
        state = null;
        if (sceneStates.TryGetValue(sceneName, out var objects))
        {
            return objects.TryGetValue(objectKey, out state);
        }
        return false;
    }

    /// <summary>
    /// Réinitialise toutes les sauvegardes en mémoire (utile au retour au menu principal).
    /// </summary>
    public void ClearAllStates()
    {
        sceneStates.Clear();
    }
}