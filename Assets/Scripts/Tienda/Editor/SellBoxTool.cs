using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Crea la caja de venta junto al puesto: el jugador echa ahi los cultivos
/// cosechados y le pagan. Se corre desde "Granja > Crear caja de venta".
/// </summary>
public static class SellBoxTool
{
    private const string RutaCaja = "Assets/Gridness Studios/Lite Farm Pack/Prefabs/Crate.prefab";
    private const float EscalaCaja = 1f;
    private const float SeparacionDelPuesto = 1.2f;

    [MenuItem("Granja/Crear caja de venta")]
    public static void CrearCaja()
    {
        GameObject anterior = GameObject.Find("CajaDeVenta");
        if (anterior != null)
        {
            Debug.LogWarning("[Venta] Ya existe una 'CajaDeVenta'. Borrala antes si quieres rehacerla.", anterior);
            Selection.activeGameObject = anterior;
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCaja);
        if (prefab == null)
        {
            Debug.LogError($"[Venta] No encontre '{RutaCaja}'.");
            return;
        }

        GameObject raiz = new GameObject("CajaDeVenta");
        Undo.RegisterCreatedObjectUndo(raiz, "Crear caja de venta");

        // Se coloca al lado del puesto si existe; si no, frente a la camara
        GameObject tienda = GameObject.Find("TiendaSemillas");
        if (tienda != null)
            raiz.transform.position = tienda.transform.position + tienda.transform.right * SeparacionDelPuesto;
        else if (SceneView.lastActiveSceneView != null)
            raiz.transform.position = SceneView.lastActiveSceneView.pivot;

        GameObject caja = (GameObject)PrefabUtility.InstantiatePrefab(prefab, raiz.transform);
        caja.name = "Caja";
        caja.transform.localPosition = Vector3.zero;
        caja.transform.localScale = Vector3.one * EscalaCaja;

        // Volumen de cobro: se pone encima de la caja para atrapar lo que se suelte ahi
        Bounds b = CalcularBounds(caja);

        // La zona cuelga de la CAJA, no de la raiz: asi sigue a la caja si alguien
        // la mueve por separado. Como hermanas se desincronizaban en silencio.
        GameObject zona = new GameObject("ZonaDeCobro");
        zona.transform.SetParent(caja.transform, worldPositionStays: false);

        // La zona vive DENTRO de la caja, no la envuelve: el jugador tiene que
        // meter la verdura de verdad. Si sobresale, se vende de lejos sin que
        // el jugador entienda que paso.
        Vector3 escala = caja.transform.lossyScale;
        Vector3 tamanoLocal = new Vector3(
            b.size.x * 0.85f / Mathf.Max(escala.x, 0.0001f),
            b.size.y * 0.80f / Mathf.Max(escala.y, 0.0001f),
            b.size.z * 0.85f / Mathf.Max(escala.z, 0.0001f));

        // Centrada en el hueco de la caja, ligeramente arriba de su base
        zona.transform.position = new Vector3(b.center.x, b.center.y + b.size.y * 0.05f, b.center.z);

        BoxCollider trigger = zona.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = tamanoLocal;

        zona.AddComponent<SellBox>();

        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Selection.activeGameObject = raiz;
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log("[Venta] Caja de venta creada. Echa ahi los cultivos cosechados para cobrarlos.", raiz);
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
}
