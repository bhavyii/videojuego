using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Hace cosechable a una planta ya crecida. Mientras esta sembrada no se mueve;
/// en cuanto el jugador la agarra queda arrancada y se comporta como un objeto
/// suelto que se puede llevar a vender.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class HarvestableCrop : MonoBehaviour
{
    [Tooltip("Que cultivo es, para saber cuanto vale al venderlo")]
    public CropData crop;

    [Tooltip("Marca el tallo. Los tallos no se venden: solo se arrancan para liberar la parcela")]
    public bool esTallo;

    [Tooltip("Segundos que tarda el tallo arrancado en desaparecer, para no dejar basura")]
    [Min(0f)] public float segundosParaMarchitarse = 5f;

    /// <summary>Ya fue arrancada de la tierra.</summary>
    public bool Cosechada { get; private set; }

    /// <summary>Se dispara al arrancarla. La parcela lo usa para liberarse.</summary>
    public event Action Cosechado;

    /// <summary>Ya se pago por ella. Evita cobrarla de nuevo mientras se desvanece.</summary>
    public bool Vendida { get; private set; }

    public void MarcarVendida()
    {
        Vendida = true;

        // Se apaga el agarre para que no se pueda sacar de la caja algo ya pagado.
        // Si el jugador la trae en la mano, XRI cancela la seleccion al desactivar
        // el interactable, asi que la suelta sola dentro de la caja.
        if (grab != null)
            grab.enabled = false;
    }

    private Rigidbody cuerpo;
    private XRGrabInteractable grab;

    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        // Sembrada: no se cae ni rueda hasta que alguien la arranque.
        // Se congela con constraints y no con isKinematic a proposito: XRI guarda
        // el isKinematic al agarrar y lo restaura al soltar, asi que la planta
        // terminaria flotando en el aire despues de la primera cosecha.
        cuerpo.constraints = RigidbodyConstraints.FreezeAll;
    }

    private void OnEnable()
    {
        if (grab != null)
            grab.selectEntered.AddListener(AlArrancar);
    }

    private void OnDisable()
    {
        if (grab != null)
            grab.selectEntered.RemoveListener(AlArrancar);
    }

    private void AlArrancar(SelectEnterEventArgs args)
    {
        if (Cosechada)
            return;

        Cosechada = true;
        cuerpo.constraints = RigidbodyConstraints.None;

        // El tallo no sirve de nada una vez arrancado; se marchita solo
        // para no dejar tallos tirados por toda la granja.
        if (esTallo && segundosParaMarchitarse > 0f)
            Destroy(gameObject, segundosParaMarchitarse);

        Debug.Log($"[Cosecha] '{(crop != null ? crop.cropName : name)}' arrancada de la tierra.", this);

        Cosechado?.Invoke();
    }
}
