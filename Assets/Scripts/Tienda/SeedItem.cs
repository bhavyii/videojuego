using UnityEngine;

/// <summary>
/// Marca a una semilla con el cultivo que representa. La parcela lee este
/// componente al plantarla para saber que debe crecer. Sin esto, todas las
/// semillas serian identicas.
/// </summary>
public class SeedItem : MonoBehaviour
{
    [Tooltip("Que cultivo es esta semilla")]
    public CropData crop;
}
