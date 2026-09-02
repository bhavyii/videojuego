using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Baja el puesto hasta apoyarlo en el suelo, sin tener que arrastrarlo a ojo.
/// Lanza un rayo hacia abajo desde el puesto e ignora sus propios colliders.
/// </summary>
public static class GroundSnapTool
{
    private const float AlturaBusqueda = 50f;
    private const float DistanciaMaxima = 200f;

    [MenuItem("Granja/Bajar el puesto al suelo")]
    public static void BajarAlSuelo()
    {
        GameObject tienda = GameObject.Find("TiendaSemillas");
        if (tienda == null)
        {
            Debug.LogError("[Suelo] No encontre 'TiendaSemillas' en la escena.");
            return;
        }

        Renderer[] renderers = tienda.GetComponentsInChildren<Renderer>()
            .Where(r => r.GetComponent<TMPro.TextMeshPro>() == null)
            .ToArray();

        if (renderers.Length == 0)
        {
            Debug.LogError("[Suelo] El puesto no tiene nada visible que apoyar.", tienda);
            return;
        }

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        Vector3 origen = new Vector3(b.center.x, b.center.y + AlturaBusqueda, b.center.z);

        // Se ignoran los colliders del propio puesto: si no, el rayo se pega a sus cajitas
        RaycastHit[] hits = Physics.RaycastAll(origen, Vector3.down, DistanciaMaxima);
        RaycastHit? suelo = hits
            .Where(h => !h.collider.transform.IsChildOf(tienda.transform))
            .OrderBy(h => h.distance)
            .Select(h => (RaycastHit?)h)
            .FirstOrDefault();

        if (suelo == null)
        {
            Debug.LogError("[Suelo] No encontre piso debajo del puesto. Muevelo sobre el terreno y vuelve a intentar.", tienda);
            return;
        }

        Undo.RecordObject(tienda.transform, "Bajar puesto al suelo");

        float caida = b.min.y - suelo.Value.point.y;
        tienda.transform.position -= Vector3.up * caida;

        EditorSceneManager.MarkSceneDirty(tienda.scene);
        Selection.activeGameObject = tienda;

        Debug.Log($"[Suelo] Puesto bajado {caida:F2} m; apoyado sobre '{suelo.Value.collider.name}'.", tienda);
    }
}
