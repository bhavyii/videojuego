using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Compone una planta por cultivo: el follaje generico del Lite Farm Pack mas la
/// verdura, para que zanahoria, cebolla y lechuga broten como mata y no como
/// una verdura suelta tirada en la tierra, igual que ya hace el tomate.
///
/// Mide los modelos dentro de Unity y los escala contra la altura de
/// Plant_Tomato_Medium, asi todos los cultivos crecen del mismo tamano.
/// Guarda los prefabs en Assets/Cultivos/Plantas y los asigna a cada CropData.
/// </summary>
public static class PlantBuilderTool
{
    private const string CarpetaCultivos = "Assets/Cultivos";
    private const string CarpetaPlantas = "Assets/Cultivos/Plantas";
    private const string CarpetaFrutos = "Assets/Cultivos/Frutos";

    private const string RutaReferencia = "Assets/Gridness Studios/Lite Farm Pack/Prefabs/Plant_Tomato_Medium.prefab";
    private const string RutaFollaje = "Assets/Gridness Studios/Lite Farm Pack/Prefabs/Plant_Medium.prefab";

    private struct PlantaDef
    {
        public readonly string cultivo;
        public readonly string rutaVerdura;
        public readonly bool llevaFollaje;
        public readonly float proporcionFollaje; // altura de la mata respecto a la referencia
        public readonly float proporcion;        // altura de la verdura respecto a la referencia
        public readonly float hundido;           // fraccion de la verdura que queda bajo tierra
        public readonly int frutos;              // 0 = se arranca la planta entera; N = N frutos sueltos

        public PlantaDef(string cultivo, string rutaVerdura, bool llevaFollaje, float proporcionFollaje, float proporcion, float hundido, int frutos = 0)
        {
            this.frutos = frutos;
            this.cultivo = cultivo;
            this.rutaVerdura = rutaVerdura;
            this.llevaFollaje = llevaFollaje;
            this.proporcionFollaje = proporcionFollaje;
            this.proporcion = proporcion;
            this.hundido = hundido;
        }
    }

    // Zanahoria y cebolla son raices, pero se entierran poco a proposito: el
    // jugador tiene que VER el fruto para saber que hay algo que cosechar.
    // Realismo perdido, legibilidad ganada.
    private static readonly PlantaDef[] Plantas =
    {
        //                                                                          mata  verdura  hundido
        // Sin mata: los modelos de zanahoria y cebolla ya traen sus propias hojas,
        // y agregarles Plant_Medium encima se veia como una rama pegada de lado.
        new PlantaDef("Zanahoria", "Assets/CozyFarmAssetPack/cozy farm/Prefabs/carrot_.prefab", false, 0f, 0.70f, 0.15f),
        new PlantaDef("Cebolla",   "Assets/ithappy/Food_Free/Prefabs/Onion_001.prefab",          false, 0f, 0.60f, 0.20f),
        new PlantaDef("Lechuga",   "Assets/LowPolyFarmLite/Prefabs/Cabbage_01.prefab",           false, 0f,    0.45f, 0.05f),
        // El tomate se compone: mata pelada mas tomates sueltos que se cortan
        // uno por uno. Por eso usa Plant_Medium y no la malla combinada.
        // Son 2 porque la mata solo tiene dos hojas donde apoyarlos.
        new PlantaDef("Tomate",    "Assets/Gridness Studios/Lite Farm Pack/Prefabs/Tomato_A.prefab", true, 0.9f, 0.22f, 0f, 2),
    };

