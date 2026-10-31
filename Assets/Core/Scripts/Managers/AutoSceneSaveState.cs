using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoSceneSaveState : MonoBehaviour
{
    public static AutoSceneSaveState Instance { get; private set; }

    // Conteneur sous [CORE] pour préserver les objets transportés d'une autre scène
    private Transform stashedObjectsContainer;

    // Dictionnaire des objets importés en réserve : GameObject -> Nom de la scène cible
    private Dictionary<GameObject, string> stashedObjects = new Dictionary<GameObject, string>();

    // Dictionnaire des objets natifs originaux de la scène active (Clé Path -> GameObject mémoire)
    private Dictionary<string, GameObject> nativeObjectsInCurrentScene = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        GameObject containerGo = new GameObject("[StashedCrossSceneObjects]");
        stashedObjectsContainer = containerGo.transform;
        stashedObjectsContainer.SetParent(transform);
        containerGo.SetActive(false);
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    // ---------- RESTAURATION AU CHARGEMENT DE SCÈNE ----------
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SceneStateManager.Instance == null) return;

        string sceneName = scene.name;
        nativeObjectsInCurrentScene.Clear();
        HashSet<GameObject> importedGameObjects = new HashSet<GameObject>();

        // 1. Réinjecte les objets IMPORTÉS mis en réserve lors d'une visite précédente dans cette scène
        List<GameObject> objectsToUnstash = new List<GameObject>();
        foreach (var kvp in stashedObjects)
        {
            if (kvp.Value == sceneName && kvp.Key != null)
            {
                objectsToUnstash.Add(kvp.Key);
            }
        }

        foreach (GameObject stashedGo in objectsToUnstash)
        {
            stashedObjects.Remove(stashedGo);
            stashedGo.transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(stashedGo, scene);
            stashedGo.SetActive(true);
            importedGameObjects.Add(stashedGo);
            //Debug.Log($"[SAVE SYSTEM] 🚚 Objet importé réinjecté dans '{sceneName}' : {stashedGo.name}");
        }

        // 2. Récupère tous les composants ISaveable de la scène
        List<ISaveable> saveables = GetSaveablesInScene(scene);

        foreach (var saveable in saveables)
        {
            if (saveable is MonoBehaviour script && script != null)
            {
                string key = GetHierarchyPath(script.transform);

                // Si l'objet n'a pas été réinjecté de la réserve, c'est un objet natif du fichier de scène
                if (!importedGameObjects.Contains(script.gameObject))
                {
                    nativeObjectsInCurrentScene[key] = script.gameObject;
                }

                // Restaure l'état sauvegardé
                if (SceneStateManager.Instance.TryGetState(sceneName, key, out var savedData))
                {
                    saveable.LoadState(savedData);
                }
            }
        }
    }

    // ---------- SAUVEGARDE AVANT LE DÉCHARGEMENT ----------
    public void SaveCurrentSceneState()
    {
        if (SceneStateManager.Instance == null) return;

        Scene activeScene = SceneManager.GetActiveScene();
        string sceneName = activeScene.name;
        HashSet<string> savedNativeKeys = new HashSet<string>();

        List<ISaveable> saveables = GetSaveablesInScene(activeScene);

        foreach (var saveable in saveables)
        {
            if (saveable is MonoBehaviour script && script != null)
            {
                string key = GetHierarchyPath(script.transform);

                // Sauvegarde l'état de l'objet dans SceneStateManager
                object state = saveable.SaveState();
                SceneStateManager.Instance.SaveState(sceneName, key, state);

                // Vérifie si cet objet exact est natif de ce niveau
                bool isNative = nativeObjectsInCurrentScene.TryGetValue(key, out GameObject nativeGo) && nativeGo == script.gameObject;

                if (isNative)
                {
                    savedNativeKeys.Add(key);
                }
                else
                {
                    // L'objet est apporté/déposé -> On le met sous [CORE] pour qu'il survisse au déchargement Unity
                    script.gameObject.transform.SetParent(stashedObjectsContainer);
                    script.gameObject.SetActive(false);
                    stashedObjects[script.gameObject] = sceneName;
                    //Debug.Log($"[SAVE SYSTEM] 📦 Objet importé mis en réserve pour '{sceneName}' : {key}");
                }
            }
        }

        // Marque comme DESTROYED les objets natifs qui ont été ramassés ou emportés hors du niveau
        foreach (var kvp in nativeObjectsInCurrentScene)
        {
            string nativeKey = kvp.Key;
            if (!savedNativeKeys.Contains(nativeKey))
            {
                SceneStateManager.Instance.SaveState(sceneName, nativeKey, "DESTROYED");
                //Debug.Log($"[SAVE SYSTEM] 🗑️ Objet natif retiré/détruit de '{sceneName}' : {nativeKey}");
            }
        }
    }

    private List<ISaveable> GetSaveablesInScene(Scene scene)
    {
        List<ISaveable> results = new List<ISaveable>();
        if (!scene.IsValid() || !scene.isLoaded) return results;

        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (GameObject root in rootObjects)
        {
            ISaveable[] saveables = root.GetComponentsInChildren<ISaveable>(true);
            results.AddRange(saveables);
        }
        return results;
    }

    private string GetHierarchyPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null && t.parent.gameObject.scene.name != "DontDestroyOnLoad")
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}