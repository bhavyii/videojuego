using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

/// <summary>
/// Botón físico 3D para reiniciar la misión.
/// Soporta:
/// 1. Interacción a distancia por rayo (NearFarInteractor) y clic con gatillo / botón.
/// 2. Interacción física por proximidad directa (acercar el mando o la mano).
/// 3. Poke Interactor nativo de XR Interaction Toolkit.
/// 4. Feedback visual (hover / pulsación), háptico y sonoro.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class ResetMissionButton : MonoBehaviour
{
    [Header("Referencias Mecánicas")]
    [Tooltip("Pieza móvil del botón (Cap)")]
    [SerializeField] private Transform buttonCap;
    [Tooltip("Profundidad de pulsación hacia adentro")]
    [SerializeField] private float pressDepth = 0.012f;
    [Tooltip("Eje local hacia donde se hunde el botón (hacia adentro de la base)")]
    [SerializeField] private Vector3 pressAxis = new Vector3(0f, 0f, 1f);
    [Tooltip("Velocidad de movimiento visual del botón")]
    [SerializeField] private float animSpeed = 0.2f;

    [Header("Audio y Háptica")]
    [SerializeField] private AudioSource buttonAudio;
    [SerializeField] private AudioClip pressClip;
    [SerializeField] private float hapticAmplitude = 0.7f;
    [SerializeField] private float hapticDuration = 0.15f;

    [Header("Feedback Visual")]
    [SerializeField] private Renderer capRenderer;
    [SerializeField] private Color normalColor = new Color(0.9f, 0.15f, 0.15f);
    [SerializeField] private Color hoverColor = new Color(1.0f, 0.45f, 0.45f);
    [SerializeField] private Color pressedColor = new Color(0.45f, 0.05f, 0.05f);

    [Header("Detección de Proximidad Física")]
    [Tooltip("Distancia en metros para activar el botón al acercar el mando o la mano")]
    [SerializeField] private float proximityDistance = 0.07f;
    [Tooltip("Tiempo mínimo antes de permitir otra pulsación")]
    [SerializeField] private float cooldownTime = 0.6f;

    private XRSimpleInteractable interactable;
    private MaterialPropertyBlock propBlock;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private Vector3 initialLocalPos;
    private Vector3 pressedLocalPos;
    private bool isPressed = false;
    private bool isHovered = false;
    private float lastPressTime = -10f;

    // Cache de transformaciones de mandos / interactoras para proximidad
    private readonly List<Transform> trackedInteractors = new List<Transform>();
    private float nextControllerCacheTime = 0f;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null)
            interactable = gameObject.AddComponent<XRSimpleInteractable>();

        // Auto-asignar referencias si faltan
        if (buttonCap == null)
        {
            var capChild = transform.Find("Cap");
            if (capChild != null) buttonCap = capChild;
        }

        if (capRenderer == null && buttonCap != null)
        {
            capRenderer = buttonCap.GetComponent<Renderer>();
        }

        if (buttonAudio == null)
        {
            buttonAudio = GetComponentInParent<AudioSource>();
        }

        propBlock = new MaterialPropertyBlock();

        if (buttonCap != null)
        {
            initialLocalPos = buttonCap.localPosition;
            pressedLocalPos = initialLocalPos + pressAxis.normalized * pressDepth;
        }

        SetVisualColor(normalColor);
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnSelectEntered);
            interactable.firstHoverEntered.AddListener(OnHoverEntered);
            interactable.lastHoverExited.AddListener(OnHoverExited);
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnSelectEntered);
            interactable.firstHoverEntered.RemoveListener(OnHoverEntered);
            interactable.lastHoverExited.RemoveListener(OnHoverExited);
        }
    }

    private void Start()
    {
        RefreshTrackedInteractors();
    }

    private void Update()
    {
        // 1. Refrescar lista de interactoras si es necesario
        if (Time.time >= nextControllerCacheTime)
        {
            RefreshTrackedInteractors();
            nextControllerCacheTime = Time.time + 2.0f;
        }

        // 2. Detección física por proximidad del mando o mano
        CheckProximityInteraction();

        // 3. Animación fluida de la tapa del botón
        if (buttonCap != null)
        {
            Vector3 targetPos = isPressed ? pressedLocalPos : initialLocalPos;
            buttonCap.localPosition = Vector3.MoveTowards(buttonCap.localPosition, targetPos, animSpeed * Time.deltaTime);
        }

        // 4. Liberar pulsación una vez transcurrido el cooldown y que el mando se haya alejado
        if (isPressed && Time.time >= lastPressTime + cooldownTime)
        {
            if (!IsAnyInteractorNear(proximityDistance * 1.25f))
            {
                ReleaseButton();
            }
        }
    }

    private void CheckProximityInteraction()
    {
        if (isPressed) return;

        Vector3 capWorldPos = buttonCap != null ? buttonCap.position : transform.position;

        for (int i = 0; i < trackedInteractors.Count; i++)
        {
            var t = trackedInteractors[i];
            if (t == null || !t.gameObject.activeInHierarchy) continue;

            float dist = Vector3.Distance(capWorldPos, t.position);
            if (dist <= proximityDistance)
            {
                PressButton(t);
                return;
            }
        }
    }

    private bool IsAnyInteractorNear(float threshold)
    {
        Vector3 capWorldPos = buttonCap != null ? buttonCap.position : transform.position;
        for (int i = 0; i < trackedInteractors.Count; i++)
        {
            var t = trackedInteractors[i];
            if (t == null || !t.gameObject.activeInHierarchy) continue;

            if (Vector3.Distance(capWorldPos, t.position) <= threshold)
                return true;
        }
        return false;
    }

    private void RefreshTrackedInteractors()
    {
        trackedInteractors.Clear();

        // Buscar Poke Interactors y Near-Far Interactors en la escena
        var pokeInteractors = Object.FindObjectsByType<XRPokeInteractor>(FindObjectsInactive.Exclude);
        foreach (var pi in pokeInteractors)
        {
            if (pi != null)
            {
                Transform target = pi.attachTransform != null ? pi.attachTransform : pi.transform;
                if (!trackedInteractors.Contains(target))
                    trackedInteractors.Add(target);
            }
        }

        var nearFarInteractors = Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Exclude);
        foreach (var nf in nearFarInteractors)
        {
            if (nf != null)
            {
                Transform target = nf.attachTransform != null ? nf.attachTransform : nf.transform;
                if (!trackedInteractors.Contains(target))
                    trackedInteractors.Add(target);
            }
        }

        // También mandos del XROrigin directamente si existen
        var controllers = GameObject.FindGameObjectsWithTag("Player");
        foreach (var c in controllers)
        {
            if (c != null && !trackedInteractors.Contains(c.transform))
                trackedInteractors.Add(c.transform);
        }
    }

    #region Eventos XR Interaction Toolkit
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        Transform interactorTransform = args.interactorObject != null ? args.interactorObject.transform : null;
        PressButton(interactorTransform);
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        isHovered = true;
        if (!isPressed)
        {
            SetVisualColor(hoverColor);
        }
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        isHovered = false;
        if (!isPressed)
        {
            SetVisualColor(normalColor);
        }
    }
    #endregion

    #region Triggers y Colisiones físicas de respaldo
    private void OnTriggerEnter(Collider other)
    {
        if (isPressed) return;
        if (other == null) return;

        string n = other.name;
        if (other.CompareTag("Player") || n.Contains("Controller") || n.Contains("Hand") || n.Contains("Poke") || n.Contains("Interactor"))
        {
            PressButton(other.transform);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isPressed) return;
        if (collision == null || collision.collider == null) return;

        string n = collision.collider.name;
        if (collision.collider.CompareTag("Player") || n.Contains("Controller") || n.Contains("Hand") || n.Contains("Poke") || n.Contains("Interactor"))
        {
            PressButton(collision.transform);
        }
    }
    #endregion

    public void PressButton(Transform interactor)
    {
        if (isPressed) return;
        if (Time.time < lastPressTime + 0.3f) return;

        isPressed = true;
        lastPressTime = Time.time;

        SetVisualColor(pressedColor);

        // Feedback sonoro
        if (buttonAudio != null && pressClip != null)
        {
            buttonAudio.PlayOneShot(pressClip);
        }

        // Feedback háptico
        TriggerHaptics(interactor);

        // Reiniciar la misión
        var missionManager = FarmerMissionManager.Instance != null 
            ? FarmerMissionManager.Instance 
            : Object.FindAnyObjectByType<FarmerMissionManager>();

        if (missionManager != null)
        {
            missionManager.ReiniciarMision();
        }
        else
        {
            Debug.LogWarning("[ResetMissionButton] FarmerMissionManager no encontrado en la escena.");
        }
    }

    private void ReleaseButton()
    {
        isPressed = false;
        SetVisualColor(isHovered ? hoverColor : normalColor);
    }

    private void TriggerHaptics(Transform interactor)
    {
        bool hapticSent = false;
        if (interactor != null)
        {
            var hip = interactor.GetComponentInParent<HapticImpulsePlayer>();
            if (hip != null)
            {
                hip.SendHapticImpulse(hapticAmplitude, hapticDuration);
                hapticSent = true;
            }
        }

        if (!hapticSent)
        {
            // Enviar a todos los mandos activos
            var allHaptics = Object.FindObjectsByType<HapticImpulsePlayer>(FindObjectsInactive.Exclude);
            foreach (var h in allHaptics)
            {
                if (h != null)
                {
                    h.SendHapticImpulse(hapticAmplitude, hapticDuration);
                }
            }
        }
    }

    private void SetVisualColor(Color color)
    {
        if (capRenderer == null) return;
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        capRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor(BaseColorId, color);
        capRenderer.SetPropertyBlock(propBlock);
    }
}
