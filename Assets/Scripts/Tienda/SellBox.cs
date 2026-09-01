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

    [Tooltip("Segundos que la verdura sigue visible tras venderla, para que el jugador la vea")]
    [Min(0f)] public float segundosParaDesaparecer = 5f;

    [Tooltip("Deja rastro en consola de todo lo que entra a la zona. Apagalo cuando ya funcione")]
    public bool diagnostico = true;

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

        // Imprescindible: OnTriggerStay corre cada cuadro, y sin esta bandera
        // la misma verdura se pagaria una y otra vez durante los 5 segundos
        // que tarda en desaparecer.
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

        Debug.Log($"[Venta] '{cultivo.crop.cropName}' vendida en ${pago}. Saldo: ${PlayerWallet.Instance.Money}.", this);

        // El pago es inmediato; la verdura se queda un rato para poder verla
        Destroy(cultivo.gameObject, segundosParaDesaparecer);
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
