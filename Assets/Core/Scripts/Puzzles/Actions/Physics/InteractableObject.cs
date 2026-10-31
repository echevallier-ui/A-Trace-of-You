using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public enum DistanceSafetyMode
{
    None,
    Drop,
    Teleport
}

[System.Serializable]
public struct ObjectTransformData
{
    public Vector3 localPosition;
    public Quaternion localRotation;
}

[RequireComponent(typeof(Rigidbody))]
public class InteractableObject : MonoBehaviour, IInteractable, IThrowable, ISaveable
{
    [Header("Autorisations")]
    [SerializeField] private bool isGrabbable = true;
    [SerializeField] private bool isKickable = true;
    [SerializeField] private bool isThrowable = true;

    [Header("Réglages du Portage Physique")]
    [Tooltip("Vitesse d'attraction vers la main")]
    [SerializeField] private float followSpeed = 25f;
    [Tooltip("Vitesse d'alignement de la rotation")]
    [SerializeField] private float rotationSpeed = 15f;
    [Tooltip("Facteur d'amortissement/freinage pendant le portage")]
    [SerializeField] private float holdDrag = 10f;

    [Header("Amortissement au sol (Objet Lâché)")]
    [Tooltip("Freinage linéaire au sol (évite de glisser comme sur du verglas)")]
    [SerializeField] private float defaultLinearDamping = 1.5f;
    [Tooltip("Freinage angulaire au sol (évite de tourner indéfiniment)")]
    [SerializeField] private float defaultAngularDamping = 1f;

    [Header("Sécurité de Distance")]
    [Tooltip("Mode de sécurité si l'objet est coincé derrière un mur ou trop éloigné")]
    [SerializeField] private DistanceSafetyMode safetyMode = DistanceSafetyMode.Drop;
    [Tooltip("Distance maximale tolérée entre la main et l'objet")]
    [SerializeField] private float maxDistance = 5f;

    [Header("Réglages des forces (Action)")]
    [SerializeField] private float kickForce = 10f;
    [SerializeField] private float upwardForce = 2f;

    [Header("Événements")]
    [SerializeField] private UnityEvent onGrab;
    [SerializeField] private UnityEvent onDrop;
    [SerializeField] private UnityEvent onKick;
    [SerializeField] private UnityEvent onThrow;

    private Rigidbody rb;
    private Collider[] myColliders;
    private Collider[] playerColliders;
    private Transform grabPoint;
    private bool isHeld;

    private Matrix4x4 portalMatrix = Matrix4x4.identity;

    private bool defaultUseGravity = true;
    private float defaultDrag = 1.5f;
    private float defaultAngularDrag = 1f;

    public bool IsHeld => isHeld;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        myColliders = GetComponentsInChildren<Collider>();

        defaultUseGravity = rb.useGravity;

        // Applique un amortissement au sol s'il est à zéro dans l'Inspector
        if (rb.linearDamping <= 0f) rb.linearDamping = defaultLinearDamping;
        if (rb.angularDamping <= 0.05f) rb.angularDamping = defaultAngularDamping;

