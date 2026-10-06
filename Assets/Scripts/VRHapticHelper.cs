using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public static class VRHapticHelper
{
    /// <summary>
    /// Envía un pulso háptico al interactor que esté interactuando con el objeto.
    /// </summary>
    public static void TriggerHaptic(IXRSelectInteractor interactor, float amplitude = 0.5f, float duration = 0.1f)
    {
        if (interactor == null) return;
        var player = interactor.transform.GetComponentInParent<HapticImpulsePlayer>();
        if (player != null)
        {
            player.SendHapticImpulse(amplitude, duration);
        }
    }

    /// <summary>
    /// Busca el HapticImpulsePlayer en el GameObject o sus padres y envía un pulso.
    /// </summary>
    public static void TriggerHaptic(Component component, float amplitude = 0.5f, float duration = 0.1f)
    {
        if (component == null) return;
        var player = component.GetComponentInParent<HapticImpulsePlayer>();
        if (player != null)
        {
            player.SendHapticImpulse(amplitude, duration);
        }
    }
}
