using UnityEngine;

[DefaultExecutionOrder(19995)] // Après PortalTeleporter (19990), avant PortalCamera (20000)
public class PortalObjectCloner : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Layer ignoré par la MainCamera mais rendu par la PortalCamera")]
    public string hiddenCloneLayer = "Portal";

    [Tooltip("Distance max (m) autour du portail de sortie pour que la MainCamera voie le clone")]
    public float visibilityRadius = 6f;

    private GameObject visualClone;
    private Transform currentEntryPortal;
    private Transform currentExitPortal;
    private PortalTeleporter exitTeleporter;

    private bool isInsidePortal = false;
    private int originalLayer;
    private int hiddenLayerIndex;

    private void Awake()
    {
        originalLayer = gameObject.layer;
        hiddenLayerIndex = LayerMask.NameToLayer(hiddenCloneLayer);
        if (hiddenLayerIndex == -1) hiddenLayerIndex = originalLayer;
    }

    private void LateUpdate() => UpdateClone();

    private void UpdateClone()
    {
        if (!isInsidePortal || visualClone == null || currentEntryPortal == null || currentExitPortal == null)
            return;

        Matrix4x4 portalMatrix = PortalMath.GetMatrix(currentEntryPortal, currentExitPortal);

        visualClone.transform.SetPositionAndRotation(
            portalMatrix.MultiplyPoint3x4(transform.position),
            portalMatrix.rotation * transform.rotation);

        UpdateCloneLayer();
    }

    // Le clone est vu par la MainCamera uniquement si le joueur est du côté du clone
    // (devant le portail de sortie du clone). Sinon il n'est vu qu'à travers la PortalCamera.
    private void UpdateCloneLayer()
    {
        if (exitTeleporter == null) return;

        bool visibleToPlayer = exitTeleporter.isPlayerOverlapping;

        if (!visibleToPlayer && exitTeleporter.playerCamera != null)
        {
            Vector3 toCam = exitTeleporter.playerCamera.transform.position - currentExitPortal.position;
            visibleToPlayer = Vector3.Dot(currentExitPortal.forward, toCam) > 0f
                              && toCam.sqrMagnitude < visibilityRadius * visibilityRadius;
        }

        int targetLayer = visibleToPlayer ? originalLayer : hiddenLayerIndex;
        if (visualClone.layer != targetLayer) SetLayerRecursive(visualClone, targetLayer);
    }

    public void OnEnterPortalZone(Transform entryPortal, Transform exitPortal)
    {
        currentEntryPortal = entryPortal;
        currentExitPortal = exitPortal;
        exitTeleporter = exitPortal.GetComponent<PortalTeleporter>();
        isInsidePortal = true;

        if (visualClone == null) CreateClone();
        UpdateClone(); // placement immédiat, sans attendre le LateUpdate
    }

    public void OnExitPortalZone()
    {
        isInsidePortal = false;
        DestroyClone();
    }

    private void CreateClone()
    {
        DestroyClone();

        visualClone = new GameObject($"{gameObject.name}_PortalClone");
        visualClone.layer = hiddenLayerIndex;
        visualClone.transform.localScale = transform.lossyScale;

        MeshFilter originalFilter = GetComponent<MeshFilter>();
        MeshRenderer originalRenderer = GetComponent<MeshRenderer>();

        if (originalFilter != null && originalRenderer != null)
            CopyMesh(visualClone, originalFilter, originalRenderer);

        Vector3 rootScale = transform.lossyScale;

        foreach (MeshRenderer childRenderer in GetComponentsInChildren<MeshRenderer>())
        {
            if (childRenderer.gameObject == gameObject) continue;

            MeshFilter childFilter = childRenderer.GetComponent<MeshFilter>();
            if (childFilter == null) continue;

            GameObject childClone = new GameObject(childRenderer.name);
            childClone.layer = hiddenLayerIndex;
            childClone.transform.SetParent(visualClone.transform, false);

            // Pose relative à la racine, quelle que soit la profondeur dans la hiérarchie
            childClone.transform.localPosition = transform.InverseTransformPoint(childRenderer.transform.position);
            childClone.transform.localRotation = Quaternion.Inverse(transform.rotation) * childRenderer.transform.rotation;

            // lossyScale monde / échelle de la racine (le clone racine porte déjà l'échelle)
            Vector3 ls = childRenderer.transform.lossyScale;
            childClone.transform.localScale = new Vector3(
                rootScale.x != 0f ? ls.x / rootScale.x : ls.x,
                rootScale.y != 0f ? ls.y / rootScale.y : ls.y,
                rootScale.z != 0f ? ls.z / rootScale.z : ls.z);

            CopyMesh(childClone, childFilter, childRenderer);
        }
    }

    private static void CopyMesh(GameObject target, MeshFilter srcFilter, MeshRenderer srcRenderer)
    {
        MeshFilter mf = target.AddComponent<MeshFilter>();
        MeshRenderer mr = target.AddComponent<MeshRenderer>();
        mf.sharedMesh = srcFilter.sharedMesh;
        mr.sharedMaterials = srcRenderer.sharedMaterials;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void DestroyClone()
    {
        if (visualClone != null)
        {
            Destroy(visualClone);
            visualClone = null;
        }
    }

    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, newLayer);
    }

    private void OnDisable() => DestroyClone();
    private void OnDestroy() => DestroyClone();
}