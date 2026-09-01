using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Quita las semillas sueltas que quedaron regadas en la escena de las pruebas
/// originales. Esas semillas no tienen SeedItem, asi que dan tomate por respaldo
/// y ademas se saltan la tienda: son gratis.
///
/// Solo toca objetos con tag "Seed" que esten fuera de 'TiendaSemillas'.
/// Las semillas de la tienda se crean en Play, no viven en la escena.
/// </summary>
public static class FreeSeedCleanupTool
{
    [MenuItem("Granja/Listar semillas gratis en la escena")]
    public static void Listar()
    {
        List<GameObject> sueltas = Buscar();

        if (sueltas.Count == 0)
        {
            Debug.Log("[Limpieza] No hay semillas sueltas en la escena.");
            return;
        }

        string nombres = string.Join(", ", sueltas.Select(s => s.name));
        Debug.Log($"[Limpieza] {sueltas.Count} semillas sueltas: {nombres}. Usa 'Granja > Quitar semillas gratis' para borrarlas.");

        Selection.objects = sueltas.ToArray();
    }

    [MenuItem("Granja/Quitar semillas gratis de la escena")]
    public static void Quitar()
    {
        List<GameObject> sueltas = Buscar();

        if (sueltas.Count == 0)
        {
            Debug.Log("[Limpieza] No hay semillas sueltas que quitar.");
            return;
        }

        string nombres = string.Join(", ", sueltas.Select(s => s.name));

        foreach (GameObject seed in sueltas)
            Undo.DestroyObjectImmediate(seed);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log($"[Limpieza] {sueltas.Count} semillas gratis eliminadas: {nombres}. Ctrl+Z las regresa.");
    }

    private static List<GameObject> Buscar()
    {
        return Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(go => go.CompareTag("Seed"))
            .Where(go => go.GetComponentInParent<SeedDispenser>() == null)
            .Where(go => go.transform.root.name != "TiendaSemillas")
            .OrderBy(go => go.name)
            .ToList();
    }
}
