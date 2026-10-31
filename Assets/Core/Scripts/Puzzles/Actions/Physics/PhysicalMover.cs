using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public struct PhysicalMoverSaveData
{
    public float currentValue;
    public bool isLocked;
}

[RequireComponent(typeof(Rigidbody))]
public class PhysicalMover : MonoBehaviour, IInteractable, ISaveable
{
    public enum MovementType { Translation, Rotation }

    [Header("Type de Mouvement")]
    [SerializeField] private MovementType movementType = MovementType.Translation;

    private bool IsTranslation => movementType == MovementType.Translation;
    private bool IsRotation => movementType == MovementType.Rotation;

    [Header("Lissage & Suivi")]
    [Tooltip("Vitesse de suivi/lissage du mouvement (évite les tremblements de caméra)")]
    [SerializeField] private float smoothSpeed = 20f;

    [Header("Axe local")]
    [Tooltip("Axe de glissement (Translation) ou de pivotement (Rotation)")]
    [SerializeField] private Vector3 localAxis = Vector3.forward;

    [Header("Limites Translation (mètres)")]
    [ShowIf("IsTranslation")]
    [SerializeField] private float minDistance = 0f;

    [ShowIf("IsTranslation")]
    [SerializeField] private float maxDistance = 0.5f;

    [Header("Limites Rotation (degrés)")]
    [ShowIf("IsRotation")]
    [SerializeField] private float minAngle = 0f;

    [ShowIf("IsRotation")]
    [SerializeField] private float maxAngle = 90f;

    [Header("Verrouillage / Serrure")]
    [SerializeField] private bool isLocked = false;

    [Tooltip("Événement déclenché lorsque le joueur tente de manipuler l'objet alors qu'il est verrouillé")]
    [SerializeField] private UnityEvent onLockedInteract;

    private Rigidbody rb;
    private Transform grabPoint;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private bool isInitialized = false;

    private bool isGrabbed = false;
    private float grabOffset = 0f;
    private float currentSmoothValue = 0f;

    public bool IsLocked => isLocked;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (isInitialized) return;

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.freezeRotation = true;
            // Active l'interpolation pour un rendu visuel ultra-fluide entre les frames physiques
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        isInitialized = true;
    }

    private Vector3 GetWorldAxis()
    {
        return transform.parent != null 
            ? transform.parent.TransformDirection(localAxis.normalized) 
            : initialLocalRotation * localAxis.normalized;
    }

    private Vector3 GetOriginPosition()
    {
        return transform.parent != null 
            ? transform.parent.TransformPoint(initialLocalPosition) 
            : initialLocalPosition;
    }

    private Quaternion GetOriginRotation()
    {
        return transform.parent != null 
            ? transform.parent.rotation * initialLocalRotation 
            : initialLocalRotation;
    }

    // ---------- CONTRÔLES DU VERROUILLAGE ----------

    public void Unlock()
    {
        isLocked = false;
    }

    public void Lock()
    {
        isLocked = true;
        OnInteractEnd();
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
        if (isLocked) OnInteractEnd();
    }

    // ---------- INTERACTION JOUEUR ----------

    public void OnInteractStart(Transform interactorTransform)
    {
        EnsureInitialized();

        if (isLocked)
        {
            onLockedInteract?.Invoke();
            if (isLocked) return; // Si toujours verrouillé après l'événement, on stoppe
        }

        grabPoint = interactorTransform;
        isGrabbed = true;

        currentSmoothValue = GetCurrentValue();
        float grabVal = GetGrabValue();
        grabOffset = grabVal - currentSmoothValue;
    }

    public void OnInteractEnd()
    {
        isGrabbed = false;
        grabPoint = null;
    }

    private void FixedUpdate()
    {
        if (isLocked || !isGrabbed || grabPoint == null) return;

        float targetValue = GetGrabValue() - grabOffset;

        // Lissage de la valeur pour supprimer tout tremblement
        currentSmoothValue = Mathf.Lerp(currentSmoothValue, targetValue, Time.fixedDeltaTime * smoothSpeed);
        ApplyValue(currentSmoothValue);
    }

    // ---------- CALCULS ET APPLICATION ----------

    public float GetCurrentValue()
    {
        Vector3 worldAxis = GetWorldAxis();

        if (movementType == MovementType.Translation)
        {
            return Vector3.Dot(transform.position - GetOriginPosition(), worldAxis);
        }
        else // Rotation
        {
            Quaternion relativeRot = transform.localRotation * Quaternion.Inverse(initialLocalRotation);
            relativeRot.ToAngleAxis(out float angle, out Vector3 axis);
            return Vector3.Dot(axis, localAxis.normalized) < 0 ? -angle : angle;
        }
    }

    private float GetGrabValue()
    {
        Vector3 worldAxis = GetWorldAxis();

        if (movementType == MovementType.Translation)
        {
            return Vector3.Dot(grabPoint.position - GetOriginPosition(), worldAxis);
        }
        else // Rotation
        {
            Vector3 dirToGrab = Vector3.ProjectOnPlane(grabPoint.position - transform.position, worldAxis);
            // Direction de référence FIXE basée sur l'orientation d'origine (évite la boucle de rétroaction)
            Vector3 staticForward = GetOriginRotation() * Vector3.forward;
            Vector3 referenceDir = Vector3.ProjectOnPlane(staticForward, worldAxis);
            return Vector3.SignedAngle(referenceDir, dirToGrab, worldAxis);
        }
    }

    private void ApplyValue(float rawValue)
    {
        if (movementType == MovementType.Translation)
        {
            float clampedDist = Mathf.Clamp(rawValue, minDistance, maxDistance);
            Vector3 targetPos = GetOriginPosition() + (GetWorldAxis() * clampedDist);

            if (rb != null)
            {
                rb.MovePosition(targetPos);
            }
            else
            {
                transform.position = targetPos;
            }
        }
        else // Rotation
        {
            float clampedAngle = Mathf.Clamp(rawValue, minAngle, maxAngle);
            Quaternion targetRot = GetOriginRotation() * Quaternion.AngleAxis(clampedAngle, localAxis.normalized);

            if (rb != null)
            {
                rb.MoveRotation(targetRot);
            }
            else
            {
                transform.rotation = targetRot;
            }
        }
    }

    // ---------- CONTRAT ISAVEABLE ----------

    public object SaveState()
    {
        EnsureInitialized();
        return new PhysicalMoverSaveData
        {
            currentValue = GetCurrentValue(),
            isLocked = isLocked
        };
    }

    public void LoadState(object state)
    {
        EnsureInitialized();

        if (state is PhysicalMoverSaveData data)
        {
            isLocked = data.isLocked;
            currentSmoothValue = data.currentValue;
            ApplyValue(data.currentValue);
        }
        else if (state is float savedValue)
        {
            currentSmoothValue = savedValue;
            ApplyValue(savedValue);
        }
        else if (state is string strState && strState == "DESTROYED")
        {
            Destroy(gameObject);
        }
    }
}