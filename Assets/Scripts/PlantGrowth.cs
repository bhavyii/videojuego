using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PlantGrowth : MonoBehaviour
{
    [Header("Configuracion de Planta")]
    [Tooltip("Planta que crece si la semilla no trae cultivo asignado (respaldo)")]
    public GameObject plantPrefab;

    [Tooltip("Punto donde brotará")]
    public Transform spawnPoint;

    [Tooltip("Tiempo en segundos que tarda la animación (respaldo)")]
    public float growDuration = 3f;

    [Header("Audio 3D")]
    [Tooltip("AudioSource para los efectos de la tierra")]
    public AudioSource plantAudioSource;

    [Tooltip("Sonido al sembrar la semilla")]
    public AudioClip seedPlantedClip;

    [Tooltip("Sonido al finalizar el crecimiento")]
    public AudioClip plantGrownClip;

    private XRSocketInteractor socket;
    private bool hasSeed = false;
    private bool isWatered = false;

    // Cultivo que se sembro aqui. Lo trae la semilla, no la parcela.
    private CropData plantedCrop;

    // Frutos que aun no se cortan. El tallo no sale hasta vaciarla.
    private readonly List<HarvestableCrop> frutosPendientes = new List<HarvestableCrop>();

    private void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();

        if (plantAudioSource == null)
            plantAudioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        socket.selectEntered.AddListener(OnPlantSeeded);
    }

    private void OnDisable()
    {
        socket.selectEntered.RemoveListener(OnPlantSeeded);
    }

    private void OnPlantSeeded(SelectEnterEventArgs args)
    {
        if (args.interactableObject.transform.CompareTag("Seed") && !hasSeed)
        {
            GameObject seedObj = args.interactableObject.transform.gameObject;

            // La semilla decide que crece. Si no trae datos se usa el respaldo.
            SeedItem item = seedObj.GetComponent<SeedItem>();
            plantedCrop = item != null ? item.crop : null;

            Debug.Log($"[PlantGrowth] '{name}': sembrado '{(plantedCrop != null ? plantedCrop.cropName : "SIN CULTIVO (respaldo)")}'. Ahora riega aqui.", this);

            // Audio al plantar la semilla
            if (plantAudioSource != null && seedPlantedClip != null)
                plantAudioSource.PlayOneShot(seedPlantedClip);

            Destroy(seedObj);

            hasSeed = true;
            socket.enabled = false;
        }
    }

    // Detecta el choque de las particulas de agua contra el collider de la tierra
    private void OnParticleCollision(GameObject other)
    {
        // Ojo: esto se dispara en cada fotograma mientras cae el agua.
        // Nada de logs aqui salvo dentro del if, que corre una sola vez.
        if (hasSeed && !isWatered)
        {
            isWatered = true;
            Debug.Log($"[PlantGrowth] '{name}': regada con '{other.name}'.", this);
            StartCoroutine(GrowRoutine());
        }
    }

    /// <summary>Deja la parcela lista para volver a sembrar.</summary>
    private void LiberarParcela()
    {
        hasSeed = false;
        isWatered = false;
        plantedCrop = null;

        if (socket != null)
            socket.enabled = true;

        Debug.Log($"[PlantGrowth] '{name}': parcela libre, se puede volver a sembrar.", this);
    }

    private IEnumerator GrowRoutine()
    {
        GameObject prefabToGrow = plantedCrop != null && plantedCrop.grownPlantPrefab != null
            ? plantedCrop.grownPlantPrefab
            : plantPrefab;

        float duration = plantedCrop != null ? plantedCrop.growDuration : growDuration;

        if (prefabToGrow == null)
        {
            Debug.LogError($"[PlantGrowth] '{name}' no tiene ninguna planta que hacer crecer.", this);
            yield break;
        }

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        Debug.Log($"[PlantGrowth] '{name}': creciendo '{prefabToGrow.name}' en {spawnPos} durante {duration}s.", this);

        GameObject plant = Instantiate(prefabToGrow, spawnPos, spawnRot);

        HarvestableCrop cosechable = plant.GetComponent<HarvestableCrop>();
        if (cosechable != null)
            cosechable.Cosechado += LiberarParcela;
        else
            Debug.LogWarning($"[PlantGrowth] '{name}': la planta no es cosechable, la parcela quedara ocupada.", this);

        Vector3 finalScale = plant.transform.localScale;
        plant.transform.localScale = Vector3.zero;

        // INICIO DE CRECIMIENTO: ajustar tono/velocidad del clip a la duración exacta
        if (plantAudioSource != null && plantGrownClip != null)
        {
            plantAudioSource.clip = plantGrownClip;
            // Adapta la velocidad de reproducción para que dure exactamente lo que dura la animación
            plantAudioSource.pitch = plantGrownClip.length / duration;
            plantAudioSource.Play();
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            plant.transform.localScale = Vector3.Lerp(Vector3.zero, finalScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        plant.transform.localScale = finalScale;

        // FIN DE CRECIMIENTO: apagar el sonido y restaurar el pitch
        if (plantAudioSource != null && plantAudioSource.isPlaying)
        {
            plantAudioSource.Stop();
            plantAudioSource.pitch = 1f;
        }

        if (plantedCrop != null && plantedCrop.fruitsPerPlant > 0 && plantedCrop.fruitPrefab != null)
            ColgarFrutos(plant, plantedCrop);
    }
    /// <summary>
    /// Reparte los frutos en circulo alrededor de la planta ya crecida.
    /// Quedan sueltos en el mundo: cortar uno no afecta a los demas ni al tallo.
    /// </summary>
    private void ColgarFrutos(GameObject planta, CropData cultivo)
    {
        // Los puntos que dejo la herramienta mandan; son los que se pueden mover
        // a mano en el prefab para cuadrarlos con las hojas de cada modelo.
        List<Transform> puntos = planta.GetComponentsInChildren<FruitAnchor>()
            .OrderBy(a => a.name)
            .Select(a => a.transform)
            .ToList();

        frutosPendientes.Clear();

        // Los puntos mandan sobre el numero configurado: si la planta trae
        // anclas, sale un fruto por ancla. Asi se agregan o quitan frutos
        // borrando puntos en el prefab, sin tocar codigo ni perder posiciones.
        int cantidad = puntos.Count > 0 ? puntos.Count : cultivo.fruitsPerPlant;

        for (int i = 0; i < cantidad; i++)
        {
            Vector3 pos = i < puntos.Count
                ? puntos[i].position
                : PosicionDeRespaldo(planta, cultivo, i);

            GameObject fruto = Instantiate(cultivo.fruitPrefab, pos, Quaternion.identity);
            fruto.name = $"Fruto_{cultivo.cropName}_{i}";

            HarvestableCrop datos = fruto.GetComponent<HarvestableCrop>();
            if (datos != null)
            {
                datos.crop = cultivo;
                frutosPendientes.Add(datos);
            }
        }

        BloquearTalloHastaVaciar(planta);

        Debug.Log($"[PlantGrowth] '{name}': {frutosPendientes.Count} frutos de '{cultivo.cropName}' listos para cortar.", this);
    }

    /// <summary>
    /// El tallo no se deja arrancar mientras cuelguen frutos sin cortar. Sin esto
    /// el jugador libera la parcela y deja los tomates flotando en el aire.
    /// </summary>
    private void BloquearTalloHastaVaciar(GameObject planta)
    {
        XRGrabInteractable tallo = planta.GetComponent<XRGrabInteractable>();
        if (tallo == null)
            return;

        // El collider del tallo cubria todo el follaje, y los frutos nacen dentro
        // de el. Como el rayo choca con lo primero que encuentra, el jugador
        // apuntaba a un tomate y pegaba en la caja del tallo, que ademas esta
        // bloqueada: el resultado era que no se podia cosechar nada.
        // Se estrecha a una columna central, que es donde esta el tronco.
        BoxCollider caja = planta.GetComponent<BoxCollider>();
        if (caja != null)
        {
            Vector3 tamano = caja.size;
            caja.size = new Vector3(tamano.x * 0.25f, tamano.y, tamano.z * 0.25f);
        }

        tallo.selectFilters.Add(new XRSelectFilterDelegate((interactor, interactable) =>
            // Un fruto destruido cuenta como cortado, por eso el null
            frutosPendientes.All(f => f == null || f.Cosechada)));
    }

    private Vector3 PosicionDeRespaldo(GameObject planta, CropData cultivo, int indice)
    {
        Bounds b = CalcularBounds(planta);
        float radio = Mathf.Max(b.size.x, b.size.z) * 0.30f;
        float angulo = (360f / Mathf.Max(cultivo.fruitsPerPlant, 1)) * indice * Mathf.Deg2Rad;

        return new Vector3(
            b.center.x + Mathf.Cos(angulo) * radio,
            b.min.y + b.size.y * 0.55f,
            b.center.z + Mathf.Sin(angulo) * radio);
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