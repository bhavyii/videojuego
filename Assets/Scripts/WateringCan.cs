using UnityEngine;

public class WateringCan : MonoBehaviour
{
    [Header("Referencias")]
    public ParticleSystem waterParticles;
    public AudioSource waterAudio;

    [Header("Audio")]
    public AudioClip waterClip;

    [Header("Inclinacion")]
    [Tooltip("Angulo a partir del cual sale agua")]
    public float pourAngleThreshold = 45f;

    private void Start()
    {
        if (waterAudio != null && waterClip != null)
        {
            waterAudio.clip = waterClip;
        }
    }

    private void Update()
    {
        float angle = Vector3.Angle(transform.up, Vector3.up);

        if (angle > pourAngleThreshold)
        {
            if (!waterParticles.isPlaying)
                waterParticles.Play();

            if (waterAudio != null && !waterAudio.isPlaying)
                waterAudio.Play();
        }
        else
        {
            if (waterParticles.isPlaying)
                waterParticles.Stop();

            if (waterAudio != null && waterAudio.isPlaying)
                waterAudio.Stop();
        }
    }
}