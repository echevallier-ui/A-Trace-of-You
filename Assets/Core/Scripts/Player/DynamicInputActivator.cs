using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public enum InputActivationMode
{
    Toggle,     // Alterne entre actif / inactif à chaque pression
    Activate,   // Active uniquement (SetActive true)
    Deactivate  // Désactive uniquement (SetActive false)
}

public class DynamicInputActivator : MonoBehaviour
{
    [Header("Configuration de l'Input")]
    [Tooltip("Nom de l'action dans l'Input Action Asset (ex: 'Flashlight', 'Map', 'Journal')")]
    [SerializeField] private string actionName;

    [Tooltip("OU référence directe à une action d'un Input Action Asset (Optionnel)")]
    [SerializeField] private InputActionReference actionReference;

    [Header("Cible & Comportement")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private InputActivationMode mode = InputActivationMode.Toggle;

    [Header("Événements Optionnels")]
    [SerializeField] private UnityEvent onActionTriggered;

    private PlayerInput playerInput;
    private InputAction boundAction;

    private void OnEnable()
    {
        BindAction();
    }

    private void OnDisable()
    {
        UnbindAction();
    }

    public void BindAction()
    {
        UnbindAction();

        if (actionReference != null)
        {
            boundAction = actionReference.action;
        }

        else if (!string.IsNullOrEmpty(actionName))
        {
            if (PersistentCoreManager.Instance != null && PersistentCoreManager.Instance.PlayerGameObject != null)
            {
                playerInput = PersistentCoreManager.Instance.PlayerGameObject.GetComponent<PlayerInput>();
                
                if (playerInput == null)
                {
                    playerInput = PersistentCoreManager.Instance.PlayerGameObject.GetComponentInChildren<PlayerInput>();
                }
            }

            if (playerInput != null && playerInput.actions != null)
            {
                boundAction = playerInput.actions.FindAction(actionName);
            }
        }

        // 3. Activation de l'écoute de la touche
        if (boundAction != null)
        {
            boundAction.performed += OnInputPerformed;
            boundAction.Enable();
        }
    }

    private void UnbindAction()
    {
        if (boundAction != null)
        {
            boundAction.performed -= OnInputPerformed;
            boundAction = null;
        }
    }

    private void OnInputPerformed(InputAction.CallbackContext context)
    {
        ExecuteActivation();
    }

    public void ExecuteActivation()
    {
        if (targetObject != null)
        {
            switch (mode)
            {
                case InputActivationMode.Toggle:
                    targetObject.SetActive(!targetObject.activeSelf);
                    break;
                case InputActivationMode.Activate:
                    targetObject.SetActive(true);
                    break;
                case InputActivationMode.Deactivate:
                    targetObject.SetActive(false);
                    break;
            }
        }

        onActionTriggered?.Invoke();
    }

    public void SetActionName(string newActionName)
    {
        actionName = newActionName;
        if (gameObject.activeInHierarchy)
        {
            BindAction();
        }
    }
}