    [MenuItem("Granja/Crear plantas compuestas")]
    public static void CrearPlantas()
    {
        GameObject refPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaReferencia);
        if (refPrefab == null)
        {
            Debug.LogError($"[Plantas] No encontre la referencia '{RutaReferencia}'.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(CarpetaPlantas))
            AssetDatabase.CreateFolder(CarpetaCultivos, "Plantas");

        if (!AssetDatabase.IsValidFolder(CarpetaFrutos))
            AssetDatabase.CreateFolder(CarpetaCultivos, "Frutos");

        float alturaRef = MedirAltura(refPrefab);
        if (alturaRef <= 0.001f)
        {
            Debug.LogError("[Plantas] La planta de referencia mide cero; no puedo escalar contra ella.");
            return;
        }

        GameObject follajePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaFollaje);
        int hechas = 0;

        foreach (PlantaDef def in Plantas)
        {
            GameObject verduraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(def.rutaVerdura);
            if (verduraPrefab == null)
            {
                Debug.LogWarning($"[Plantas] '{def.cultivo}': no encontre '{def.rutaVerdura}'. Se salta.");
                continue;
            }

            // Se rescatan las anclas del prefab anterior: son trabajo manual del
            // artista y regenerar no debe tirarlo a la basura.
            List<Vector3> anclasPrevias = LeerAnclas($"{CarpetaPlantas}/Planta_{def.cultivo}.prefab");

            CropData data = AssetDatabase.LoadAssetAtPath<CropData>($"{CarpetaCultivos}/{def.cultivo}.asset");
            if (data == null)
                Debug.LogWarning($"[Plantas] '{def.cultivo}': no encontre su CropData; la planta no sabra cuanto vale.");

            GameObject raiz = new GameObject($"Planta_{def.cultivo}");
            GameObject follaje = null;

            if (def.llevaFollaje && follajePrefab != null)
            {
                follaje = (GameObject)PrefabUtility.InstantiatePrefab(follajePrefab, raiz.transform);
                follaje.name = "Follaje";
                AjustarAltura(follaje, alturaRef * def.proporcionFollaje);
                ApoyarEnCero(follaje);
            }

            // Con frutos sueltos la planta es solo la mata. Los frutos los crea
            // PlantGrowth al terminar de crecer, como objetos independientes.
            if (def.frutos == 0)
            {
                GameObject verdura = (GameObject)PrefabUtility.InstantiatePrefab(verduraPrefab, raiz.transform);
                verdura.name = "Verdura";
                AjustarAltura(verdura, alturaRef * def.proporcion);

                // Se apoya en el suelo y luego se hunde la fraccion pedida
                ApoyarEnCero(verdura);
                float alturaVerdura = Bounds(verdura).size.y;
                verdura.transform.position -= Vector3.up * (alturaVerdura * def.hundido);

                // Las hojas brotan DESDE ARRIBA de la verdura, no la atraviesan.
                // Centradas en el mismo punto, las hojas salian clavadas en ella.
                if (follaje != null)
                {
                    float topeVerdura = Bounds(verdura).max.y;
                    follaje.transform.position += Vector3.up * (topeVerdura - Bounds(follaje).min.y);
                }
            }
            else
            {
                CrearPuntosDeFruto(raiz, follaje, def, alturaRef, anclasPrevias);
            }

            Bounds zonaRaiz = Bounds(raiz);

            BoxCollider col = raiz.AddComponent<BoxCollider>();
            col.center = zonaRaiz.center - raiz.transform.position;
            col.size = zonaRaiz.size;

            Rigidbody cuerpo = raiz.AddComponent<Rigidbody>();
            cuerpo.mass = 0.5f;

            XRGrabInteractable grabRaiz = raiz.AddComponent<XRGrabInteractable>();
            grabRaiz.colliders.Clear();
            grabRaiz.colliders.Add(col);

            HarvestableCrop cosechable = raiz.AddComponent<HarvestableCrop>();
            cosechable.crop = data;
            cosechable.esTallo = def.frutos > 0;

            string ruta = $"{CarpetaPlantas}/Planta_{def.cultivo}.prefab";
            GameObject guardada = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
            Object.DestroyImmediate(raiz);

            if (data != null)
            {
                data.grownPlantPrefab = guardada;
                data.fruitsPerPlant = def.frutos;
                data.fruitPrefab = def.frutos > 0
                    ? ConstruirFruto(def.cultivo, verduraPrefab, alturaRef * def.proporcion, data)
                    : null;

                EditorUtility.SetDirty(data);
                hechas++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Plantas] {hechas} plantas compuestas creadas en '{CarpetaPlantas}' y asignadas a sus cultivos. " +
                  "Si alguna se ve mal proporcionada, abre el prefab y mueve 'Verdura' a mano.");
    }

