using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

public enum MoveMode
{
    Position,
    Rotation,
    Both
}

public class SimpleMover : MonoBehaviour, ISaveable, IInteractable
{
    [Header("Configuration du Mouvement")]
    [SerializeField] private MoveMode moveMode = MoveMode.Both;

    private bool UsePosition => moveMode == MoveMode.Position || moveMode == MoveMode.Both;
    private bool UseRotation => moveMode == MoveMode.Rotation || moveMode == MoveMode.Both;

    [Header("Mode Cranté / Barillet (Optionnel)")]
    [SerializeField] private bool useStepping = false;

    [ShowIf("useStepping")]
    [SerializeField] private int totalSteps = 10;

    [ShowIf("useStepping")]
    [SerializeField] private int slotIndex = 0;

    [ShowIf("useStepping")]
    [SerializeField] private int valueMultiplier = 100;

    [ShowIf("useStepping")]
    [SerializeField] private UnityEvent<int, int> onStepValueChanged;

    // --- CHAMPS CONDITIONNELS EN MODE A-VERS-B ---
    [HideIf("useStepping")]
    [ShowIf("UsePosition")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0, 2f, 0);

    [ShowIf("UseRotation")]
    [Tooltip("En mode Stepping, représente la rotation par CRAN (ex: 36° sur Y)")]
    [SerializeField] private Vector3 targetRotationOffset = new Vector3(0, 90f, 0);

    [Header("Animation")]
    [SerializeField] private float duration = 0.2f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool isOpen = false;
    private int currentStep = 0;
    private bool isInitialized = false;
    private Coroutine moveCoroutine;

    public int CurrentStep => currentStep;

    private void Awake()
    {
        EnsureInitialized();
    }

    /// <summary>
    /// Garantit que la position et la rotation initiales sont capturées 
    /// avant toute opération de sauvegarde, chargement ou mouvement.
    /// </summary>
    private void EnsureInitialized()
    {
        if (isInitialized) return;

        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
        isInitialized = true;
    }

    // ---------- CONTRÔLE CRANTÉ / MOLETTE ----------

    public void StepNext()
    {
        EnsureInitialized();
        if (!useStepping) return;

        currentStep = (currentStep + 1) % totalSteps;
        AnimateStep();
    }

    public void StepPrevious()
    {
        EnsureInitialized();
        if (!useStepping) return;

        currentStep = (currentStep - 1 + totalSteps) % totalSteps;
        AnimateStep();
    }

    private void AnimateStep()
    {
        Vector3 targetPos = (UsePosition) ? startPosition + (targetOffset * currentStep) : startPosition;
        Quaternion targetRot = (UseRotation) ? startRotation * Quaternion.Euler(targetRotationOffset * currentStep) : startRotation;

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(AnimateMove(targetPos, targetRot));

        onStepValueChanged?.Invoke(slotIndex, currentStep * valueMultiplier);
    }

    // ---------- IINTERACTABLE (Clic Joueur) ----------

    public void OnInteractStart(Transform interactorTransform)
    {
        EnsureInitialized();
        if (useStepping)
        {
            StepNext();
        }
        else
        {
            ToggleMove();
        }
    }

    public void OnInteractEnd() { }

    // ---------- CONTRÔLE A-VERS-B D'ORIGINE ----------

    public void ToggleMove()
    {
        EnsureInitialized();
        if (useStepping) return;

        isOpen = !isOpen;

        Vector3 targetPos = (UsePosition && isOpen) ? startPosition + targetOffset : startPosition;
        Quaternion targetRot = (UseRotation && isOpen) ? startRotation * Quaternion.Euler(targetRotationOffset) : startRotation;

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(AnimateMove(targetPos, targetRot));
    }

    private IEnumerator AnimateMove(Vector3 destinationPos, Quaternion destinationRot)
    {
        Vector3 initialPos = transform.localPosition;
        Quaternion initialRot = transform.localRotation;
        float time = 0;

        while (time < duration)
        {
            float t = Mathf.SmoothStep(0, 1, time / duration);

            if (UsePosition) transform.localPosition = Vector3.Lerp(initialPos, destinationPos, t);
            if (UseRotation) transform.localRotation = Quaternion.Slerp(initialRot, destinationRot, t);

            time += Time.deltaTime;
            yield return null;
        }

        if (UsePosition) transform.localPosition = destinationPos;
        if (UseRotation) transform.localRotation = destinationRot;
    }

    public void ResetToStart()
    {
        EnsureInitialized();
        isOpen = false;
        currentStep = 0;

        if (moveCoroutine != null) StopCoroutine(moveCoroutine);
        moveCoroutine = StartCoroutine(AnimateMove(startPosition, startRotation));
    }

    // ---------- CONTRAT ISAVEABLE ----------

    public object SaveState()
    {
        EnsureInitialized();
        if (useStepping) return currentStep;
        return isOpen;
    }

    public void LoadState(object state)
    {
        EnsureInitialized();

        if (useStepping && state is int savedStep)
        {
            currentStep = savedStep;

            if (moveCoroutine != null) StopCoroutine(moveCoroutine);

            // Applique directement la position/rotation exacte de départ + décalage du cran
            Vector3 targetPos = (UsePosition) ? startPosition + (targetOffset * currentStep) : startPosition;
            Quaternion targetRot = (UseRotation) ? startRotation * Quaternion.Euler(targetRotationOffset * currentStep) : startRotation;

            if (UsePosition) transform.localPosition = targetPos;
            if (UseRotation) transform.localRotation = targetRot;

            onStepValueChanged?.Invoke(slotIndex, currentStep * valueMultiplier);
        }
        else if (!useStepping && state is bool savedIsOpen)
        {
            isOpen = savedIsOpen;

            if (moveCoroutine != null) StopCoroutine(moveCoroutine);

            Vector3 targetPos = (UsePosition && isOpen) ? startPosition + targetOffset : startPosition;
            Quaternion targetRot = (UseRotation && isOpen) ? startRotation * Quaternion.Euler(targetRotationOffset) : startRotation;

            if (UsePosition) transform.localPosition = targetPos;
            if (UseRotation) transform.localRotation = targetRot;
        }
        else if (state is string strState && strState == "DESTROYED")
        {
            Destroy(gameObject);
        }
    }
}