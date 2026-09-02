using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Construye un prefab de semilla agarrable a partir de un modelo cualquiera de
/// los packs, y lo asigna a todos los cultivos. Sirve para cambiar el racimo
/// original por algo mas compacto que quepa en las cajitas del puesto.
///
/// El modelo elegido se escala a un tamano comodo de agarrar en VR y se le
/// agregan Rigidbody, collider, XRGrabInteractable y el tag "Seed", que es lo
/// que la parcela busca al sembrar.
/// </summary>
public static class SeedBuilderTool
{
    private const string CarpetaCultivos = "Assets/Cultivos";
    private const string CarpetaSemillas = "Assets/Cultivos/Semillas";

    // Dimension mayor de la semilla, en metros. 12 cm agarra comodo con mando
    // y con hand tracking, y cabe en una caja chica sin pelearse con las paredes.
    private const float TamanoAgarre = 0.12f;

    private const string RutaOriginal = "Assets/Gridness Studios/Lite Farm Pack/Prefabs/Seed.prefab";

    [MenuItem("Granja/Semilla/Usar costal redondo")]
    public static void UsarCostalRedondo() =>
        Construir("Costal", "Assets/CozyFarmAssetPack/cozy farm/Prefabs/haystackround.prefab");

    [MenuItem("Granja/Semilla/Usar costal cuadrado")]
    public static void UsarCostalCuadrado() =>
        Construir("CostalCuadrado", "Assets/CozyFarmAssetPack/cozy farm/Prefabs/haystackcube.prefab");

    [MenuItem("Granja/Semilla/Usar cajita de madera")]
    public static void UsarCajita() =>
        Construir("Cajita", "Assets/CozyFarmAssetPack/cozy farm/Prefabs/woodenbox.prefab");

    [MenuItem("Granja/Semilla/Volver al racimo original")]
    public static void VolverAlOriginal()
    {
        GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(RutaOriginal);
        if (original == null)
        {
            Debug.LogError($"[Semilla] No encontre '{RutaOriginal}'.");
            return;
        }

        int n = AsignarATodos(original);
        Debug.Log($"[Semilla] {n} cultivos vuelven al racimo original. Corre 'Rehacer puesto' para reajustar las cajas.");
    }

    private static void Construir(string nombre, string rutaModelo)
    {
        GameObject modeloPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo);
        if (modeloPrefab == null)
        {
            Debug.LogError($"[Semilla] No encontre el modelo '{rutaModelo}'.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(CarpetaSemillas))
            AssetDatabase.CreateFolder(CarpetaCultivos, "Semillas");

        GameObject raiz = new GameObject($"Semilla_{nombre}");
        raiz.transform.position = Vector3.zero;
        raiz.tag = "Seed"; // la parcela busca este tag al sembrar

        GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(modeloPrefab, raiz.transform);
        modelo.name = "Modelo";
        modelo.transform.localPosition = Vector3.zero;

        // Escalar a tamano de agarre
        Bounds b = CalcularBounds(modelo);
        float mayor = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
        if (mayor > 0.0001f)
            modelo.transform.localScale *= TamanoAgarre / mayor;

        // Centrar el modelo sobre el pivote de la raiz, para que gire bien en la mano
        b = CalcularBounds(modelo);
        modelo.transform.position -= b.center - raiz.transform.position;

        b = CalcularBounds(modelo);
        BoxCollider col = raiz.AddComponent<BoxCollider>();
        col.center = b.center - raiz.transform.position;
        col.size = b.size;

        Rigidbody rb = raiz.AddComponent<Rigidbody>();
        rb.mass = 0.2f;

        raiz.AddComponent<XRGrabInteractable>();

        string ruta = $"{CarpetaSemillas}/Semilla_{nombre}.prefab";
        GameObject guardada = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        Object.DestroyImmediate(raiz);

        int n = AsignarATodos(guardada);

        Debug.Log($"[Semilla] '{nombre}' creada en '{ruta}' a {TamanoAgarre * 100f:F0} cm y asignada a {n} cultivos. " +
                  "Ahora corre 'Granja > Rehacer puesto con espacios separados' para que las cajas se reajusten.");
    }

    private static int AsignarATodos(GameObject semilla)
    {
        CropData[] cultivos = AssetDatabase.FindAssets("t:CropData", new[] { CarpetaCultivos })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<CropData>)
            .Where(c => c != null)
            .ToArray();

        foreach (CropData c in cultivos)
        {
            c.seedPrefab = semilla;
            EditorUtility.SetDirty(c);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return cultivos.Length;
    }

    private static Bounds CalcularBounds(GameObject go)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.zero);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        return b;
    }
}
