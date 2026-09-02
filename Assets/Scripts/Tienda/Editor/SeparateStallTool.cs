using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Rehace el puesto con una cajita por cultivo en vez de un solo cajon compartido.
/// Cada cultivo tiene su propio espacio con su etiqueta, asi no se mezcla lo que
/// esta en venta con lo que el jugador ya compro y solto.
/// Conserva la posicion y rotacion del puesto anterior.
/// </summary>
public static class SeparateStallTool
{
    private const string CarpetaCultivos = "Assets/Cultivos";
    private const string RutaCaja = "Assets/Gridness Studios/Lite Farm Pack/Prefabs/Crate.prefab";

    // La caja ya no lleva escala fija: se calcula midiendo la semilla, para que
    // el racimo siempre quepa. Sube la holgura si aun se ven apretadas.
    private const float HolguraCaja = 2.5f;      // la caja debe ser N veces el ancho del racimo
    private const float FraccionInterior = 0.8f; // cuanto del ancho exterior es hueco aprovechable
    private const float EscalaMinima = 0.3f;
    private const float EscalaMaxima = 2.5f;
    private const float SeparacionExtra = 1.3f; // 1.0 = pegadas; mas = mas aire entre ellas
    private const float AlturaSemilla = 0.03f;  // sobre el borde de su cajita
    private const float AlturaEtiqueta = 0.22f;
    private const float AlturaSaldo = 0.70f;

    private const float TamanoTextoSaldo = 8f;
    private const float TamanoTextoPrecio = 5.5f;
    private const float EscalaTexto = 0.1f;

    [MenuItem("Granja/Rehacer puesto con espacios separados")]
    public static void RehacerPuesto()
    {
        List<CropData> cultivos = CargarCultivos();
        if (cultivos.Count == 0)
        {
            Debug.LogError($"[Puesto] No hay cultivos en '{CarpetaCultivos}'. Corre primero 'Granja > Crear cultivos de prueba'.");
            return;
        }

        GameObject prefabCaja = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCaja);
        if (prefabCaja == null)
        {
            Debug.LogError($"[Puesto] No se encontro la caja en '{RutaCaja}'.");
            return;
        }

        // Conserva donde estaba el puesto viejo antes de reemplazarlo
        GameObject anterior = GameObject.Find("TiendaSemillas");
        Vector3 posicion = anterior != null ? anterior.transform.position : PosicionFrenteACamara();
        Quaternion rotacion = anterior != null ? anterior.transform.rotation : Quaternion.identity;

        if (anterior != null)
            Undo.DestroyObjectImmediate(anterior);

        GameObject raiz = new GameObject("TiendaSemillas");
        Undo.RegisterCreatedObjectUndo(raiz, "Rehacer puesto");
        raiz.transform.SetPositionAndRotation(posicion, rotacion);

        // La caja se dimensiona contra el racimo de semillas, no a ojo
        float escalaCaja = CalcularEscalaCaja(prefabCaja, cultivos[0].seedPrefab);

        // Se mide una cajita ya escalada para saber cuanto separarlas
        float anchoCaja = MedirAncho(prefabCaja, escalaCaja);
        float separacion = anchoCaja * SeparacionExtra;
        float inicioX = -separacion * (cultivos.Count - 1) * 0.5f;

        float alturaMaxima = 0f;

