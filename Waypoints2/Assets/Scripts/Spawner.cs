using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrafficSpawner : MonoBehaviour
{
    [Header("Loader y prefabs")]
    public TrafficLoader loader;         // Objeto Loader en escena
    public GameObject autoPrefab;
    public GameObject camionPrefab;
    public GameObject motoPrefab;

    [Header("Rutas en escena")]
    public Transform[] rutas;            // Objetos vacíos (Ruta1, Ruta2, ...)

    // Estado interno
    private int currentMinute;           // Tiempo simulado en minutos desde 00:00
    private int nextEventIndex = 0;      // Índice al siguiente evento en loader.eventos
    private float spawnTime;             // Retraso real en seg. que equivale a 15 min simulados

    void Awake()
    {
        // 1. Carga y ordena eventos
        loader.LoadTrafficData();
        loader.eventos.Sort((a, b) => a.timeInMinutes.CompareTo(b.timeInMinutes));

        // 2. Hora inicial simulada (08:00)
        currentMinute = 8 * 60;
    }

    void Start()
    {
        // Queremos que cada 15 minutos simulados sean 60 segundos reales
        int segundosRealesPorTick = 60;
        spawnTime = segundosRealesPorTick;

        Debug.Log($"[Spawner] spawnTime = {spawnTime} segundos por tick");
        StartCoroutine(SpawnTrafficCoroutine());
    }

    IEnumerator SpawnTrafficCoroutine()
    {
        while (nextEventIndex < loader.eventos.Count)
        {
            TrySpawnForCurrentTime();

            // Avanza 15 minutos simulados
            currentMinute += 15;

            yield return new WaitForSeconds(spawnTime);
        }
    }

    void TrySpawnForCurrentTime()
    {
        int hh = currentMinute / 60;
        int mm = currentMinute % 60;
        Debug.Log($"[Spawner] Hora simulada: {hh:D2}:{mm:D2}");

        var events = loader.eventos;
        while (nextEventIndex < events.Count &&
               events[nextEventIndex].timeInMinutes == currentMinute)
        {
            SpawnForEvent(events[nextEventIndex]);
            nextEventIndex++;
        }
    }

    void SpawnForEvent(TrafficEvent e)
    {
        int autos    = e.cantidad * e.autoPct   / 100;
        int camiones = e.cantidad * e.camionPct / 100;
        int motos    = e.cantidad - autos - camiones;

        Transform ruta = GetRutaTransform(e.ruta);
        if (ruta == null)
        {
            Debug.LogWarning($"[Spawner] Ruta no encontrada: {e.ruta}");
            return;
        }

        SpawnVehicles(ruta, autoPrefab, autos);
        SpawnVehicles(ruta, camionPrefab, camiones);
        SpawnVehicles(ruta, motoPrefab, motos);
    }

    void SpawnVehicles(Transform ruta, GameObject prefab, int cantidad)
    {
        if (prefab == null) return;

        for (int i = 0; i < cantidad; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-2f, 2f),
                0f,
                Random.Range(-2f, 2f)
            );

            var go = Instantiate(prefab, ruta.position + offset, ruta.rotation);

            // 🔹 Configuración del AICarScript
            var ai = go.GetComponent<AICarScript>();
            if (ai != null)
            {
                ai.pathGroup = ruta.gameObject;  // El objeto "RutaX"
                //ai.GetPath();                   // Carga sus waypoints
                ai.velocity = Random.Range(20f, 40f); // Velocidad aleatoria
            }

            Debug.Log($"[Spawner] Instanciado {go.name} en {ruta.name}");
        }
    }

    Transform GetRutaTransform(string rutaName)
    {
        foreach (var t in rutas)
        {
            if (t != null && t.name.Trim() == rutaName.Trim())
                return t;
        }
        return null;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (rutas == null) return;
        foreach (var r in rutas)
            if (r != null)
                Gizmos.DrawSphere(r.position, 0.5f);
    }
}
