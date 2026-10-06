using System.Collections;
using UnityEngine;

/// <summary>
/// Comportamiento vivo para las vacas de la granja:
/// - Animación Idle, Walk y masticar
/// - Pequeños paseos dentro de su corral
/// - Sonidos de mugido ("Muuu") procesales tiernos
/// - Reacción cuando el jugador se acerca (la vaca gira la cabeza hacia el jugador)
/// </summary>
public class CowBehavior : MonoBehaviour
{
    [Header("Animación")]
    private Animator animator;

    [Header("Límites de Paseo (Corral)")]
    public Vector3 penCenter = new Vector3(-8f, 0f, 0f);
    public float penRadius = 4f;
    public float walkSpeed = 0.8f;
    public float turnSpeed = 90f;

    [Header("Sonido")]
    private AudioSource audioSource;
    private float nextMooTime;

    private Vector3 targetPos;
    private bool isWalking = false;
    private Transform playerHead;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 18f;
            audioSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        targetPos = transform.position;
        nextMooTime = Time.time + Random.Range(4f, 15f);

        Camera mainCam = Camera.main;
        if (mainCam != null)
            playerHead = mainCam.transform;

        StartCoroutine(LifeRoutine());
    }

    private IEnumerator LifeRoutine()
    {
        while (true)
        {
            // Decidir si descansar o dar un paseo
            float waitTime = Random.Range(3f, 8f);
            isWalking = false;

            if (animator != null)
                animator.Play("root_Idle");

            yield return new WaitForSeconds(waitTime);

            // Elegir un punto dentro del corral
            Vector2 circle = Random.insideUnitCircle * penRadius;
            targetPos = new Vector3(penCenter.x + circle.x, transform.position.y, penCenter.z + circle.y);
            isWalking = true;

            if (animator != null)
                animator.Play("root_Walk");

            float walkTimeout = 6f;
            while (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                    new Vector3(targetPos.x, 0, targetPos.z)) > 0.3f && walkTimeout > 0f)
            {
                Vector3 dir = (targetPos - transform.position);
                dir.y = 0;
                if (dir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
                    transform.position += transform.forward * walkSpeed * Time.deltaTime;
                }
                walkTimeout -= Time.deltaTime;
                yield return null;
            }
        }
    }

    private void Update()
    {
        // Mugir periódicamente
        if (Time.time >= nextMooTime)
        {
            nextMooTime = Time.time + Random.Range(12f, 25f);
            PlayMoo();
        }

        // Si el jugador está muy cerca y la vaca está descansando, mirarlo amigablemente
        if (!isWalking && playerHead != null)
        {
            float dist = Vector3.Distance(transform.position, playerHead.position);
            if (dist < 4.5f)
            {
                Vector3 toPlayer = playerHead.position - transform.position;
                toPlayer.y = 0;
                if (toPlayer.sqrMagnitude > 0.1f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(toPlayer);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 2f);
                }
            }
        }
    }

    private void PlayMoo()
    {
        if (audioSource != null)
        {
            AudioClip mooClip = CreateMooClip();
            audioSource.pitch = Random.Range(0.85f, 1.15f);
            audioSource.PlayOneShot(mooClip, 0.7f);
        }
    }

    /// <summary>
    /// Genera un mugido amigable ("Muuu") procesal en audio 3D
    /// </summary>
    private static AudioClip CreateMooClip()
    {
        int sampleRate = 44100;
        float duration = 1.3f;
        int totalSamples = (int)(sampleRate * duration);
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            // Forma de onda cálida y modulada (140Hz - 110Hz con inflexión natural de vaca)
            float freq = Mathf.Lerp(130f, 105f, t / duration);
            float env = Mathf.Sin(Mathf.PI * (t / duration)); // Fade in y out natural
            env = Mathf.Pow(env, 0.7f);

            float wave = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f +
                         Mathf.Sin(4f * Mathf.PI * freq * t) * 0.3f +
                         Mathf.Sin(6f * Mathf.PI * freq * t) * 0.1f;

            samples[i] = wave * env * 0.4f;
        }

        AudioClip clip = AudioClip.Create("MooSound", totalSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