        defaultDrag = rb.linearDamping;
        defaultAngularDrag = rb.angularDamping;
    }

    // ---------- Lien portail ----------

    public void SetPortalLink(Matrix4x4 m) => portalMatrix = m;
    public void ClearPortalLink() => portalMatrix = Matrix4x4.identity;
    public void NotifyObjectTeleported(Matrix4x4 t) => portalMatrix = t * portalMatrix;
    public void NotifyPlayerTeleported(Matrix4x4 t) => portalMatrix = portalMatrix * t.inverse;

    // ---------- Portage Physique ----------

    private void FixedUpdate()
    {
        if (!isHeld || grabPoint == null) return;

        Vector3 targetGrabPos = portalMatrix.MultiplyPoint3x4(grabPoint.position);
        Quaternion targetGrabRot = portalMatrix.rotation * grabPoint.rotation;

        float currentDistance = Vector3.Distance(rb.position, targetGrabPos);

        if (safetyMode != DistanceSafetyMode.None && currentDistance >= maxDistance)
        {
            if (safetyMode == DistanceSafetyMode.Drop)
            {
                OnInteractEnd();
                return;
            }

            rb.position = targetGrabPos;
            rb.rotation = targetGrabRot;
            transform.SetPositionAndRotation(targetGrabPos, targetGrabRot);
            rb.linearVelocity = Vector3.zero;
        }

        // --- CORRECTION DAMPING EN MAIN ---
        // On calcule la vitesse cible puis on l'amortit via Lerp avec holdDrag
        Vector3 desiredVelocity = (targetGrabPos - rb.position) * followSpeed;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, desiredVelocity, Time.fixedDeltaTime * holdDrag);

        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetGrabRot, Time.fixedDeltaTime * rotationSpeed));
    }

    public void OnInteractStart(Transform interactorTransform)
    {
        if (!isGrabbable || isHeld) return;

        isHeld = true;
        grabPoint = interactorTransform;
        portalMatrix = Matrix4x4.identity;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.linearDamping = holdDrag;
        rb.angularDamping = holdDrag;

        playerColliders = interactorTransform.root.GetComponentsInChildren<Collider>();
        SetPlayerCollisions(true);

        rb.WakeUp();
        onGrab?.Invoke();
    }

    public void OnInteractEnd()
    {
        if (!isHeld) return;

        isHeld = false;
        portalMatrix = Matrix4x4.identity;

        transform.SetParent(null);
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());

        rb.isKinematic = false;
        rb.useGravity = defaultUseGravity;
        rb.linearDamping = defaultDrag;
        rb.angularDamping = defaultAngularDrag;

        SetPlayerCollisions(false);
        grabPoint = null;

        rb.WakeUp();
        onDrop?.Invoke();
    }

    public void Throw(float force)
    {
        if (!isThrowable || !isHeld) return;

        Transform origin = grabPoint;
        Matrix4x4 currentMatrix = portalMatrix;

        OnInteractEnd();

        Vector3 direction = origin != null ? origin.forward : transform.forward;
        Vector3 upward = Vector3.up;

        direction = currentMatrix.rotation * direction;
        upward = currentMatrix.rotation * upward;

        Vector3 throwDir = direction + (upward * (upwardForce / Mathf.Max(force, 0.1f)));

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(throwDir.normalized * force, ForceMode.Impulse);

        onThrow?.Invoke();
    }

    public void Kick(Transform interactorTransform)
    {
        if (!isKickable || isHeld) return;

        Vector3 direction = interactorTransform.forward + (Vector3.up * (upwardForce / Mathf.Max(kickForce, 0.1f)));
        rb.AddForce(direction.normalized * kickForce, ForceMode.Impulse);

        onKick?.Invoke();
    }

    private void SetPlayerCollisions(bool ignore)
    {
        if (playerColliders == null) return;

        foreach (Collider myCol in myColliders)
        {
            if (myCol == null) continue;
            foreach (Collider playerCol in playerColliders)
            {
                if (playerCol == null) continue;
                Physics.IgnoreCollision(myCol, playerCol, ignore);
            }
        }
    }

    // ---------- Contrat ISaveable ----------

    public object SaveState()
    {
        return new ObjectTransformData
        {
            localPosition = transform.localPosition,
            localRotation = transform.localRotation
        };
    }

    public void LoadState(object state)
    {
        if (state is string strState && strState == "DESTROYED")
        {
            Destroy(gameObject);
            return;
        }

        if (state is ObjectTransformData data)
        {
            transform.localPosition = data.localPosition;
            transform.localRotation = data.localRotation;

            if (TryGetComponent(out Rigidbody rigid))
            {
                rigid.position = transform.position;
                rigid.rotation = transform.rotation;
                rigid.linearVelocity = Vector3.zero;
                rigid.angularVelocity = Vector3.zero;
                rigid.WakeUp();
            }
        }
    }
}