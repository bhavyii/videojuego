using TMPro;
using UnityEngine;

/// <summary>
/// Cartel que muestra el saldo del jugador. Se cuelga de un objeto con TMP_Text
/// y se actualiza solo cada vez que la cartera cambia.
/// </summary>
public class MoneySign : MonoBehaviour
{
    [Tooltip("Texto donde se escribe el saldo. Si se deja vacio se busca en los hijos")]
    [SerializeField] private TMP_Text label;

    [Tooltip("{0} es el saldo")]
    [SerializeField] private string formato = "Dinero: ${0}";

    private void Awake()
    {
        if (label == null)
            label = GetComponentInChildren<TMP_Text>();
    }

    // En Start, no en OnEnable: asi la cartera ya corrio su Awake y existe Instance
    private void Start()
    {
        if (PlayerWallet.Instance == null)
        {
            Debug.LogWarning("[MoneySign] No hay PlayerWallet en la escena; el cartel se queda vacio.", this);
            return;
        }

        PlayerWallet.Instance.OnMoneyChanged += Actualizar;
        Actualizar(PlayerWallet.Instance.Money);
    }

    private void OnDestroy()
    {
        if (PlayerWallet.Instance != null)
            PlayerWallet.Instance.OnMoneyChanged -= Actualizar;
    }

    private void Actualizar(int dinero)
    {
        if (label != null)
            label.text = string.Format(formato, dinero);
    }
}
