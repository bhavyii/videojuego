using UnityEngine;

public class WaterRelay : MonoBehaviour
{
    private PlantGrowth[] spots;

    private void Awake()
    {
        // Obtiene todos los puntos de siembra hijos (SocketPoint, SocketPoint (1), etc.)
        spots = GetComponentsInChildren<PlantGrowth>();
    }

    private void OnParticleCollision(GameObject other)
    {
        // Reenvía el evento de riego a cada spot hijo
        for (int i = 0; i < spots.Length; i++)
        {
            if (spots[i] != null)
            {
                spots[i].Regar(other.name);
            }
        }
    }
}