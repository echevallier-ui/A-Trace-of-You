using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
[RequireComponent(typeof(LayoutElement))]
public class DynamicMaxWidth : MonoBehaviour
{
    [Tooltip("Largeur maximale du texte avant le passage à la ligne.")]
    public float maxWidth = 700f;

    private TMP_Text textComponent;
    private LayoutElement layoutElement;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
        layoutElement = GetComponent<LayoutElement>();
    }

    private void Update()
    {
        RecalculateWidth();
    }

    public void RecalculateWidth()
    {
        if (textComponent == null) textComponent = GetComponent<TMP_Text>();
        if (layoutElement == null) layoutElement = GetComponent<LayoutElement>();
        if (textComponent == null || layoutElement == null) return;

        float naturalWidth = textComponent.GetPreferredValues(float.PositiveInfinity, float.PositiveInfinity).x;

        if (naturalWidth > maxWidth)
        {
            layoutElement.preferredWidth = maxWidth;
        }
        else
        {
            layoutElement.preferredWidth = -1;
        }
    }
}