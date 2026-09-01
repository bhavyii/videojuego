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
    private bool hasSeed = false;
    private bool isWatered = false;

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
        if (args.interactableObject.transform.CompareTag("Seed") && !hasSeed)
        {
            GameObject seedObj = args.interactableObject.transform.gameObject;
            Destroy(seedObj);

            hasSeed = true;
            socket.enabled = false;
        }
    }

    // Detecta el choque de las particulas de agua contra el collider de la tierra
    private void OnParticleCollision(GameObject other)
    {
        if (hasSeed && !isWatered)
        {
            isWatered = true;
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