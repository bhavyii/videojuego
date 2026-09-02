using System;
using UnityEngine;

/// <summary>
/// Dinero del jugador. Se pone una sola vez en la escena, en un objeto vacio.
/// Cualquier script lo consulta con PlayerWallet.Instance.
/// </summary>
public class PlayerWallet : MonoBehaviour
{
    public static PlayerWallet Instance { get; private set; }

    [Header("Economia")]
    [Tooltip("Dinero con el que arranca el jugador, en pesos")]
    [SerializeField, Min(0)] private int startingMoney = 5000;

    [Space]
    [Tooltip("Saldo en vivo. Se ve aqui durante Play Mode; editarlo fuera de Play no sirve de nada")]
    [SerializeField] private int saldoActual;

    /// <summary>Dinero actual. Solo se modifica con TrySpend y Add.</summary>
    public int Money => saldoActual;

    /// <summary>Se dispara cada vez que cambia el saldo. Util para carteles y UI.</summary>
    public event Action<int> OnMoneyChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[PlayerWallet] Ya existe una cartera en la escena. Se elimina la de '{name}'.", this);
            Destroy(this);
            return;
        }

        Instance = this;
        saldoActual = startingMoney;
    }

    private void Start()
    {
        // En Start para que los carteles alcancen a suscribirse en su Awake
        OnMoneyChanged?.Invoke(saldoActual);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public bool CanAfford(int amount) => saldoActual >= amount;

    /// <summary>Cobra si alcanza. Devuelve false y no cobra nada si no alcanza.</summary>
    public bool TrySpend(int amount)
    {
        if (amount < 0 || !CanAfford(amount))
            return false;

        saldoActual -= amount;
        OnMoneyChanged?.Invoke(saldoActual);
        return true;
    }

    /// <summary>Para cuando se venda la cosecha mas adelante.</summary>
    public void Add(int amount)
    {
        if (amount <= 0)
            return;

        saldoActual += amount;
        OnMoneyChanged?.Invoke(saldoActual);
    }
}
