using System.Collections;
using UnityEngine;

/// <summary>
/// Caja de venta: se le echan los cultivos cosechados y paga por ellos.
/// Necesita un Collider marcado como trigger.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SellBox : MonoBehaviour
{
    [Tooltip("Si esta activo, solo paga por plantas ya arrancadas, no por las sembradas")]
    public bool exigirCosechada = true;

    [Tooltip("Segundos que la verdura sigue visible tras venderla antes de desaparecer")]
    [Min(0f)] public float segundosParaDesaparecer = 1.5f;

    [Tooltip("Deja rastro en consola de todo lo que entra a la zona. Apagalo cuando ya funcione")]
    public bool diagnostico = false;

    [Header("Audio y Feedback")]
    public AudioSource sellAudioSource;
    public AudioClip sellClip;

    /// <summary>Se dispara cuando una verdura se vende con éxito en la caja.</summary>
    public static event System.Action<CropData> OnCropSold;

    private void Awake()
    {
        if (sellAudioSource == null)
            sellAudioSource = GetComponent<AudioSource>();
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (diagnostico)
            Debug.Log($"[Venta] Entro a la zona: '{other.name}'.", this);

        Vender(other);
    }

    // Red de seguridad: si algo quedo dentro sin dispararse el Enter
    // (porque cayo mientras lo sostenian, o ya estaba ahi), se cobra igual.
    private void OnTriggerStay(Collider other)
    {
        Vender(other);
    }

    private void Vender(Collider other)
    {
        HarvestableCrop cultivo = other.GetComponentInParent<HarvestableCrop>();
        if (cultivo == null)
            return;

        if (cultivo.esTallo)
            return; // los tallos no se compran

        if (cultivo.Vendida)
            return;

        if (exigirCosechada && !cultivo.Cosechada)
        {
            if (diagnostico)
                Debug.Log($"[Venta] '{cultivo.name}' aun no esta cosechada; no se paga.", this);
            return;
        }

        if (cultivo.crop == null)
        {
            Debug.LogWarning($"[Venta] '{cultivo.name}' no tiene cultivo asignado; no se puede pagar.", cultivo);
            return;
        }

        if (PlayerWallet.Instance == null)
        {
            Debug.LogError("[Venta] No hay PlayerWallet en la escena; no se puede pagar.", this);
            return;
        }

        int pago = cultivo.crop.harvestValue;
        PlayerWallet.Instance.Add(pago);
        cultivo.MarcarVendida();
        OnCropSold?.Invoke(cultivo.crop);

        // Audio al vender
        if (sellAudioSource != null && sellClip != null)
        {
            sellAudioSource.pitch = Random.Range(0.95f, 1.05f);
            sellAudioSource.PlayOneShot(sellClip);
        }

        // Feedback háptico al jugador
        var players = Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics.HapticImpulsePlayer>(FindObjectsInactive.Exclude);
        foreach (var p in players)
        {
            if (Vector3.Distance(p.transform.position, transform.position) < 4f)
            {
                p.SendHapticImpulse(0.5f, 0.1f);
            }
        }

        Debug.Log($"[Venta] '{cultivo.crop.cropName}' vendida en ${pago}. Saldo: ${PlayerWallet.Instance.Money}.", this);

        // Desaparición suave encogiéndose gradualmente
        StartCoroutine(DesvanecerVendido(cultivo.gameObject, segundosParaDesaparecer));
    }

    private IEnumerator DesvanecerVendido(GameObject obj, float delay)
    {
        if (obj == null) yield break;

        yield return new WaitForSeconds(delay);

        if (obj == null) yield break;

        Vector3 initialScale = obj.transform.localScale;
        float shrinkDuration = 0.4f;
        float elapsed = 0f;

        while (elapsed < shrinkDuration && obj != null)
        {
            float t = elapsed / shrinkDuration;
            obj.transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, Mathf.SmoothStep(0f, 1f, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (obj != null)
        {
            Destroy(obj);
        }
    }

    // Dibuja la zona de cobro para poder colocarla sin adivinar
    private void OnDrawGizmos()
    {
        BoxCollider caja = GetComponent<BoxCollider>();
        if (caja == null)
            return;

        Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(caja.center, caja.size);
        Gizmos.color = new Color(0.2f, 0.9f, 0.3f, 0.9f);
        Gizmos.DrawWireCube(caja.center, caja.size);
    }
}
