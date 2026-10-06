using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Botón físico 3D para reiniciar la misión. Se activa al presionarlo con la mano o mando.
/// </summary>
public class ResetMissionButton : MonoBehaviour
{
    [SerializeField] private Transform buttonCap;
    [SerializeField] private float pressDepth = 0.02f;
    [SerializeField] private AudioSource buttonAudio;
    [SerializeField] private AudioClip pressClip;

    private Vector3 initialLocalPos;
    private bool isPressed = false;

    private void Start()
    {
        if (buttonCap != null)
            initialLocalPos = buttonCap.localPosition;

        var interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnSelect);
        }
    }

    private void OnSelect(SelectEnterEventArgs args)
    {
        PressButton(args.interactorObject != null ? args.interactorObject.transform : null);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isPressed) return;
        if (other.CompareTag("Player") || other.name.Contains("Controller") || other.name.Contains("Hand") || other.name.Contains("Poke"))
        {
            PressButton(other.transform);
        }
    }

    private void PressButton(Transform interactor)
    {
        if (isPressed) return;
        isPressed = true;

        if (buttonCap != null)
            buttonCap.localPosition = initialLocalPos - new Vector3(0, pressDepth, 0);

        if (buttonAudio != null && pressClip != null)
            buttonAudio.PlayOneShot(pressClip);

        if (interactor != null)
            VRHapticHelper.TriggerHaptic(interactor, 0.6f, 0.1f);

        if (FarmerMissionManager.Instance != null)
        {
            FarmerMissionManager.Instance.ReiniciarMision();
        }

        Invoke(nameof(ReleaseButton), 0.5f);
    }

    private void ReleaseButton()
    {
        if (buttonCap != null)
            buttonCap.localPosition = initialLocalPos;
        isPressed = false;
    }
}
