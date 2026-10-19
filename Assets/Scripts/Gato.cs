using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class Gato : MonoBehaviour
{
    public enum Estado
    {
        Reposo = 0,
        Arisco = 1,
        Comiendo = 2,
        Cazando = 3,
        Durmiendo = 4,
        Acicalando = 5,
        Muerto = 6
    }

    public enum Entrada
    {
        Alimentar,    // I
        Jugar,        // J
        Dormir,       // K
        Cepillar,     // L
        Warning,      
        Zero,         
        FinAnimacion  
    }

    [Header("Stats (0-100)")]
    [Range(0, 100)] public float hambre = 70f;
    [Range(0, 100)] public float energia = 70f;
    [Range(0, 100)] public float felicidad = 70f;

    [Header("Gasto por segundo")]
    public float gastoHambre = 0.8f;
    public float gastoEnergia = 0.6f;
    public float gastoFelicidad = 0.5f;

    [Header("Recuperaciones")]
    public float recHambre = 40f;
    public float recFelicidad = 35f;
    public float recEnergiaPorSegundo = 15f;

    [Header("Reglas")]
    public float umbralBajo = 30f;
    public float duracionAccion = 3f;

    [Header("Integración")]
    public Arbitro arbitro;
    public bool controlesActivos = true;

    [Header("Solo lectura")]
    public Estado estadoActual = Estado.Reposo;
    public bool EstaMuerto => estadoActual == Estado.Muerto;

    private Animator animator;
    private float tiempoAccion;
    private bool warningAnterior;
    private bool zeroAnterior;

    void Awake()
    {
        animator = GetComponent<Animator>();
        animator.SetInteger("Estado", (int)estadoActual);
        warningAnterior = AlgunStatBajo();
        zeroAnterior = AlgunStatEnCero();
    }

    void Update()
    {
        if (EstaMuerto) return;

        float dt = Time.deltaTime;
        hambre = Mathf.Clamp(hambre - gastoHambre * dt, 0f, 100f);
        energia = Mathf.Clamp(energia - gastoEnergia * dt, 0f, 100f);
        felicidad = Mathf.Clamp(felicidad - gastoFelicidad * dt, 0f, 100f);

        if (estadoActual == Estado.Durmiendo)
            energia = Mathf.Clamp(energia + recEnergiaPorSegundo * dt, 0f, 100f);

        if (estadoActual == Estado.Comiendo || estadoActual == Estado.Cazando ||
            estadoActual == Estado.Durmiendo || estadoActual == Estado.Acicalando)
        {
            tiempoAccion += dt;
            if (tiempoAccion >= duracionAccion)
                RecibirEntrada(Entrada.FinAnimacion);
        }

        bool warningAhora = AlgunStatBajo();
        bool zeroAhora = AlgunStatEnCero();

        if (zeroAhora && !zeroAnterior) RecibirEntrada(Entrada.Zero);
        else if (warningAhora && !warningAnterior)
        {
            RecibirEntrada(Entrada.Warning);
            if (arbitro != null) arbitro.NotificarWarning("Gato");
        }

        warningAnterior = warningAhora;
        zeroAnterior = zeroAhora;

        if (controlesActivos) LeerTeclado();
    }

    private void LeerTeclado()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.iKey.wasPressedThisFrame) RecibirEntrada(Entrada.Alimentar);
        else if (kb.jKey.wasPressedThisFrame) RecibirEntrada(Entrada.Jugar);
        else if (kb.kKey.wasPressedThisFrame) RecibirEntrada(Entrada.Dormir);
        else if (kb.lKey.wasPressedThisFrame) RecibirEntrada(Entrada.Cepillar);
    }

    private bool AlgunStatBajo() => hambre < umbralBajo || energia < umbralBajo || felicidad < umbralBajo;
    private bool AlgunStatEnCero() => hambre <= 0f || energia <= 0f || felicidad <= 0f;

    public void RecibirEntrada(Entrada entrada)
    {
        if (EstaMuerto) return;

        Estado siguiente = estadoActual;

        switch (estadoActual)
        {
            case Estado.Reposo:
                if (entrada == Entrada.Alimentar) siguiente = Estado.Comiendo;
                else if (entrada == Entrada.Jugar) siguiente = Estado.Cazando;
                else if (entrada == Entrada.Dormir) siguiente = Estado.Durmiendo;
                else if (entrada == Entrada.Cepillar) siguiente = Estado.Acicalando;
                else if (entrada == Entrada.Warning) siguiente = Estado.Arisco;
                else if (entrada == Entrada.Zero) siguiente = Estado.Muerto;
                break;
            case Estado.Arisco:
                if (entrada == Entrada.Alimentar) siguiente = Estado.Comiendo;
                else if (entrada == Entrada.Dormir) siguiente = Estado.Durmiendo;
                else if (entrada == Entrada.Cepillar) siguiente = Estado.Acicalando;
                else if (entrada == Entrada.Zero) siguiente = Estado.Muerto;
                break;
            case Estado.Comiendo:
            case Estado.Cazando:
            case Estado.Durmiendo:
            case Estado.Acicalando:
                if (entrada == Entrada.FinAnimacion) siguiente = Estado.Reposo;
                else if (entrada == Entrada.Zero) siguiente = Estado.Muerto;
                break;
        }

        if (siguiente != estadoActual)
            CambiarEstado(siguiente);
    }

    private void CambiarEstado(Estado nuevo)
    {
        estadoActual = nuevo;
        animator.SetInteger("Estado", (int)nuevo);
        tiempoAccion = 0f;

        if (nuevo == Estado.Comiendo || nuevo == Estado.Cazando || nuevo == Estado.Durmiendo || nuevo == Estado.Acicalando)
        {
            if (arbitro != null) arbitro.NotificarAccion("Gato", nuevo.ToString());
        }

        switch (nuevo)
        {
            case Estado.Comiendo: hambre = Mathf.Min(100f, hambre + recHambre); break;
            case Estado.Cazando: felicidad = Mathf.Min(100f, felicidad + recFelicidad); break;
            case Estado.Acicalando: felicidad = Mathf.Min(100f, felicidad + 20f); break; // Usar L ahora sube felicidad
        }
    }

    public void FinAnimacion() => RecibirEntrada(Entrada.FinAnimacion);
    public bool TieneStatCritico() => AlgunStatBajo();
    public bool TieneStatEnCero() => AlgunStatEnCero();
}