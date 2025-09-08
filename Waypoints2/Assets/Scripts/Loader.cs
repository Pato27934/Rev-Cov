using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TrafficLoader : MonoBehaviour
{
    [Tooltip("Ruta relativa dentro de Assets (por ejemplo: 'Data/waypoints.csv')")]
    public string relativeFilePath = "Data/waypoints.csv";

    [Tooltip("Milisegundos de espera real que equivalen a 15 min simulados")]
    public int miliSecondsDelay = 30000;
    

    public List<TrafficEvent> eventos = new List<TrafficEvent>();

    void Awake()
    {
        LoadTrafficData();
    }

    public void LoadTrafficData()
    {
        eventos.Clear();

        string fullPath = Path.Combine(Application.dataPath, relativeFilePath);
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[TrafficLoader] CSV no encontrado en: {fullPath}");
            return;
        }

        var lines = File.ReadAllLines(fullPath);
        for (int i = 1; i < lines.Length; i++) // saltar encabezado
        {
            var cols = lines[i].Split(',');
            if (cols.Length < 6)
            {
                Debug.LogWarning($"[TrafficLoader] Línea inválida #{i+1}: {lines[i]}");
                continue;
            }

            var te = new TrafficEvent(
                cols[0].Trim(),    // hora "HH:MM"
                cols[1].Trim(),    // ruta
                int.Parse(cols[2]),// cantidad total
                int.Parse(cols[3]),// % autos
                int.Parse(cols[4]),// % camiones
                int.Parse(cols[5]) // % motos
            );

            eventos.Add(te);
            Debug.Log($"[TrafficLoader] Cargado: {cols[0].Trim()} | {cols[1].Trim()} | Cant: {cols[2].Trim()}");
        }
    }
}
