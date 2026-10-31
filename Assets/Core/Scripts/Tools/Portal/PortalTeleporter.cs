using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(19990)] // Avant le cloner (19995) et la PortalCamera (20000)
public class PortalTeleporter : MonoBehaviour
{
    [Header("Références")]
    [HideInInspector] public Transform player;
    [HideInInspector] public Camera playerCamera;
    public PortalTeleporter receiver;

    [Header("Écran du portail")]
    [Tooltip("Un CUBE (sans collider) dont l'axe local Z est aligné sur la normale du portail. " +
             "Son épaisseur est pilotée par le script. Le material doit être en Render Face: Both / Cull Off.")]
    public Transform portalVisualQuad;

    [Tooltip("Décalage de la face avant vers la caméra. 0 = face avant sur le plan du portail. " +
             "À augmenter (0.003 à 0.01) uniquement si l'écran fait du z-fighting avec un mur plein.")]
    [SerializeField] private float screenFrontOffset = 0f;

    [Tooltip("Marge de sécurité sur l'épaisseur calculée")]
    [SerializeField] private float screenThicknessMargin = 1.15f;

    [Header("Sens unique")]
    [Tooltip("Ne téléporte que le joueur entré dans la zone par l'AVANT (côté de la flèche bleue Z). " +
             "Depuis l'arrière, le portail est une ouverture ordinaire.")]
    [SerializeField] private bool oneWay = true;

    [Tooltip("Masque l'écran du portail quand la caméra est derrière le plan")]
    [SerializeField] private bool hideScreenFromBehind = true;

    [HideInInspector] public bool isTeleportingDisabled = false;
    [HideInInspector] public bool isPlayerOverlapping = false;

    private Vector3 previousCameraPos;
    private Vector3 originalQuadLocalPos;
    private Vector3 originalQuadLocalScale;
    private bool enteredFromFront = true;
    private Renderer[] screenRenderers;

    private class TrackedObject
    {
        public Rigidbody rb;
        public InteractableObject interactable;
        public PortalObjectCloner cloner;
        public Vector3 previousPos;
    }

    private readonly Dictionary<Rigidbody, TrackedObject> trackedObjects = new();
    private readonly Dictionary<Rigidbody, HashSet<Collider>> overlapping = new();
    private static readonly Dictionary<Rigidbody, float> immunityTimers = new();

    private readonly List<Rigidbody> expiredImmunity = new();
    private readonly List<Rigidbody> toRemove = new();

    private void Start()
    {
        if (PersistentCoreManager.Instance != null)
        {
            player = PersistentCoreManager.Instance.PlayerTransform;
            playerCamera = PersistentCoreManager.Instance.MainCamera;
        }

        if (portalVisualQuad != null)
        {
            originalQuadLocalPos = portalVisualQuad.localPosition;
            originalQuadLocalScale = portalVisualQuad.localScale;
            screenRenderers = portalVisualQuad.GetComponentsInChildren<Renderer>(true);
            if (screenRenderers.Length == 0)
                Debug.LogWarning($"[{name}] Aucun Renderer trouvé sur portalVisualQuad : l'écran ne pourra pas être masqué par l'arrière.", this);
        }
    }

    // ---------- Triggers ----------

    private void OnTriggerEnter(Collider other) => HandleOverlap(other);
    private void OnTriggerStay(Collider other) => HandleOverlap(other);

    private bool IsPlayer(Collider c) =>
        c.CompareTag("Player") ||
        (player != null && (c.transform == player || c.transform.IsChildOf(player)));

    private void HandleOverlap(Collider other)
    {
        if (IsPlayer(other))
        {
            if (isTeleportingDisabled) return;
            if (!isPlayerOverlapping)
            {
                isPlayerOverlapping = true;
                if (playerCamera != null)
                {
                    previousCameraPos = playerCamera.transform.position;
                    // Entré par l'avant ? Sinon, pas de téléportation tant qu'il n'est pas ressorti de la zone.
                    enteredFromFront = Vector3.Dot(transform.forward, previousCameraPos - transform.position) > 0f;
                }
            }
            return;
        }

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null || rb.isKinematic) return;

        if (!overlapping.TryGetValue(rb, out var set))
            overlapping[rb] = set = new HashSet<Collider>();
        set.Add(other);

        if (trackedObjects.ContainsKey(rb)) return;
        if (immunityTimers.TryGetValue(rb, out float until) && Time.time < until) return;

        if (Vector3.Dot(transform.forward, rb.position - transform.position) <= 0f) return;

        PortalObjectCloner cloner = rb.GetComponent<PortalObjectCloner>();
        if (cloner != null && receiver != null)
            cloner.OnEnterPortalZone(transform, receiver.transform);

