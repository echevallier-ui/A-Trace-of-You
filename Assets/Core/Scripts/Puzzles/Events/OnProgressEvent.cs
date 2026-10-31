using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public struct ProgressSaveData
{
    public int currentStepIndex;
    public bool hasSequenceError;
    public int[] slotValues;
}

public class OnProgressEvent : MonoBehaviour, ISaveable
{
    public enum LogicMode { SimpleCount, SequenceOrder, ExactValue }

    [Header("Configuration")]
    [SerializeField] private LogicMode mode = LogicMode.SimpleCount;

    // --- CONDITIONS NAUGHTYATTRIBUTES ---
    private bool IsSimpleCount => mode == LogicMode.SimpleCount;
    private bool IsSequenceOrder => mode == LogicMode.SequenceOrder;
    private bool IsExactValue => mode == LogicMode.ExactValue;

    // --- REGLAGES COMPTEUR SIMPLE ---
    [ShowIf("IsSimpleCount")]
    [SerializeField] private int targetCount = 3;

    // --- REGLAGES SÉQUENCE ORDONNÉE ---
    [ShowIf("IsSequenceOrder")]
    [SerializeField] private int[] requiredSequence = { 1, 2, 3 };

    [ShowIf("IsSequenceOrder")]
    [Tooltip("Si vrai, attend que la longueur totale de la séquence soit atteinte avant de réinitialiser en cas d'erreur.")]
    [SerializeField] private bool resetOnlyAtEnd = true;

    // --- REGLAGES VALEUR EXACTE / CADENAS ---
    [ShowIf("IsExactValue")]
    [Tooltip("La valeur totale attendue (ex: 372 pour la combinaison 3 - 7 - 2)")]
    [SerializeField] private int targetValue = 372;

    [Header("Events")]
    [SerializeField] private UnityEvent onStepValid;      // À chaque étape réussie ou changement de valeur
    [SerializeField] private UnityEvent onSuccess;        // Quand la combinaison/progression est validée
    [SerializeField] private UnityEvent onFailedOrReset;  // Si erreur ou réinitialisation

    private int currentStepIndex = 0;
    private bool hasSequenceError = false;

    // Dictionnaire de stockage des valeurs des slots (ex: Slot 0 = 300, Slot 1 = 70, Slot 2 = 2)
    private Dictionary<int, int> slotValues = new Dictionary<int, int>();

    public int CurrentStepIndex => currentStepIndex;

    public void Increment()
    {
        AddStep(0);
    }

    public void Decrement()
    {
        if (mode == LogicMode.SimpleCount && currentStepIndex > 0)
        {
            currentStepIndex--;
            onFailedOrReset?.Invoke();
        }
    }

    public void AddStep(int stepID)
    {
        if (mode == LogicMode.SimpleCount)
        {
            currentStepIndex++;
            onStepValid?.Invoke();

            if (currentStepIndex >= targetCount)
            {
                onSuccess?.Invoke();
            }
        }
        else if (mode == LogicMode.SequenceOrder)
        {
            if (requiredSequence.Length == 0) return;

            if (resetOnlyAtEnd)
            {
                if (stepID != requiredSequence[currentStepIndex])
                {
                    hasSequenceError = true;
                }

                currentStepIndex++;
                onStepValid?.Invoke();

                if (currentStepIndex >= requiredSequence.Length)
                {
                    if (!hasSequenceError)
                    {
                        onSuccess?.Invoke();
                        currentStepIndex = 0;
                    }
                    else
                    {
                        ResetProgress();
                    }
                }
            }
            else
            {
                if (stepID == requiredSequence[currentStepIndex])
                {
                    currentStepIndex++;
                    onStepValid?.Invoke();

                    if (currentStepIndex >= requiredSequence.Length)
                    {
                        onSuccess?.Invoke();
                        currentStepIndex = 0;
                    }
                }
                else
                {
                    ResetProgress();
                }
            }
        }
    }

    /// <summary>
    /// Utilisé par les barillets/molettes : définit la valeur d'un slot (ex: Slot 0 = 300, Slot 1 = 70, Slot 2 = 2)
    /// </summary>
    public void SetSlotValue(int slotIndex, int value)
    {
        if (mode != LogicMode.ExactValue) return;

        slotValues[slotIndex] = value;
        onStepValid?.Invoke();

        // Calcul de la somme totale de tous les slots
        int totalSum = 0;
        foreach (var val in slotValues.Values)
        {
            totalSum += val;
        }

        //Debug.Log($"[CADENAS] Slot {slotIndex} = {value} | Somme Actuelle : {totalSum} / Cible : {targetValue}");

        if (totalSum == targetValue)
        {
            //Debug.Log("[CADENAS] COMBINAISON EXACTE ! Déclenchement de OnSuccess.");
            onSuccess?.Invoke();
        }
    }

    public void ResetProgress()
    {
        currentStepIndex = 0;
        hasSequenceError = false;
        slotValues.Clear();
        onFailedOrReset?.Invoke();
    }

    // ---------- CONTRAT ISAVEABLE ----------

    public object SaveState()
    {
        int[] savedSlots = new int[slotValues.Count * 2];
        int i = 0;
        foreach (var kvp in slotValues)
        {
            savedSlots[i] = kvp.Key;
            savedSlots[i + 1] = kvp.Value;
            i += 2;
        }

        return new ProgressSaveData
        {
            currentStepIndex = currentStepIndex,
            hasSequenceError = hasSequenceError,
            slotValues = savedSlots
        };
    }

    public void LoadState(object state)
    {
        if (state is ProgressSaveData data)
        {
            currentStepIndex = data.currentStepIndex;
            hasSequenceError = data.hasSequenceError;
            slotValues.Clear();

            if (data.slotValues != null)
            {
                for (int i = 0; i < data.slotValues.Length; i += 2)
                {
                    slotValues[data.slotValues[i]] = data.slotValues[i + 1];
                }
            }
        }
        else if (state is string strState && strState == "DESTROYED")
        {
            Destroy(gameObject);
        }
    }
}