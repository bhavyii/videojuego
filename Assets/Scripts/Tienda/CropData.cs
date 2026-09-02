using UnityEngine;

/// <summary>
/// Define un cultivo del juego: cuanto cuesta su semilla, que planta brota
/// al regarla y cuanto tarda. Se crea desde Assets > Create > Granja > Cultivo.
/// Agregar un cultivo nuevo no requiere tocar codigo, solo crear otro de estos.
/// </summary>
[CreateAssetMenu(fileName = "Cultivo", menuName = "Granja/Cultivo")]
public class CropData : ScriptableObject
{
    [Header("Identidad")]
    [Tooltip("Nombre que se muestra al jugador")]
    public string cropName = "Tomate";

    [Tooltip("Imagen para carteles o UI (opcional por ahora)")]
    public Sprite icon;

    [Tooltip("Color con el que se pinta la semilla, para distinguirla en el cajon")]
    public Color seedColor = Color.white;

    [Header("Economia")]
    [Tooltip("Precio de una semilla, en pesos")]
    [Min(0)] public int seedPrice = 10;

    [Tooltip("Lo que paga la caja de venta por el cultivo cosechado")]
    [Min(0)] public int harvestValue = 25;

    [Header("Prefabs")]
    [Tooltip("La semilla que el jugador agarra en la tienda y planta")]
    public GameObject seedPrefab;

    [Tooltip("La planta que brota de la tierra al regarla")]
    public GameObject grownPlantPrefab;

    [Header("Frutos sueltos")]
    [Tooltip("Fruto que se corta uno por uno. Dejalo vacio si se cosecha la planta entera")]
    public GameObject fruitPrefab;

    [Tooltip("Cuantos frutos da la planta. 0 = se arranca la planta completa")]
    [Min(0)] public int fruitsPerPlant;

    [Header("Crecimiento")]
    [Tooltip("Tiempo en segundos que tarda la animacion de crecer")]
    [Min(0.1f)] public float growDuration = 3f;
}