        trackedObjects.Add(rb, new TrackedObject
        {
            rb = rb,
            interactable = rb.GetComponent<InteractableObject>(),
            cloner = cloner,
            previousPos = rb.position
        });
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            isPlayerOverlapping = false;
            ResetScreen();
            return;
        }

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        if (overlapping.TryGetValue(rb, out var set))
        {
            set.Remove(other);
            if (set.Count > 0) return;
            overlapping.Remove(rb);
        }

        if (trackedObjects.TryGetValue(rb, out var t))
        {
            if (t.cloner != null) t.cloner.OnExitPortalZone();
            trackedObjects.Remove(rb);
        }
    }

    public void RegisterTeleportedObject(Rigidbody rb, PortalObjectCloner cloner, InteractableObject interactable, Vector3 posAtReceiver)
    {
        immunityTimers[rb] = Time.time + 0.15f;
        trackedObjects[rb] = new TrackedObject
        {
            rb = rb,
            interactable = interactable,
            cloner = cloner,
            previousPos = posAtReceiver
        };
    }

    // ---------- Boucle principale ----------

    private void LateUpdate()
    {
        expiredImmunity.Clear();
        foreach (var kvp in immunityTimers)
            if (kvp.Key == null || Time.time >= kvp.Value) expiredImmunity.Add(kvp.Key);
        foreach (Rigidbody r in expiredImmunity) immunityTimers.Remove(r);

        toRemove.Clear();
        foreach (var kvp in trackedObjects)
        {
            Rigidbody rb = kvp.Key;
            TrackedObject obj = kvp.Value;

            if (rb == null || !rb.gameObject.activeInHierarchy) { toRemove.Add(rb); continue; }

            if (immunityTimers.TryGetValue(rb, out float until) && Time.time < until)
            {
                obj.previousPos = rb.position;
                continue;
            }

            Vector3 currentPos = rb.position;
            Vector3 movement = currentPos - obj.previousPos;

            float prevDot = Vector3.Dot(transform.forward, obj.previousPos - transform.position);
            float currDot = Vector3.Dot(transform.forward, currentPos - transform.position);
            float motionDot = Vector3.Dot(transform.forward, movement);

            if (prevDot > 0f && currDot <= 0f && motionDot <= 0f && receiver != null)
            {
                TeleportObject(obj);
                toRemove.Add(rb);
                continue;
            }

            obj.previousPos = currentPos;
        }
        foreach (Rigidbody r in toRemove) trackedObjects.Remove(r);

        UpdateScreenVisibility();

        if (!isPlayerOverlapping || player == null || playerCamera == null || receiver == null)
        {
            ResetScreen();
            return;
        }

        // L'écran est mis à jour même pendant la fenêtre isTeleportingDisabled,
        // sinon il retombe à l'état "plan fin" pendant 1 à 3 frames après chaque passage.
        UpdateScreen();

        if (isTeleportingDisabled)
        {
            previousCameraPos = playerCamera.transform.position;
            return;
        }

        Vector3 currentCamPos = playerCamera.transform.position;

        bool canTeleport = !oneWay || enteredFromFront;

        if (canTeleport &&
            Vector3.Dot(transform.forward, previousCameraPos - transform.position) > 0f &&
            Vector3.Dot(transform.forward, currentCamPos - transform.position) <= 0f)
        {
            ExecutePlayerTeleport();
        }

        previousCameraPos = playerCamera.transform.position;
    }

    // ---------- Écran du portail (épaisseur dynamique) ----------

    /// <summary>
    /// Le near plane d'une caméra est un rectangle, pas un point : ses coins sont plus loin que
    /// nearClipPlane. De biais, un coin peut franchir un quad infiniment fin avant que la caméra
    /// ne franchisse le plan, et on voit alors derrière l'écran. On donne donc à l'écran une
    /// épaisseur égale à la distance caméra -> coin du near plane, placée du côté opposé à la caméra.
    /// </summary>
    public void UpdateScreen()
    {
        if (portalVisualQuad == null || playerCamera == null) return;

        Camera cam = playerCamera;
        Matrix4x4 proj = cam.projectionMatrix;
        float near = cam.nearClipPlane;

        float halfH = near / proj.m11;
        float halfW = near / proj.m00;
        float cornerDist = new Vector3(halfW, halfH, near).magnitude * screenThicknessMargin;

        float side = Vector3.Dot(transform.forward, cam.transform.position - transform.position) >= 0f ? 1f : -1f;

        // La boîte couvre [-cornerDist, +screenFrontOffset] le long de la normale, côté caméra = +
        float thickness = cornerDist + screenFrontOffset;
        float centerOffset = (screenFrontOffset - cornerDist) * 0.5f;

        portalVisualQuad.position = transform.position + transform.forward * (side * centerOffset);

        float parentScaleZ = portalVisualQuad.parent != null ? Mathf.Abs(portalVisualQuad.parent.lossyScale.z) : 1f;
        Vector3 s = originalQuadLocalScale;
        s.z = thickness / Mathf.Max(parentScaleZ, 1e-4f);
        portalVisualQuad.localScale = s;
    }

    public void UpdateScreenVisibility()
    {
        if (!hideScreenFromBehind || screenRenderers == null || playerCamera == null) return;

        bool inFront = Vector3.Dot(transform.forward, playerCamera.transform.position - transform.position) >= 0f;
        for (int i = 0; i < screenRenderers.Length; i++)
        {
            Renderer r = screenRenderers[i];
            if (r != null && r.enabled != inFront) r.enabled = inFront;
        }
    }

    // Flèche cyan = AVANT du portail (côté depuis lequel on est téléporté)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
        Gizmos.DrawWireSphere(transform.position + transform.forward * 1.5f, 0.08f);
    }

    private void ResetScreen()
    {
        if (portalVisualQuad == null) return;
        portalVisualQuad.localPosition = originalQuadLocalPos;
        portalVisualQuad.localScale = originalQuadLocalScale;
    }

    private Matrix4x4 GetPortalMatrix() => PortalMath.GetMatrix(transform, receiver.transform);

    // ---------- Téléportation d'un objet ----------

    private void TeleportObject(TrackedObject obj)
    {
        Rigidbody rb = obj.rb;
        Matrix4x4 matrix = GetPortalMatrix();

        RigidbodyInterpolation interp = rb.interpolation;
        if (interp != RigidbodyInterpolation.None)
        {
            rb.interpolation = RigidbodyInterpolation.None;
            StartCoroutine(RestoreInterpolation(rb, interp));
        }

        Vector3 targetPos = matrix.MultiplyPoint3x4(rb.position);
        Quaternion targetRot = matrix.rotation * rb.rotation;

        rb.position = targetPos;
        rb.rotation = targetRot;
        rb.transform.SetPositionAndRotation(targetPos, targetRot);
        Physics.SyncTransforms();

        rb.linearVelocity = matrix.MultiplyVector(rb.linearVelocity);
        rb.angularVelocity = matrix.MultiplyVector(rb.angularVelocity);
        rb.WakeUp();

        overlapping.Remove(rb);

        if (obj.cloner != null) obj.cloner.OnEnterPortalZone(receiver.transform, transform);

        receiver.RegisterTeleportedObject(rb, obj.cloner, obj.interactable, targetPos);

        if (obj.interactable != null && obj.interactable.IsHeld)
            obj.interactable.NotifyObjectTeleported(matrix);
    }

    private IEnumerator RestoreInterpolation(Rigidbody rb, RigidbodyInterpolation mode)
    {
        yield return new WaitForFixedUpdate();
        yield return null;
        if (rb != null) rb.interpolation = mode;
    }

    // ---------- Téléportation du joueur ----------

    private void ExecutePlayerTeleport()
    {
        Matrix4x4 m = GetPortalMatrix();
        Vector3 oldPlayerPos = player.position;

        Vector3 oldCamPos = playerCamera.transform.position;
        Quaternion oldCamRot = playerCamera.transform.rotation;

        PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
        if (interactor == null) interactor = player.GetComponentInChildren<PlayerInteractor>();
        InteractableObject heldObj = interactor != null ? interactor.HeldObject : null;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.SetPositionAndRotation(m.MultiplyPoint3x4(oldPlayerPos), m.rotation * player.rotation);
        playerCamera.transform.SetPositionAndRotation(m.MultiplyPoint3x4(oldCamPos), m.rotation * oldCamRot);

        player.BroadcastMessage("OnTargetObjectWarped", player.position - oldPlayerPos, SendMessageOptions.DontRequireReceiver);

        Rigidbody prb = player.GetComponent<Rigidbody>();
        if (prb != null)
        {
            prb.linearVelocity = m.MultiplyVector(prb.linearVelocity);
            prb.WakeUp();
        }

        if (cc != null) cc.enabled = true;

        if (heldObj != null && heldObj.IsHeld) heldObj.NotifyPlayerTeleported(m);

        receiver.isTeleportingDisabled = true;
        receiver.isPlayerOverlapping = true;
        receiver.enteredFromFront = true;
        receiver.previousCameraPos = playerCamera.transform.position;

        // Épaisseur de l'écran d'arrivée appliquée tout de suite : la caméra est à quelques mm
        // du plan du receiver, et son LateUpdate peut passer avant ou après celui-ci.
        receiver.UpdateScreen();
        // Sans ça, l'écran du receiver pourrait avoir été masqué plus tôt dans la frame
        // (avec l'ancienne position de la caméra) : un flash d'une image à chaque passage.
        receiver.UpdateScreenVisibility();

        ResetScreen();
        isPlayerOverlapping = false;
        StartCoroutine(EnableReceiverNextFrame(receiver));
    }

    private IEnumerator EnableReceiverNextFrame(PortalTeleporter targetReceiver)
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForEndOfFrame();
        if (targetReceiver != null) targetReceiver.isTeleportingDisabled = false;
    }
}