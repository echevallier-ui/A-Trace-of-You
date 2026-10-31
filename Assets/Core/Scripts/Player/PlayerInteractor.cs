using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum GrabControlMode
{
    HoldToGrab,  // Maintenir le bouton enfoncé
    ToggleGrab   // Clic pour activer / Clic pour désactiver
}

public class PlayerInteractor : MonoBehaviour
{
    [Header("Configuration des Modes d'Interaction")]
    [Tooltip("Mode pour les objets portables et caisses (InteractableObject)")]
    [SerializeField] private GrabControlMode objectGrabMode = GrabControlMode.ToggleGrab;

    [Tooltip("Mode pour les tiroirs, leviers et poignées physiques (PhysicalMover)")]
    [SerializeField] private GrabControlMode physicalMoverGrabMode = GrabControlMode.HoldToGrab;

    [Header("Configuration Raycast")]
    [SerializeField] private Transform playerCameraTransform;
    [SerializeField] private Transform objectGrabPointTransform;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private LayerMask portalLayer;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private float throwForce = 10f;

    [Header("Réglages Portails")]
    [SerializeField] private bool fullReachThroughPortal = true;

    [Header("UI Crosshair")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color interactableColor = Color.green;
    [SerializeField] private Color activeColor = Color.red;

    private IInteractable currentInteractable;
    private PlayerInput playerInput;

    public GrabControlMode ObjectGrabMode
    {
        get => objectGrabMode;
        set => objectGrabMode = value;
    }

    public GrabControlMode PhysicalMoverGrabMode
    {
        get => physicalMoverGrabMode;
        set => physicalMoverGrabMode = value;
    }

    public IInteractable CurrentInteractable => currentInteractable;
    public InteractableObject HeldObject => currentInteractable as InteractableObject;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null) playerInput = GetComponentInParent<PlayerInput>();
    }

    private void Start()
    {
        if (playerCameraTransform == null && PersistentCoreManager.Instance != null)
        {
            playerCameraTransform = PersistentCoreManager.Instance.MainCamera.transform;
        }
    }   

    private void OnDisable()
    {
        ForceReleaseInteraction();
    }

    private void Update()
    {
        if (playerInput != null && playerInput.currentActionMap != null && playerInput.currentActionMap.name != "Player")
        {
            ForceReleaseInteraction();
        }

        UpdateCrosshair();
    }

    public void ForceReleaseInteraction()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnInteractEnd();
            currentInteractable = null;
        }
    }

    public void OnInteract(InputValue value)
    {
        if (value.isPressed)
        {
            // --- BOUTON APPUYÉ (Clic enfoncé) ---
            if (currentInteractable != null)
            {
                // Si l'interaction actuelle est en mode Toggle, un nouveau clic relâche l'objet
                if (IsCurrentInteractionToggle())
                {
                    ForceReleaseInteraction();
                }
            }
            else
            {
                // Sinon, on tente de commencer une nouvelle interaction
                if (TryStartInteraction())
                {
                    bool isSustained = (currentInteractable is InteractableObject grabbable && grabbable.IsHeld)
                                    || (currentInteractable is PhysicalMover);

                    // Pour un événement instantané (bouton, interrupteur), on relâche immédiatement
                    if (!isSustained)
                    {
                        ForceReleaseInteraction();
                    }
                }
            }
        }
        else
        {
            // --- BOUTON RELÂCHÉ (Clic levé) ---
            if (currentInteractable != null)
            {
                // Si l'interaction nécessite un maintien (HoldToGrab), relâcher le clic stoppe l'action
                if (!IsCurrentInteractionToggle())
                {
                    ForceReleaseInteraction();
                }
            }
        }
    }

    /// <summary>
    /// Vérifie si l'interaction en cours doit utiliser le mode Toggle (Clic/Clic) ou Hold (Maintenir).
    /// </summary>
    private bool IsCurrentInteractionToggle()
    {
        if (currentInteractable is PhysicalMover)
        {
            return physicalMoverGrabMode == GrabControlMode.ToggleGrab;
        }

        if (currentInteractable is InteractableObject)
        {
            return objectGrabMode == GrabControlMode.ToggleGrab;
        }

        return false;
    }

    private bool TryStartInteraction()
    {
        if (PerformPortalRaycast(out RaycastHit hit, out Matrix4x4? portalMatrix))
        {
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                currentInteractable = interactable;
                currentInteractable.OnInteractStart(objectGrabPointTransform);

                if (currentInteractable is InteractableObject grabbable)
                {
                    if (portalMatrix.HasValue)
                    {
                        grabbable.SetPortalLink(portalMatrix.Value);
                    }
                    else
                    {
                        grabbable.ClearPortalLink();
                    }
                }
                return true;
            }
        }
        return false;
    }

    public void OnAction(InputValue value)
    {
        if (!value.isPressed) return;

        if (currentInteractable is IThrowable throwable)
        {
            throwable.Throw(throwForce);

            if (currentInteractable is InteractableObject grabbable)
            {
                if (!grabbable.IsHeld)
                {
                    currentInteractable = null;
                }
            }
            else
            {
                currentInteractable = null;
            }
            return;
        }

        if (currentInteractable == null)
        {
            if (PerformPortalRaycast(out RaycastHit hit, out _))
            {
                if (hit.collider.TryGetComponent(out InteractableObject grabbable))
                {
                    grabbable.Kick(playerCameraTransform);
                }
            }
        }
    }

    private bool PerformPortalRaycast(out RaycastHit finalHit, out Matrix4x4? portalMatrix)
    {
        portalMatrix = null;
        Vector3 origin = playerCameraTransform.position;
        Vector3 direction = playerCameraTransform.forward;

        bool hitInteractable = Physics.Raycast(origin, direction, out RaycastHit objHit, interactDistance, interactableLayer, QueryTriggerInteraction.Collide);

        Vector3 portalRayOrigin = origin - direction * 0.5f;
        bool hitPortal = Physics.Raycast(portalRayOrigin, direction, out RaycastHit portalHit, interactDistance + 0.5f, portalLayer, QueryTriggerInteraction.Collide);

        if (hitPortal)
        {
            PortalTeleporter portal = portalHit.collider.GetComponent<PortalTeleporter>();
            if (portal == null) portal = portalHit.collider.GetComponentInParent<PortalTeleporter>();

            if (portal != null && portal.receiver != null
                && PortalMath.SignedDistance(portal.transform, origin) > 0f)
            {
                Plane portalPlane = new Plane(portal.transform.forward, portal.transform.position);
                Ray cameraRay = new Ray(origin, direction);
                
                float distanceToPortal = portalPlane.Raycast(cameraRay, out float exactDistance) ? exactDistance : (portalHit.distance - 0.5f);

                if (distanceToPortal <= interactDistance && (!hitInteractable || distanceToPortal < objHit.distance))
                {
                    Matrix4x4 m = PortalMath.GetMatrix(portal.transform, portal.receiver.transform);

                    Vector3 virtualCameraOrigin = m.MultiplyPoint3x4(origin);
                    Vector3 virtualDirection = m.MultiplyVector(direction);
                    
                    float remainingDistance = fullReachThroughPortal ? interactDistance : (interactDistance - distanceToPortal);

                    if (Physics.Raycast(virtualCameraOrigin, virtualDirection, out RaycastHit subHit, remainingDistance, interactableLayer, QueryTriggerInteraction.Collide))
                    {
                        finalHit = subHit;
                        portalMatrix = m;
                        return true;
                    }
                    
                    finalHit = default;
                    return false; 
                }
            }
        }

        if (hitInteractable)
        {
            finalHit = objHit;
            return true;
        }

        finalHit = default;
        return false;
    }
    
    private void UpdateCrosshair()
    {
        if (crosshairImage == null) return;

        if (currentInteractable is InteractableObject grabbable && !grabbable.IsHeld)
        {
            currentInteractable = null;
        }

        if (currentInteractable != null)
        {
            crosshairImage.enabled = false;
            return;
        }

        crosshairImage.enabled = true;

        if (PerformPortalRaycast(out RaycastHit hit, out _))
        {
            if (hit.collider.GetComponent<IInteractable>() != null)
            {
                crosshairImage.color = interactableColor;
                return;
            }
        }

        crosshairImage.color = defaultColor;
    }
}