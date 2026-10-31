using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("UI Transition")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    public string TargetSpawnPointID { get; private set; } = "";
    public bool IsTransitioning { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void LoadScene(string sceneName, string spawnPointID = "")
    {
        if (IsTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName, spawnPointID));
    }

    private IEnumerator TransitionRoutine(string sceneName, string spawnPointID)
    {
        IsTransitioning = true;
        TargetSpawnPointID = spawnPointID;

        // 1. Sauvegarde automatique de l'état de la scène courante
        if (AutoSceneSaveState.Instance != null)
        {
            AutoSceneSaveState.Instance.SaveCurrentSceneState();
        }

        // 2. Désactive les contrôles
        if (PersistentCoreManager.Instance != null && PersistentCoreManager.Instance.PlayerController != null)
        {
            PersistentCoreManager.Instance.PlayerController.enabled = false;
        }

        // 3. SÉCURITÉ JOUEUR : Rapatrie le Joueur sous le [CORE] pour éviter qu'il ne soit 
        // détruit au déchargement s'il était devenu l'enfant d'un objet de la scène (plate-forme, Trigger, etc.)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && PersistentCoreManager.Instance != null)
        {
            player.transform.SetParent(PersistentCoreManager.Instance.transform);
        }

        // 4. Fondu au noir
        yield return StartCoroutine(Fade(1f));

        // 5. Chargement asynchrone
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return new WaitForEndOfFrame();

        // 6. Fondu transparent et réactivation
        yield return StartCoroutine(Fade(0f));

        if (PersistentCoreManager.Instance != null && PersistentCoreManager.Instance.PlayerController != null)
        {
            PersistentCoreManager.Instance.PlayerController.enabled = true;
        }

        IsTransitioning = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.gameObject.SetActive(true);
        fadeCanvasGroup.blocksRaycasts = targetAlpha > 0f;

        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            fadeCanvasGroup.gameObject.SetActive(false);
        }
    }
}