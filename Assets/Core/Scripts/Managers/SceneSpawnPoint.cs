using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnPointID = "Default";

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string targetID = SceneTransitionManager.Instance != null 
            ? SceneTransitionManager.Instance.TargetSpawnPointID 
            : "";

        if (string.IsNullOrEmpty(targetID) || targetID == spawnPointID)
        {
            TeleportPlayerToSpawn();
        }
    }

    private void TeleportPlayerToSpawn()
    {
        if (PersistentCoreManager.Instance != null && PersistentCoreManager.Instance.PlayerGameObject != null)
        {
            GameObject player = PersistentCoreManager.Instance.PlayerGameObject;

            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;

            player.transform.position = transform.position;
            player.transform.rotation = transform.rotation;

            if (controller != null) controller.enabled = true;
        }
    }

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 1. Dessiner une icône 2D au-dessus du point d'apparition
        // (Cherche une image dans le dossier "Assets/Gizmos/")
        Gizmos.DrawIcon(transform.position + Vector3.up * 1.0f, "SpawnIcon.png", true);

        // 2. Dessiner une étiquette avec l'ID du SpawnPoint
        UnityEditor.Handles.color = Color.cyan;
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.cyan;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontStyle = FontStyle.Bold;

        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2.0f, 
            $"[Spawn: {spawnPointID}]", 
            style
        );

        UnityEditor.Handles.ArrowHandleCap(
            0,
            transform.position,
            transform.rotation,
            1.5f,
            EventType.Repaint
        );

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
    #endif
}