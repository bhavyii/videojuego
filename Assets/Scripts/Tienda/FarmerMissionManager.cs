using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

/// <summary>
/// Misión "La Canasta del Granjero": El jugador debe cosechar y vender
/// al menos 1 verdura de cada tipo (Tomate, Zanahoria, Cebolla, Lechuga).
/// Ideal para niños: objetivo claro, visual, sin números complicados y con recompensa festiva.
/// </summary>
public class FarmerMissionManager : MonoBehaviour
{
    public static FarmerMissionManager Instance { get; private set; }

    [Header("Referencias Visuales")]
    [Tooltip("Texto 3D donde se muestra la lista y progreso")]
    [SerializeField] private TMP_Text missionText;

    [Tooltip("Sistema de partículas de confeti para la celebración")]
    [SerializeField] private ParticleSystem celebrationConfetti;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip itemCheckedClip;
    [SerializeField] private AudioClip victoryFanfareClip;

    [Header("Estado de Cultivos")]
    private bool tomateCompletado = false;
    private bool zanahoriaCompletado = false;
    private bool cebollaCompletado = false;
    private bool lechugaCompletado = false;

    public bool IsCompleted => tomateCompletado && zanahoriaCompletado && cebollaCompletado && lechugaCompletado;
    public int CompletedCount => (tomateCompletado ? 1 : 0) + (zanahoriaCompletado ? 1 : 0) + (cebollaCompletado ? 1 : 0) + (lechugaCompletado ? 1 : 0);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (missionText == null)
            missionText = GetComponentInChildren<TMP_Text>();
    }

    private void Start()
    {
        // Si no se asignó clip de victoria, generar uno alegre procesalmente
        if (victoryFanfareClip == null)
        {
            victoryFanfareClip = CreateFanfareClip();
        }

        ActualizarVisual();
    }

    private void OnEnable()
    {
        SellBox.OnCropSold += OnCropSold;
    }

    private void OnDisable()
    {
        SellBox.OnCropSold -= OnCropSold;
    }

    private void OnCropSold(CropData crop)
    {
        if (crop == null || IsCompleted) return;

        string nombre = crop.cropName.Trim().ToLower();
        bool nuevoLogro = false;

        if (nombre.Contains("tomate") && !tomateCompletado)
        {
            tomateCompletado = true;
            nuevoLogro = true;
        }
        else if (nombre.Contains("zanahoria") && !zanahoriaCompletado)
        {
            zanahoriaCompletado = true;
            nuevoLogro = true;
        }
        else if (nombre.Contains("cebolla") && !cebollaCompletado)
        {
            cebollaCompletado = true;
            nuevoLogro = true;
        }
        else if (nombre.Contains("lechuga") && !lechugaCompletado)
        {
            lechugaCompletado = true;
            nuevoLogro = true;
        }

        if (nuevoLogro)
        {
            int completados = CompletedCount;
            Debug.Log($"[Mision] ¡Nueva verdura para la canasta! '{crop.cropName}'. Progreso: {completados}/4");

            // Feedback háptico en las manos del jugador
            EnviarHapticAJugador(0.5f, 0.12f);

            if (IsCompleted)
            {
                StartCoroutine(SecuenciaVictoria());
            }
            else
            {
                // Sonido alegre con tono ascendente según el avance (do, mi, sol)
                if (audioSource != null && itemCheckedClip != null)
                {
                    audioSource.pitch = 1.0f + (completados * 0.15f);
                    audioSource.PlayOneShot(itemCheckedClip);
                }
                ActualizarVisual();
            }
        }
    }

    private void ActualizarVisual()
    {
        if (missionText == null) return;

        if (IsCompleted)
        {
            missionText.text =
                "<b><size=115%><color=#51CF66>¡MISIÓN COMPLETADA!</color></size></b>\n" +
                "<b><size=95%><color=#FFD43B>¡ERES UN SÚPER GRANJERO!</color></size></b>\n\n" +
                "<size=75%><color=#FFFFFF>¡Llenaste toda la canasta de verduras!</color></size>\n\n" +
                "<size=68%><color=#74C0FC>[ Pulsa el botón rojo para reiniciar ]</color></size>";
            return;
        }

        string checkTomate = tomateCompletado ? "<color=#51CF66><b>[ LISTO ]</b></color>" : "<color=#868E96>[ PENDIENTE ]</color>";
        string checkZanahoria = zanahoriaCompletado ? "<color=#51CF66><b>[ LISTO ]</b></color>" : "<color=#868E96>[ PENDIENTE ]</color>";
        string checkCebolla = cebollaCompletado ? "<color=#51CF66><b>[ LISTO ]</b></color>" : "<color=#868E96>[ PENDIENTE ]</color>";
        string checkLechuga = lechugaCompletado ? "<color=#51CF66><b>[ LISTO ]</b></color>" : "<color=#868E96>[ PENDIENTE ]</color>";

        missionText.text =
            "<b><size=110%><color=#FFD43B>CANASTA DEL GRANJERO</color></size></b>\n" +
            "<size=70%><color=#D0D0D0>¡Cosecha y vende una de cada verdura!</color></size>\n\n" +
            $"<color=#FF6B6B>● Tomate</color>            {checkTomate}\n" +
            $"<color=#FFA94D>● Zanahoria</color>         {checkZanahoria}\n" +
            $"<color=#FCC419>● Cebolla</color>           {checkCebolla}\n" +
            $"<color=#51CF66>● Lechuga</color>           {checkLechuga}\n\n" +
            $"<b><size=85%><color=#FFFFFF>Progreso: {CompletedCount} / 4</color></size></b>";
    }

    private IEnumerator SecuenciaVictoria()
    {
        ActualizarVisual();

        // Audio de fanfarria alegre
        if (audioSource != null && victoryFanfareClip != null)
        {
            audioSource.pitch = 1.0f;
            audioSource.PlayOneShot(victoryFanfareClip);
        }

        // Lluvia de confeti
        if (celebrationConfetti != null)
        {
            celebrationConfetti.Play();
        }

        // Ráfaga de vibraciones celebratorias en mandos
        for (int i = 0; i < 3; i++)
        {
            EnviarHapticAJugador(0.7f, 0.15f);
            yield return new WaitForSeconds(0.2f);
        }
    }

    public void ReiniciarMision()
    {
        tomateCompletado = false;
        zanahoriaCompletado = false;
        cebollaCompletado = false;
        lechugaCompletado = false;

        if (celebrationConfetti != null)
        {
            celebrationConfetti.Stop();
            celebrationConfetti.Clear();
        }

        if (audioSource != null && itemCheckedClip != null)
        {
            audioSource.pitch = 0.9f;
            audioSource.PlayOneShot(itemCheckedClip);
        }

        EnviarHapticAJugador(0.4f, 0.1f);
        ActualizarVisual();
        Debug.Log("[Mision] Misión reiniciada.");
    }

    private void EnviarHapticAJugador(float amplitud, float duracion)
    {
        var players = Object.FindObjectsByType<HapticImpulsePlayer>(FindObjectsInactive.Exclude);
        foreach (var p in players)
        {
            if (p != null)
            {
                p.SendHapticImpulse(amplitud, duracion);
            }
        }
    }

    /// <summary>
    /// Genera una fanfarria alegre en código sin requerir archivos de audio externos.
    /// Melodía en arpegio triunfante (Do - Mi - Sol - Do Mayor con armónicos).
    /// </summary>
    private static AudioClip CreateFanfareClip()
    {
        int sampleRate = 44100;
        float duration = 1.6f;
        int totalSamples = (int)(sampleRate * duration);
        float[] samples = new float[totalSamples];

        // 4 notas alegres: C5 (523Hz), E5 (659Hz), G5 (784Hz), C6 (1046Hz)
        float[] noteTimes = { 0.0f, 0.22f, 0.44f, 0.70f };
        float[] noteFreqs = { 523.25f, 659.25f, 783.99f, 1046.50f };
        float[] noteDurs  = { 0.20f, 0.20f, 0.24f, 0.85f };

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / sampleRate;
            float val = 0f;

            for (int n = 0; n < noteTimes.Length; n++)
            {
                float noteStart = noteTimes[n];
                float noteEnd = noteStart + noteDurs[n];
                if (t >= noteStart && t <= noteEnd)
                {
                    float noteT = t - noteStart;
                    float env = Mathf.Exp(-noteT * (n == 3 ? 3.5f : 6f)); // Decaimiento suave
                    float f = noteFreqs[n];
                    // Tono cálido con fundamental + segundo armónico
                    float wave = Mathf.Sin(2f * Mathf.PI * f * noteT) * 0.7f +
                                 Mathf.Sin(4f * Mathf.PI * f * noteT) * 0.3f;
                    val += wave * env * 0.4f;
                }
            }
            samples[i] = Mathf.Clamp(val, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("VictoryFanfare", totalSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
