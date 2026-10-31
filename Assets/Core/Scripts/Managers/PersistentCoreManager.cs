using UnityEngine;
using StarterAssets;

public class PersistentCoreManager : MonoBehaviour
{
    private const string CORE_PREFAB_PATH = "[CORE]";

    public static PersistentCoreManager Instance { get; private set; }

    [Header("Références Joueur & Caméra")]
    [SerializeField] private GameObject playerGameObject;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private PlayerInteractor playerInteractor;

    [Header("Systèmes Core")]
    [SerializeField] private CountdownTimer countdownTimer;

    // Propriétés d'accès direct aux sous-systèmes
    public GameObject PlayerGameObject => playerGameObject;
    public Transform PlayerTransform => playerGameObject != null ? playerGameObject.transform : null;
    public Camera MainCamera => mainCamera;
    public FirstPersonController PlayerController => playerController;
    public PlayerInteractor PlayerInteractor => playerInteractor;
    
    // Accès direct au CountdownTimer
    public CountdownTimer CountdownTimer
    {
        get
        {
            if (countdownTimer == null)
            {
                countdownTimer = GetComponentInChildren<CountdownTimer>();
            }
            return countdownTimer;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitializeCore()
    {
        if (Instance != null) return;

        PersistentCoreManager existingInScene = FindFirstObjectByType<PersistentCoreManager>();
        if (existingInScene != null) return;

        GameObject corePrefab = Resources.Load<GameObject>(CORE_PREFAB_PATH);

        if (corePrefab != null)
        {
            GameObject coreInstance = Instantiate(corePrefab);
            coreInstance.name = "[CORE]";
            DontDestroyOnLoad(coreInstance);
        }
        else
        {
            Debug.LogError($"[CORE] ERREUR : Aucun Prefab trouvé dans 'Resources/{CORE_PREFAB_PATH}.prefab'");
        }
    }

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
}