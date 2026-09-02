using UnityEngine;

/// <summary>
/// Marca donde nace un fruto dentro de la planta. Se mueve a mano en el prefab
/// para cuadrarlo con las hojas del modelo. Dibuja una esfera para poder verlo,
/// porque un objeto vacio es invisible en la vista Scene.
/// </summary>
public class FruitAnchor : MonoBehaviour
{
    [Tooltip("Solo para verlo en el editor; no afecta al juego")]
    public Color colorGuia = new Color(1f, 0.25f, 0.2f);

    [Min(0.005f)] public float radioGuia = 0.03f;

    private void OnDrawGizmos()
    {
        Gizmos.color = colorGuia;
        Gizmos.DrawSphere(transform.position, radioGuia);

        Gizmos.color = new Color(colorGuia.r, colorGuia.g, colorGuia.b, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radioGuia * 1.8f);
    }
}
