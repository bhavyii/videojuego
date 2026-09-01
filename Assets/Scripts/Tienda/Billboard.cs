using UnityEngine;

/// <summary>
/// Gira el objeto para que siempre mire a la camara del jugador. Se usa en los
/// carteles del puesto, para que se lean desde cualquier lado sin tener que
/// rodear la tienda.
/// </summary>
public class Billboard : MonoBehaviour
{
    [Tooltip("Girar solo en horizontal. Evita que el texto se incline cuando el jugador mira hacia arriba o abajo")]
    public bool soloEjeY = true;

    private Transform camara;

    // LateUpdate: despues de que la camara ya se movio en este cuadro,
    // asi el cartel no va un fotograma atrasado.
    private void LateUpdate()
    {
        if (camara == null)
        {
            Camera principal = Camera.main;
            if (principal == null)
                return;

            camara = principal.transform;
        }

        Vector3 direccion = transform.position - camara.position;

        if (soloEjeY)
            direccion.y = 0f;

        if (direccion.sqrMagnitude < 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(direccion);
    }
}
