using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PlantGrowth : MonoBehaviour
{
    [Header("Configuracion de Planta")]
    [Tooltip("El prefab de la planta/tomate que crecerá")]
    public GameObject plantPrefab;

    [Tooltip("Punto donde brotará")]
    public Transform spawnPoint;

    [Tooltip("Tiempo en segundos que tarda la animación")]
    public float growDuration = 3f;

    private XRSocketInteractor socket;

    private void Awake()
    {
        socket = GetComponent<XRSocketInteractor>();
    }

    private void OnEnable()
    {
        socket.selectEntered.AddListener(OnPlantSeeded);
    }

    private void OnDisable()
    {
        socket.selectEntered.RemoveListener(OnPlantSeeded);
    }

    private void OnPlantSeeded(SelectEnterEventArgs args)
    {
        // Validar si el objeto colocado tiene el Tag Seed
        if (args.interactableObject.transform.CompareTag("Seed"))
        {
            GameObject seedObj = args.interactableObject.transform.gameObject;

            // Destruir la semilla consumida
            Destroy(seedObj);

            // Desactivar el socket para que no reciba otra semilla mientras crece
            socket.enabled = false;

            // Brotar la planta
            StartCoroutine(GrowRoutine());
        }
    }

    private IEnumerator GrowRoutine()
    {
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion spawnRot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject plant = Instantiate(plantPrefab, spawnPos, spawnRot);

        Vector3 finalScale = plant.transform.localScale;
        plant.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < growDuration)
        {
            plant.transform.localScale = Vector3.Lerp(Vector3.zero, finalScale, elapsed / growDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        plant.transform.localScale = finalScale;
    }
}