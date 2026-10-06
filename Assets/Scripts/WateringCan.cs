using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class WateringCan : MonoBehaviour
{
    [Header("Referencias")]
    public ParticleSystem waterParticles;
    public AudioSource waterAudio;

    [Header("Audio")]
    public AudioClip waterClip;
    [Range(0f, 1f)] public float maxVolume = 0.8f;

    [Header("Inclinacion")]
    [Tooltip("Angulo a partir del cual sale agua")]
    public float pourAngleThreshold = 45f;

    private XRGrabInteractable grabInteractable;
    private float targetVolume = 0f;
    private float hapticTimer = 0f;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void Start()
    {
        if (waterAudio != null && waterClip != null)
        {
            waterAudio.clip = waterClip;
            waterAudio.loop = true;
            waterAudio.volume = 0f;
        }
    }

    private void Update()
    {
        float angle = Vector3.Angle(transform.up, Vector3.up);
        bool isPouring = angle > pourAngleThreshold;

        if (isPouring)
        {
            if (waterParticles != null && !waterParticles.isPlaying)
                waterParticles.Play();

            targetVolume = maxVolume;

            if (waterAudio != null && !waterAudio.isPlaying)
                waterAudio.Play();

            // Vibración sutil en la mano que sostiene la regadera mientras vierte agua
            if (grabInteractable != null && grabInteractable.isSelected)
            {
                hapticTimer -= Time.deltaTime;
                if (hapticTimer <= 0f)
                {
                    hapticTimer = 0.1f;
                    foreach (var interactor in grabInteractable.interactorsSelecting)
                    {
                        VRHapticHelper.TriggerHaptic(interactor, 0.15f, 0.08f);
                    }
                }
            }
        }
        else
        {
            if (waterParticles != null && waterParticles.isPlaying)
                waterParticles.Stop();

            targetVolume = 0f;
            hapticTimer = 0f;
        }

        // Suavizado del audio para evitar cortes secos
        if (waterAudio != null)
        {
            waterAudio.volume = Mathf.MoveTowards(waterAudio.volume, targetVolume, Time.deltaTime * 3.5f);
            if (waterAudio.volume <= 0.001f && !isPouring && waterAudio.isPlaying)
            {
                waterAudio.Stop();
            }
        }
    }
}