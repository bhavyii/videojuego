using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Una casilla del estante de la tienda. Muestra una semilla en venta; cuando
/// el jugador la agarra se le cobra y el estante repone otra.
/// Si no le alcanza el dinero, la semilla simplemente no se deja agarrar.
/// </summary>
public class SeedDispenser : MonoBehaviour
{
    [Header("Producto")]
    [Tooltip("Que cultivo vende esta casilla")]
    public CropData crop;

    [Header("Colocacion")]
    [Tooltip("Donde aparece la semilla. Si se deja vacio usa este mismo objeto")]
    public Transform spawnPoint;

    [Tooltip("Segundos que tarda el estante en reponer la siguiente semilla")]
    // Un segundo, no un cuarto: con la reposicion inmediata la semilla nueva
    // aparecia encima de la que el jugador estaba sacando y la empujaba fuera.
    [Min(0f)] public float restockDelay = 1f;

    // Filtro de la semilla que esta ahorita en el estante. Se apaga al venderla
    // para que el jugador pueda soltarla y volver a agarrarla aunque ya no le alcance.
    private XRSelectFilterDelegate currentFilter;

    private void Start()
    {
        if (crop == null)
        {
            Debug.LogError($"[SeedDispenser] '{name}' no tiene cultivo asignado.", this);
            enabled = false;
            return;
        }

        if (crop.seedPrefab == null)
        {
            Debug.LogError($"[SeedDispenser] El cultivo '{crop.cropName}' no tiene seedPrefab.", this);
            enabled = false;
            return;
        }

        Restock();
    }

    private void Restock()
    {
        Transform anchor = spawnPoint != null ? spawnPoint : transform;
        GameObject seed = Instantiate(crop.seedPrefab, anchor.position, anchor.rotation);
        seed.name = $"Semilla_{crop.cropName}";

        // La semilla carga su identidad para que la parcela sepa que sembrar
        SeedItem item = seed.GetComponent<SeedItem>();
        if (item == null)
            item = seed.AddComponent<SeedItem>();
        item.crop = crop;

        PintarSemilla(seed, crop.seedColor);

        XRGrabInteractable grab = seed.GetComponent<XRGrabInteractable>();
        if (grab == null)
        {
            Debug.LogError($"[SeedDispenser] El seedPrefab de '{crop.cropName}' no tiene XRGrabInteractable, no se podra agarrar.", this);
            return;
        }

        // Bloquea el agarre en vez de dejar comprar y luego reclamar
        currentFilter = new XRSelectFilterDelegate((interactor, interactable) =>
            PlayerWallet.Instance != null && PlayerWallet.Instance.CanAfford(crop.seedPrice));
        grab.selectFilters.Add(currentFilter);

        grab.selectEntered.AddListener(OnSeedTaken);

        Debug.Log($"[SeedDispenser] '{crop.cropName}' repuesta en {anchor.position}.", seed);
    }

    // Marca en la vista Scene donde va a nacer la semilla, aunque no este en Play
    private void OnDrawGizmos()
    {
        Transform anchor = spawnPoint != null ? spawnPoint : transform;
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
        Gizmos.DrawSphere(anchor.position, 0.02f);
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(anchor.position, 0.06f);
    }

    // Tinte por cultivo: sin esto todas las semillas se ven identicas en el cajon
    private static void PintarSemilla(GameObject seed, Color color)
    {
        foreach (Renderer rend in seed.GetComponentsInChildren<Renderer>())
        {
            Material mat = rend.material; // instancia, no toca el material del prefab

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
        }
    }

    private void OnSeedTaken(SelectEnterEventArgs args)
    {
        if (args.interactableObject is not XRGrabInteractable grab)
            return;

        // Se cobra una sola vez: la semilla deja de pertenecer al estante
        grab.selectEntered.RemoveListener(OnSeedTaken);

        if (currentFilter != null)
        {
            currentFilter.canProcess = false;
            currentFilter = null;
        }

        if (PlayerWallet.Instance == null)
        {
            Debug.LogError("[SeedDispenser] No hay PlayerWallet en la escena, no se pudo cobrar.", this);
            return;
        }

        if (!PlayerWallet.Instance.TrySpend(crop.seedPrice))
        {
            Debug.LogWarning($"[SeedDispenser] No alcanzo el dinero para '{crop.cropName}'.", this);
            return;
        }

        Invoke(nameof(Restock), restockDelay);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Restock));
    }
}
