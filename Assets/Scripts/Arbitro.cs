using UnityEngine;
using UnityEngine.InputSystem;

public class Arbitro : MonoBehaviour
{
    public enum EstadoArbitro
    {
        Inicio = 0,
        Estable = 1,
        Alerta = 2,
        GameOver = 3
    }

    public enum EntradaArbitro
    {
        Start,
        Estable,
        Alerta,
        Muerte
    }

    [Header("Personajes")]
    public Perro perro;
    public Gato gato;

    [Header("Solo lectura")]
    public EstadoArbitro estadoActual = EstadoArbitro.Inicio;
    
    private float tickTimer = 0f;

    void Start()
    {
        if (perro) perro.controlesActivos = false;
        if (gato) gato.controlesActivos = false;
        Debug.Log("Árbitro: Esperando que presionen ESPACIO para iniciar la simulación.");
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        bool startPresionado = kb.spaceKey.wasPressedThisFrame;
        bool hayMuerto = (perro != null && perro.TieneStatEnCero()) || (gato != null && gato.TieneStatEnCero());
        bool hayCritico = (perro != null && perro.TieneStatCritico()) || (gato != null && gato.TieneStatCritico());
        bool todosOk = !hayMuerto && !hayCritico;

        if (estadoActual == EstadoArbitro.Inicio && startPresionado)
            RecibirEntrada(EntradaArbitro.Start);

        if (estadoActual != EstadoArbitro.Inicio && estadoActual != EstadoArbitro.GameOver)
        {
            if (hayMuerto) RecibirEntrada(EntradaArbitro.Muerte);
            else if (hayCritico) RecibirEntrada(EntradaArbitro.Alerta);
            else if (todosOk) RecibirEntrada(EntradaArbitro.Estable);

            // El Arbitro reporta el estado de las mascotas cada tick
            tickTimer += Time.deltaTime;
            if (tickTimer >= 1f)
            {
                tickTimer = 0f;
                string estadoP = perro != null ? perro.estadoActual.ToString() : "N/A";
                string estadoG = gato != null ? gato.estadoActual.ToString() : "N/A";
                Debug.Log($"[Tick] Estado actual -> Perro: {estadoP} | Gato: {estadoG}");
            }
        }
    }

    public void RecibirEntrada(EntradaArbitro entrada)
    {
        EstadoArbitro siguiente = estadoActual;

        switch (estadoActual)
        {
            case EstadoArbitro.Inicio:
                if (entrada == EntradaArbitro.Start) siguiente = EstadoArbitro.Estable;
                break;
            case EstadoArbitro.Estable:
                if (entrada == EntradaArbitro.Alerta) siguiente = EstadoArbitro.Alerta;
                else if (entrada == EntradaArbitro.Muerte) siguiente = EstadoArbitro.GameOver;
                break;
            case EstadoArbitro.Alerta:
                if (entrada == EntradaArbitro.Estable) siguiente = EstadoArbitro.Estable;
                else if (entrada == EntradaArbitro.Muerte) siguiente = EstadoArbitro.GameOver;
                break;
            case EstadoArbitro.GameOver:
                break;
        }

        if (siguiente != estadoActual)
            CambiarEstado(siguiente);
    }

    private void CambiarEstado(EstadoArbitro nuevo)
    {
        estadoActual = nuevo;

        switch (nuevo)
        {
            case EstadoArbitro.Estable:
                if (perro) perro.controlesActivos = true;
                if (gato) gato.controlesActivos = true;
                Debug.Log("Árbitro: Entorno estable. Las mascotas están bien.");
                break;

            case EstadoArbitro.Alerta:
                if (perro) perro.controlesActivos = true;
                if (gato) gato.controlesActivos = true;
                break;

            case EstadoArbitro.GameOver:
                if (perro) perro.controlesActivos = false;
                if (gato) gato.controlesActivos = false;
                Debug.Log("Árbitro: GAME OVER. Se bloquean los controles.");
                break;
        }
    }

    // Métodos para que las mascotas reporten sus acciones
    public void NotificarWarning(string nombreMascota)
    {
        Debug.Log($"Árbitro: ALERTA! {nombreMascota} ha reportado estadísticas bajas.");
    }

    public void NotificarAccion(string nombreMascota, string accion)
    {
        Debug.Log($"Árbitro: El jugador ha mandado al {nombreMascota} a estado: {accion}.");
    }
}