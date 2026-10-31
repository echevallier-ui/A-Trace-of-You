using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(20000)] // S'exécute en tout dernier, APRÈS la téléportation et Cinemachine
public class PortalCamera : MonoBehaviour
{
    [Header("Références")]
    [HideInInspector] public Camera playerCamera;
    public Transform entryPortal;
    public Transform exitPortal;
    public Camera portalCamera;
    public Material portalMaterial;

    [Header("Réglages du Plan de Coupe Oblique")]
    public bool useObliqueClipping = true;

    [Tooltip("Avance le plan de coupe pour supprimer le cadre du portail de sortie (0.01 à 0.03)")]
    public float clipPlaneOffset = 0.01f;

    [Tooltip("En dessous de cette distance (caméra virtuelle -> plan du portail de sortie), le plan oblique est " +
             "désactivé : il devient quasi confondu avec le near plane et provoque du flicker. (0.05 à 0.2)")]
    public float obliqueMinDistance = 0.1f;

    [Tooltip("Inverse la coupe si l'écran devient noir")]
    public bool invertClipPlane = false;

    [Header("Optimisation")]
    [Tooltip("Renderer de l'écran du portail d'ENTRÉE (celui qui affiche cette texture). " +
             "Le rendu est sauté quand il est hors champ ou masqué. Vide = on rend toujours.")]
    public Renderer screenRenderer;

    [Tooltip("Marge (m) autour de l'écran pour le test de visibilité")]
    public float visibilityPadding = 0.05f;

    private readonly Plane[] frustumPlanes = new Plane[6];

    private RenderTexture portalTexture;

    private void Start()
    {
        if (PersistentCoreManager.Instance != null)
        {
            playerCamera = PersistentCoreManager.Instance.MainCamera;
        }

        CreateRenderTexture();
    }

    private void LateUpdate()
    {
        if (playerCamera == null) return;

        if (entryPortal == null || exitPortal == null || portalCamera == null || portalMaterial == null)
            return;

        playerCamera.nearClipPlane = 0.01f;
        portalCamera.nearClipPlane = 0.01f;

        if (!IsScreenVisible()) return;

        if (portalTexture == null || portalTexture.width != Screen.width || portalTexture.height != Screen.height)
        {
            CreateRenderTexture();
        }

        // 1. Position et rotation relatives pour la frame
        Matrix4x4 relativeMatrix = PortalMath.GetMatrix(entryPortal, exitPortal)
            * playerCamera.transform.localToWorldMatrix;

        portalCamera.transform.SetPositionAndRotation(
            relativeMatrix.GetColumn(3),
            relativeMatrix.rotation
        );

        // 2. Synchronisation de la projection
        portalCamera.fieldOfView = playerCamera.fieldOfView;
        portalCamera.aspect = playerCamera.aspect;
        portalCamera.ResetProjectionMatrix();
        portalCamera.projectionMatrix = playerCamera.projectionMatrix;

        // 3. Plan oblique (désactivé quand la caméra virtuelle est trop près du plan de sortie)
        if (useObliqueClipping && DistanceToExitPlane() > obliqueMinDistance)
        {
            ApplyObliqueClippingPlane();
        }

        // 4. Rendu de la texture
        RenderPortal();
    }

    private bool IsScreenVisible()
    {
        if (screenRenderer == null) return true;
        if (!screenRenderer.enabled || !screenRenderer.gameObject.activeInHierarchy) return false;

        GeometryUtility.CalculateFrustumPlanes(playerCamera, frustumPlanes);
        Bounds b = screenRenderer.bounds;
        b.Expand(visibilityPadding);
        return GeometryUtility.TestPlanesAABB(frustumPlanes, b);
    }

    private float DistanceToExitPlane()
    {
        return Mathf.Abs(PortalMath.SignedDistance(exitPortal, portalCamera.transform.position));
    }

    private void ApplyObliqueClippingPlane()
    {
        Vector3 planeNormal = exitPortal.forward * (invertClipPlane ? -1f : 1f);
        Vector3 planePosition = exitPortal.position + (planeNormal * clipPlaneOffset);

        Matrix4x4 worldToCamera = portalCamera.worldToCameraMatrix;
        Vector3 cPos = worldToCamera.MultiplyPoint(planePosition);
        Vector3 cNormal = worldToCamera.MultiplyVector(planeNormal).normalized;

        float distance = -Vector3.Dot(cPos, cNormal);
        Vector4 clipPlaneCameraSpace = new Vector4(cNormal.x, cNormal.y, cNormal.z, distance);

        portalCamera.projectionMatrix = portalCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);
    }

    private void CreateRenderTexture()
    {
        if (portalTexture != null)
        {
            portalTexture.Release();
            DestroyImmediate(portalTexture);
        }

        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);

        portalTexture = new RenderTexture(width, height, 24, RenderTextureFormat.DefaultHDR)
        {
            name = $"PortalTexture_{gameObject.name}"
        };

        if (portalCamera != null) portalCamera.targetTexture = portalTexture;
        if (portalMaterial != null) portalMaterial.SetTexture("_MainTex", portalTexture);
    }

    private void RenderPortal()
    {
        if (portalCamera == null || portalTexture == null) return;

        UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest
        {
            destination = portalTexture
        };

        if (RenderPipeline.SupportsRenderRequest(portalCamera, request))
        {
            RenderPipeline.SubmitRenderRequest(portalCamera, request);
        }
    }

    private void OnDestroy()
    {
        if (portalCamera != null)
        {
            portalCamera.targetTexture = null;
        }

        if (portalTexture != null)
        {
            portalTexture.Release();

            if (Application.isPlaying) Destroy(portalTexture);
            else DestroyImmediate(portalTexture);

            portalTexture = null;
        }
    }
}