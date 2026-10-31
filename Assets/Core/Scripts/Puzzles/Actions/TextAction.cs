using UnityEngine;

public class TextAction : MonoBehaviour
{
    [Header("Message Configuration")]
    [TextArea(2, 5)]
    [SerializeField] private string message = "Insérer un texte ici...";
    [SerializeField] private float displayDuration = 3f;

    public void TriggerText()
    {
        if (TextDisplayManager.Instance != null)
        {
            TextDisplayManager.Instance.ShowMessageWithDuration(message, displayDuration);
        }
    }
}