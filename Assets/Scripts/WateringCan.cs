using UnityEngine;

public class WateringCan : MonoBehaviour
{
    [Header("Referencias")]
    public ParticleSystem waterParticles;

    [Header("Inclinacion")]
    [Tooltip("Angulo hacia abajo a partir del cual empieza a salir agua (grados)")]
    public float pourAngleThreshold = 45f;

    private void Update()
    {
        // Detecta el angulo respecto al eje vertical del mundo
        float angle = Vector3.Angle(transform.up, Vector3.up);

        if (angle > pourAngleThreshold)
        {
            if (!waterParticles.isPlaying)
            {
                waterParticles.Play();
            }
        }
        else
        {
            if (waterParticles.isPlaying)
            {
                waterParticles.Stop();
            }
        }
    }
}