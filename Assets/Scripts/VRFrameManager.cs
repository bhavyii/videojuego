using UnityEngine;

/// <summary>
/// Asegura una tasa de cuadros constante de 90 FPS en visores VR para eliminar tirones y mareo.
/// </summary>
[DefaultExecutionOrder(-100)]
public class VRFrameManager : MonoBehaviour
{
    [SerializeField] private int targetFPS = 90;

    private void Awake()
    {
        Application.targetFrameRate = targetFPS;
        QualitySettings.vSyncCount = 0;
    }
}
