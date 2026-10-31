using UnityEngine;

/// <summary>
/// Maths partagées par tous les scripts de portail : un seul endroit pour la matrice de passage.
/// </summary>
public static class PortalMath
{
    private static readonly Matrix4x4 Flip =
        Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 180f, 0f), Vector3.one);

    /// <summary>
    /// Matrice qui transforme un point/une rotation de l'espace du portail d'entrée
    /// vers celui du portail de sortie (avec le demi-tour de 180° autour de Y).
    /// L'échelle des portails est ignorée volontairement : seuls position et rotation comptent,
    /// ce qui évite toute déformation si les deux portails n'ont pas la même échelle.
    /// </summary>
    public static Matrix4x4 GetMatrix(Transform entry, Transform exit)
    {
        Matrix4x4 exitToWorld = Matrix4x4.TRS(exit.position, exit.rotation, Vector3.one);
        Matrix4x4 worldToEntry = Matrix4x4.TRS(entry.position, entry.rotation, Vector3.one).inverse;
        return exitToWorld * Flip * worldToEntry;
    }

    /// <summary>
    /// Distance signée d'un point au plan du portail : positive devant (côté forward), négative derrière.
    /// </summary>
    public static float SignedDistance(Transform portal, Vector3 point)
    {
        return Vector3.Dot(portal.forward, point - portal.position);
    }
}