        for (int i = 0; i < cultivos.Count; i++)
        {
            CropData cultivo = cultivos[i];

            GameObject casilla = new GameObject($"Casilla_{cultivo.cropName}");
            casilla.transform.SetParent(raiz.transform);
            casilla.transform.localPosition = new Vector3(inicioX + separacion * i, 0f, 0f);
            casilla.transform.localRotation = Quaternion.identity;

            GameObject caja = (GameObject)PrefabUtility.InstantiatePrefab(prefabCaja, casilla.transform);
            caja.name = "Cajita";
            caja.transform.localPosition = Vector3.zero;
            caja.transform.localScale = Vector3.one * escalaCaja;

            Bounds b = CalcularBounds(caja);
            alturaMaxima = Mathf.Max(alturaMaxima, b.max.y);

            GameObject puesto = new GameObject($"Puesto_{cultivo.cropName}");
            puesto.transform.SetParent(casilla.transform);
            puesto.transform.position = new Vector3(b.center.x, b.max.y + AlturaSemilla, b.center.z);

            SeedDispenser dispenser = puesto.AddComponent<SeedDispenser>();
            dispenser.crop = cultivo;

            CrearTexto("Etiqueta", casilla.transform,
                new Vector3(b.center.x, b.max.y + AlturaEtiqueta, b.center.z),
                $"{cultivo.cropName}\n${cultivo.seedPrice}", TamanoTextoPrecio, cultivo.seedColor);
        }

        GameObject cartel = CrearTexto("CartelSaldo", raiz.transform,
            raiz.transform.position + Vector3.up * (alturaMaxima - raiz.transform.position.y + AlturaSaldo),
            "Dinero: $----", TamanoTextoSaldo, Color.white);
        cartel.AddComponent<MoneySign>();

        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Selection.activeGameObject = raiz;
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log($"[Puesto] Rehecho con {cultivos.Count} cajitas (escala {escalaCaja:F2}): {string.Join(", ", cultivos.Select(c => c.cropName))}.", raiz);
    }

    /// <summary>
    /// Escala que necesita la caja para que el racimo de semillas quepa holgado
    /// en su interior, en vez de encimarse con las paredes y salir disparado.
    /// </summary>
    private static float CalcularEscalaCaja(GameObject prefabCaja, GameObject prefabSemilla)
    {
        if (prefabSemilla == null)
        {
            Debug.LogWarning("[Puesto] El cultivo no tiene seedPrefab; uso escala 0.6 por defecto.");
            return 0.6f;
        }

        float anchoCaja = MedirAncho(prefabCaja, 1f);
        float anchoSemilla = MedirAncho(prefabSemilla, 1f);

        if (anchoCaja <= 0.01f)
            return 0.6f;

        float interiorNecesario = anchoSemilla * HolguraCaja;
        float escala = interiorNecesario / (anchoCaja * FraccionInterior);

        Debug.Log($"[Puesto] Racimo mide {anchoSemilla:F2} m, caja {anchoCaja:F2} m -> escala {escala:F2}.");

        return Mathf.Clamp(escala, EscalaMinima, EscalaMaxima);
    }

    private static float MedirAncho(GameObject prefab, float escala)
    {
        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        temp.transform.position = Vector3.zero;
        temp.transform.localScale = Vector3.one * escala;

        Bounds b = CalcularBounds(temp);
        Object.DestroyImmediate(temp);

        return Mathf.Max(b.size.x, b.size.z);
    }

    private static GameObject CrearTexto(string nombre, Transform padre, Vector3 posicion, string texto, float tamano, Color color)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre);
        go.transform.position = posicion;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one * EscalaTexto;

        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = texto;
        tmp.fontSize = tamano;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        // Contorno oscuro: sin esto el texto se pierde sobre el pasto
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = new Color32(0, 0, 0, 255);

        go.AddComponent<Billboard>();

        RectTransform rect = go.GetComponent<RectTransform>();
        if (rect != null)
            rect.sizeDelta = new Vector2(4f, 1.5f);

        return go;
    }

    private static List<CropData> CargarCultivos()
    {
        if (!AssetDatabase.IsValidFolder(CarpetaCultivos))
            return new List<CropData>();

        return AssetDatabase.FindAssets("t:CropData", new[] { CarpetaCultivos })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<CropData>)
            .Where(c => c != null)
            .OrderBy(c => c.cropName)
            .ToList();
    }

    private static Bounds CalcularBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.one);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        return b;
    }

    private static Vector3 PosicionFrenteACamara()
    {
        SceneView vista = SceneView.lastActiveSceneView;
        return vista != null ? vista.pivot : Vector3.zero;
    }
}