    /// <summary>
    /// Deja un objeto vacio por fruto dentro de la planta. PlantGrowth crea los
    /// frutos ahi. Son movibles a mano desde el prefab, que es la unica forma
    /// razonable de cuadrarlos con las hojas de cada modelo.
    /// </summary>
    private static void CrearPuntosDeFruto(GameObject raiz, GameObject follaje, PlantaDef def, float alturaRef, List<Vector3> anclasPrevias)
    {
        // Si ya habia anclas acomodadas a mano, se respetan tal cual: tanto sus
        // posiciones como cuantas eran.
        if (anclasPrevias != null && anclasPrevias.Count > 0)
        {
            for (int i = 0; i < anclasPrevias.Count; i++)
            {
                GameObject reusado = new GameObject($"PuntoFruto_{i}");
                reusado.transform.SetParent(raiz.transform);
                reusado.transform.localPosition = anclasPrevias[i];
                reusado.AddComponent<FruitAnchor>();
            }

            Debug.Log($"[Plantas] '{def.cultivo}': se conservaron {anclasPrevias.Count} anclas ya acomodadas.");
            return;
        }

        Bounds b = follaje != null
            ? Bounds(follaje)
            : new Bounds(raiz.transform.position + Vector3.up * alturaRef * 0.5f, Vector3.one * alturaRef);

        float radio = Mathf.Max(b.size.x, b.size.z) * 0.30f;
        float altura = b.min.y + b.size.y * 0.55f;

        for (int i = 0; i < def.frutos; i++)
        {
            GameObject punto = new GameObject($"PuntoFruto_{i}");
            punto.transform.SetParent(raiz.transform);
            punto.AddComponent<FruitAnchor>(); // para poder verlo y moverlo

            float angulo = (360f / def.frutos) * i * Mathf.Deg2Rad;
            punto.transform.position = new Vector3(
                b.center.x + Mathf.Cos(angulo) * radio,
                altura,
                b.center.z + Mathf.Sin(angulo) * radio);
        }
    }

    /// <summary>
    /// Lee las posiciones locales de las anclas del prefab anterior, si existe.
    /// </summary>
    private static List<Vector3> LeerAnclas(string rutaPrefab)
    {
        GameObject previo = AssetDatabase.LoadAssetAtPath<GameObject>(rutaPrefab);
        if (previo == null)
            return null;

        return previo.GetComponentsInChildren<FruitAnchor>(true)
            .OrderBy(a => a.name)
            .Select(a => a.transform.localPosition)
            .ToList();
    }

    /// <summary>
    /// Construye el prefab de un fruto suelto: agarrable, vendible e
    /// independiente del tallo. PlantGrowth crea N de estos al crecer la planta.
    /// </summary>
    private static GameObject ConstruirFruto(string cultivo, GameObject frutoPrefab, float altura, CropData data)
    {
        GameObject raiz = new GameObject($"Fruto_{cultivo}");
        raiz.transform.position = Vector3.zero;

        GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(frutoPrefab, raiz.transform);
        modelo.name = "Modelo";
        modelo.transform.localPosition = Vector3.zero;

        AjustarAltura(modelo, altura);

        // Centrar el modelo sobre el pivote, para que gire bien en la mano
        Bounds b = Bounds(modelo);
        modelo.transform.position -= b.center - raiz.transform.position;

        b = Bounds(modelo);
        BoxCollider col = raiz.AddComponent<BoxCollider>();
        col.center = b.center - raiz.transform.position;
        col.size = b.size;

        Rigidbody cuerpo = raiz.AddComponent<Rigidbody>();
        cuerpo.mass = 0.15f;

        XRGrabInteractable grab = raiz.AddComponent<XRGrabInteractable>();
        grab.colliders.Clear();
        grab.colliders.Add(col);

        HarvestableCrop cosechable = raiz.AddComponent<HarvestableCrop>();
        cosechable.crop = data;
        cosechable.esTallo = false;

        string ruta = $"{CarpetaFrutos}/Fruto_{cultivo}.prefab";
        GameObject guardado = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        Object.DestroyImmediate(raiz);

        return guardado;
    }

    private static float MedirAltura(GameObject prefab)
    {
        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        temp.transform.position = Vector3.zero;
        temp.transform.localScale = Vector3.one;

        float alto = Bounds(temp).size.y;
        Object.DestroyImmediate(temp);

        return alto;
    }

    private static void AjustarAltura(GameObject go, float alturaDeseada)
    {
        float actual = Bounds(go).size.y;
        if (actual <= 0.0001f)
            return;

        go.transform.localScale *= alturaDeseada / actual;
    }

    private static void ApoyarEnCero(GameObject go)
    {
        go.transform.position -= Vector3.up * Bounds(go).min.y;
    }

    private static Bounds Bounds(GameObject go)
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
