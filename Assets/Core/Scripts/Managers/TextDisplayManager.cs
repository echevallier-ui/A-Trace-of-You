using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public enum ReadingSpeed
{
    Slow,     // Lente (~12 Caractères/sec)
    Standard, // Standard (~16 Caractères/sec)
    Fast,      // Rapide (~25 Caractères/sec)
    VeryFast  // Très rapide (~40 Caractères/sec)
}

public class TextDisplayManager : MonoBehaviour
{
    public static TextDisplayManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_Text textComponent;
    [SerializeField] private GameObject textPanel;
    [SerializeField] private GameObject continuePrompt;

    [Header("Text Duration")]
    [Tooltip("Permet de forcer une vitesse spécifique si besoin, sinon utilise la vitesse de l'enum.")]
    [SerializeField] private bool useAutomaticCalculation = true;    
    [SerializeField] private float defaultDuration = 3f;
    [Tooltip("Vitesse de lecture appliquée quand RequireClickToAdvance est décoché.")]
    [SerializeField] private ReadingSpeed readingSpeed = ReadingSpeed.Standard;
    [Tooltip("Si vrai, ajoute du temps d'affichage supplémentaire pour les ponctuations (., !? ;).")]
    [SerializeField] private bool includePunctuationPauses = true;

    [Header("Maximum Lines Per Panel")]
    [Tooltip("Nombre maximal de lignes affichées par bulle de texte.")]
    [SerializeField] private int maxLines = 4;

    [Header("Input System (New Input System)")]
    [Tooltip("Le composant PlayerInput situé sur votre Joueur.")]
    [SerializeField] private PlayerInput playerInputComponent;
    [SerializeField] private string playerMapName = "Player";
    [SerializeField] private string uiMapName = "UI";

    [Tooltip("Si vrai, attend un clic/validation pour passer la bulle et bloque le joueur. Si faux, le texte défile au chrono et le joueur reste libre.")]
    [SerializeField] private bool requireClickToAdvance = true;

    private DynamicMaxWidth dynamicMaxWidth;
    private Coroutine displayCoroutine;
    private InputAction advanceAction;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (textComponent != null)
        {
            dynamicMaxWidth = textComponent.GetComponent<DynamicMaxWidth>();
        }

        HidePanel();
    }

    private void Start()
    {
        if (playerInputComponent == null)
            playerInputComponent = FindFirstObjectByType<PlayerInput>();

        if (playerInputComponent != null)
        {
            advanceAction = playerInputComponent.actions.FindAction("Click");
            if (advanceAction == null)
                advanceAction = playerInputComponent.actions.FindAction("Submit");
        }
    }

    public void ShowMessage(string message)
    {
        ShowMessageWithDuration(message, defaultDuration);
    }

    public void ShowMessageWithDuration(string message, float duration)
    {
        if (textComponent == null) return;

        if (displayCoroutine != null) 
            StopCoroutine(displayCoroutine);

        displayCoroutine = StartCoroutine(DisplaySequence(message, duration));
    }

    private IEnumerator DisplaySequence(string fullText, float durationPerBubble)
{
    if (requireClickToAdvance)
    {
        EnableUIInputMap(true);
    }

    if (textPanel != null)
        textPanel.SetActive(true);

    string remainingText = fullText;

    while (!string.IsNullOrEmpty(remainingText))
    {
        textComponent.text = remainingText;
        ForceLayoutCalculation();

        string currentBubbleText;

        if (textComponent.textInfo.lineCount > maxLines)
        {
            int lastCharIndex = textComponent.textInfo.lineInfo[maxLines - 1].lastCharacterIndex;
            
            int searchStartIndex = Mathf.Clamp(lastCharIndex - 4, 0, remainingText.Length - 1);

            int spaceIndex = remainingText.LastIndexOf(' ', searchStartIndex);

            int cutIndex = (spaceIndex > 0) ? spaceIndex : searchStartIndex;

            currentBubbleText = remainingText.Substring(0, cutIndex).TrimEnd() + " ...";
            remainingText = "... " + remainingText.Substring(cutIndex).TrimStart();

            textComponent.text = currentBubbleText;
            ForceLayoutCalculation();
        }
        else
        {
            currentBubbleText = remainingText;
            remainingText = string.Empty;
        }

        yield return null;

        if (requireClickToAdvance && advanceAction != null)
        {
            bool hasMoreText = !string.IsNullOrEmpty(remainingText);
            if (continuePrompt != null) 
                continuePrompt.SetActive(hasMoreText);

            yield return new WaitUntil(() => advanceAction.triggered);

            if (continuePrompt != null) 
                continuePrompt.SetActive(false);
        }
        else
        {
            float waitTime = useAutomaticCalculation ? CalculateReadingTime(currentBubbleText) : durationPerBubble;
            yield return new WaitForSeconds(waitTime);
        }
    }

    HidePanel();
}

    private void ForceLayoutCalculation()
    {
        if (dynamicMaxWidth == null && textComponent != null)
            dynamicMaxWidth = textComponent.GetComponent<DynamicMaxWidth>();

        if (dynamicMaxWidth != null)
            dynamicMaxWidth.RecalculateWidth();

        if (textPanel != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(textPanel.GetComponent<RectTransform>());

        Canvas.ForceUpdateCanvases();
        textComponent.ForceMeshUpdate();
    }

    public void HidePanel()
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
            displayCoroutine = null;
        }

        if (textComponent != null)
            textComponent.text = string.Empty;

        if (textPanel != null)
            textPanel.SetActive(false);

        if (continuePrompt != null) 
        continuePrompt.SetActive(false);

        EnableUIInputMap(false);
    }

    private void EnableUIInputMap(bool enableUI)
    {
        if (playerInputComponent == null) return;

        string targetMap = enableUI ? uiMapName : playerMapName;
        playerInputComponent.SwitchCurrentActionMap(targetMap);
    }

    public float CalculateReadingTime(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0f;

        float baseDuration;
        float timePerChar;
        float sentencePause; 
        float commaPause;    

        switch (readingSpeed)
        {
            case ReadingSpeed.Slow:
                baseDuration = 1.5f;
                timePerChar = 0.08f;
                sentencePause = 0.5f;
                commaPause = 0.3f;
                break;

            case ReadingSpeed.Fast:
                baseDuration = 0.8f;
                timePerChar = 0.04f;
                sentencePause = 0.25f;
                commaPause = 0.15f;
                break;

            case ReadingSpeed.VeryFast:
                baseDuration = 0.4f;
                timePerChar = 0.025f;
                sentencePause = 0.12f;
                commaPause = 0.06f;
                break;

            case ReadingSpeed.Standard:
            default:
                baseDuration = 1.0f;
                timePerChar = 0.06f;
                sentencePause = 0.4f;
                commaPause = 0.2f;
                break;
        }

        float calculatedDuration = baseDuration + (text.Length * timePerChar);

        if (includePunctuationPauses)
        {
            foreach (char c in text)
            {
                if (c == '.' || c == '!' || c == '?')
                    calculatedDuration += sentencePause;
                else if (c == ',' || c == ';')
                    calculatedDuration += commaPause;
            }
        }

        return calculatedDuration;
    }
}