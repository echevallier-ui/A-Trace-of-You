using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerVisualizer : MonoBehaviour
{
    public enum TypeCouleur { BleuCamera, RougeDanger, VertStandard, JauneInfo }

    [Header("Configuration Visuelle")]
    [Tooltip("Choisissez un profil de couleur prédéfini pour trier vos triggers visuellement.")]
    public TypeCouleur usageTrigger = TypeCouleur.VertStandard;

    [Range(0f, 1f)]
    public float opaciteRemplissage = 0.15f;

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null || !col.enabled) return;

        // Configuration de la couleur selon l'usage
        Color couleurDeBase = ObtenirCouleurProfil(usageTrigger);
        Gizmos.color = new Color(couleurDeBase.r, couleurDeBase.g, couleurDeBase.b, opaciteRemplissage);
        Color couleurBordure = new Color(couleurDeBase.r, couleurDeBase.g, couleurDeBase.b, 0.6f);

        // Application de la matrice pour suivre fidèlement la position, rotation et échelle
        Gizmos.matrix = transform.localToWorldMatrix;

        // ÉVOLUTIF : Détection automatique de la forme du Collider
        if (col is BoxCollider box)
        {
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = couleurBordure;
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(sphere.center, sphere.radius);
            Gizmos.color = couleurBordure;
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);
        }
        else if (col is CapsuleCollider capsule)
        {
            // Les capsules n'ont pas de Gizmo natif "DrawCapsule", on dessine une approximation très propre
            DessinerApproximationCapsule(capsule, couleurBordure);
        }
        else
        {
            // FALLBACK : Si vous ajoutez un MeshCollider complexe ou une autre forme un jour
            // On dessine une boîte englobante alignée pour toujours voir qu'il y a quelque chose
            Gizmos.matrix = Matrix4x4.identity; // Le bound mondial nécessite une matrice d'identité
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.color = couleurBordure;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }

    private Color ObtenirCouleurProfil(TypeCouleur type)
    {
        return type switch
        {
            TypeCouleur.BleuCamera  => new Color(0.1f, 0.5f, 1f),
            TypeCouleur.RougeDanger  => new Color(1f, 0.2f, 0.2f),
            TypeCouleur.JauneInfo    => new Color(1f, 0.8f, 0f),
            _                        => new Color(0.2f, 1f, 0.2f) // VertStandard
        };
    }

    private void DessinerApproximationCapsule(CapsuleCollider cap, Color bordure)
    {
        // Remplissage volumique de la capsule par un cylindre/cube central
        Vector3 size = Vector3.zero;
        float radiusDouble = cap.radius * 2f;
        if (cap.direction == 0) size = new Vector3(cap.height, radiusDouble, radiusDouble); // Axe X
        if (cap.direction == 1) size = new Vector3(radiusDouble, cap.height, radiusDouble); // Axe Y
        if (cap.direction == 2) size = new Vector3(radiusDouble, radiusDouble, cap.height); // Axe Z

        Gizmos.DrawCube(cap.center, size);
        Gizmos.color = bordure;
        Gizmos.DrawWireCube(cap.center, size);
    }
}
