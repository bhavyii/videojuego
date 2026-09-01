using UnityEditor;
using UnityEngine;

/// <summary>
/// Crea los assets de CropData de una sola pasada, resolviendo los prefabs por
/// ruta. Se corre desde el menu "Granja > Crear cultivos de prueba".
/// Es idempotente: correrlo de nuevo actualiza los existentes en vez de duplicarlos,
/// asi que sirve tambien para agregar cultivos nuevos mas adelante.
/// </summary>
public static class CropSetupTool
{
    private const string CarpetaCultivos = "Assets/Cultivos";
    private const string RutaSemilla = "Assets/Gridness Studios/Lite Farm Pack/Prefabs/Seed.prefab";

    private struct CropDef
    {
        public readonly string nombre;
        public readonly int precio;
        public readonly int venta;
        public readonly string rutaPlanta;
        public readonly Color color;

        public CropDef(string nombre, int precio, int venta, string rutaPlanta, Color color)
        {
            this.nombre = nombre;
            this.precio = precio;
            this.venta = venta;
            this.rutaPlanta = rutaPlanta;
            this.color = color;
        }
    }

    // Precios de prueba: semilla a 10, cosecha a 25. El margen hace que sembrar
    // valga la pena y que el jugador pueda quedarse sin dinero si no cosecha.
    // Para agregar papa, solo se pone otra linea aqui.
    private static readonly CropDef[] Cultivos =
    {
        new CropDef("Tomate",    10, 10, "Assets/Gridness Studios/Lite Farm Pack/Prefabs/Plant_Tomato_Medium.prefab", new Color(0.85f, 0.15f, 0.15f)),
        new CropDef("Zanahoria", 10, 25, "Assets/CozyFarmAssetPack/cozy farm/Prefabs/carrot_.prefab",                  new Color(0.95f, 0.50f, 0.10f)),
        new CropDef("Cebolla",   10, 25, "Assets/ithappy/Food_Free/Prefabs/Onion_001.prefab",                          new Color(0.75f, 0.65f, 0.90f)),
        new CropDef("Lechuga",   10, 25, "Assets/LowPolyFarmLite/Prefabs/Cabbage_01.prefab",                           new Color(0.45f, 0.85f, 0.35f)),
    };

    [MenuItem("Granja/Crear cultivos de prueba")]
    public static void CrearCultivos()
    {
        if (!AssetDatabase.IsValidFolder(CarpetaCultivos))
            AssetDatabase.CreateFolder("Assets", "Cultivos");

        GameObject semilla = AssetDatabase.LoadAssetAtPath<GameObject>(RutaSemilla);
        if (semilla == null)
        {
            Debug.LogError($"[CropSetup] No se encontro la semilla en '{RutaSemilla}'. Se cancela.");
            return;
        }

        int creados = 0;
        int actualizados = 0;

        foreach (CropDef def in Cultivos)
        {
            string rutaAsset = $"{CarpetaCultivos}/{def.nombre}.asset";

            CropData data = AssetDatabase.LoadAssetAtPath<CropData>(rutaAsset);
            bool esNuevo = data == null;
            if (esNuevo)
                data = ScriptableObject.CreateInstance<CropData>();

            data.cropName = def.nombre;
            data.seedPrice = def.precio;
            data.harvestValue = def.venta;
            data.seedColor = def.color;
            data.growDuration = 3f;

            // Solo se pone la semilla si el cultivo no tiene una. Si se reasignara
            // siempre, correr este comando borraria la semilla elegida a mano o
            // con 'Granja > Semilla', que fue justo lo que paso una vez.
            if (data.seedPrefab == null)
                data.seedPrefab = semilla;

            GameObject planta = AssetDatabase.LoadAssetAtPath<GameObject>(def.rutaPlanta);
            if (planta == null)
                Debug.LogWarning($"[CropSetup] '{def.nombre}': no se encontro la planta en '{def.rutaPlanta}'. Asignala a mano en el Inspector.", data);

            data.grownPlantPrefab = planta;

            if (esNuevo)
            {
                AssetDatabase.CreateAsset(data, rutaAsset);
                creados++;
            }
            else
            {
                EditorUtility.SetDirty(data);
                actualizados++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CropSetup] Listo: {creados} cultivos creados, {actualizados} actualizados en '{CarpetaCultivos}'.");
    }
}
