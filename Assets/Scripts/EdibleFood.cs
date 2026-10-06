using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class EdibleFood : MonoBehaviour
{
    [Header("Efectos")]
    [Tooltip("Sonido de comer/morder")]
    public AudioClip eatClip;

    [Tooltip("Partículas opcionales al comer (migajas/efecto)")]
    public GameObject eatParticlesPrefab;

    private bool eaten = false;

    private void OnTriggerEnter(Collider other)
    {
        // Detecta si choca contra el trigger de la boca
        if (eaten) return;

        if (other.CompareTag("Mouth") || other.name.Equals("MouthTrigger"))
        {
            Eat(other);
        }
    }

    private void Eat(Collider mouthCollider)
    {
        eaten = true;

        // Reproducir sonido en la posición de la cabeza/boca
        if (eatClip != null)
        {
            AudioSource mouthAudio = mouthCollider.GetComponent<AudioSource>();
            if (mouthAudio != null)
            {
                mouthAudio.PlayOneShot(eatClip);
            }
            else
            {
                AudioSource.PlayClipAtPoint(eatClip, mouthCollider.transform.position);
            }
        }

        // Si tienes partículas de migajas, instanciarlas en la boca
        if (eatParticlesPrefab != null)
        {
            Instantiate(eatParticlesPrefab, mouthCollider.transform.position, Quaternion.identity);
        }

        // Si el jugador la tiene agarrada en la mano, dar feedback háptico y forzar a soltarla antes de destruirla
        XRGrabInteractable grab = GetComponent<XRGrabInteractable>();
        if (grab != null && grab.isSelected)
        {
            foreach (var interactor in grab.interactorsSelecting)
            {
                VRHapticHelper.TriggerHaptic(interactor, 0.7f, 0.12f);
            }
            // En XR Interaction Toolkit, deshabilitar el interactor o el componente suelta el agarre
            grab.enabled = false;
        }

        // Destruir el objeto de comida
        Destroy(gameObject);
    }
}