using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SubastadorControlador : MonoBehaviour
{
    // Cola de apuestas (nombre de la cámara y confianza)
    public static Queue<(string cameraName, float confidence)> bidQueue = new Queue<(string, float)>();

    // Estado actual del subastador
    public static string auctionStatus = "En espera";

    // Tiempo para recolectar apuestas
    public float bidCollectionTime = 3f;

    // Diccionario compartido para el estado de cámaras
    public static Dictionary<string, string> cameraStatuses = new Dictionary<string, string>();

    // Almacén temporal de apuestas recolectadas
    private Dictionary<string, float> collectedBids = new Dictionary<string, float>();

    void Update()
    {
        // Si hay apuestas y el subastador está en espera, inicia la subasta
        if (bidQueue.Count > 0 && auctionStatus == "En espera")
        {
            StartCoroutine(ConductAuction());
        }
    }

    private IEnumerator ConductAuction()
    {
        auctionStatus = "Recolectando apuestas";
        Debug.Log(auctionStatus);

        float startTime = Time.time;

        // Recolectar apuestas durante el tiempo definido
        while (Time.time - startTime < bidCollectionTime)
        {
            while (bidQueue.Count > 0)
            {
                var bid = bidQueue.Dequeue();
                collectedBids[bid.cameraName] = bid.confidence;
            }
            yield return null;
        }

        // Determinar el ganador de la subasta
        if (collectedBids.Count > 0)
        {
            var winner = DetermineWinner();
            auctionStatus = $"Cámara {winner.Key}: ganó la subasta con {winner.Value * 100:0.00}% de confianza";
            Debug.Log(auctionStatus);

            // Notificar al dron
            NotifyDrone(winner);
        }
        else
        {
            auctionStatus = "Ninguna cámara participó en la subasta.";
            Debug.Log(auctionStatus);
        }

        // Limpiar las apuestas recolectadas
        collectedBids.Clear();

        // Ya no se vuelve al estado de "En espera" automáticamente aquí
    }

    public void ReceiveNotification()
    {
        // Limpiar las apuestas recolectadas para evitar subastas no deseadas
        collectedBids.Clear();
        bidQueue.Clear();

        // Cambiar el estado del subastador a "En espera"
        auctionStatus = "En espera";

        Debug.Log("El subastador ha recibido la notificación de que las amenazas fueron resueltas.");
    }

    public void ResetCameras()
    {
        var cameras = FindObjectsByType<CamaraControlador>(FindObjectsSortMode.None);
        foreach (var camera in cameras)
        {
            camera.ResetCamera();
        }

        Debug.Log("Cámaras restablecidas por el subastador.");
    }


    private KeyValuePair<string, float> DetermineWinner()
    {
        float maxConfidence = float.MinValue;
        string winnerCamera = null;

        foreach (var bid in collectedBids)
        {
            if (bid.Value > maxConfidence)
            {
                maxConfidence = bid.Value;
                winnerCamera = bid.Key;
            }
        }

        return new KeyValuePair<string, float>(winnerCamera, maxConfidence);
    }

    private void NotifyDrone(KeyValuePair<string, float> winner)
    {
        var dron = FindFirstObjectByType<DronControlador>(); // Usar el nuevo método recomendado
        if (dron != null)
        {
            dron.ReceiveAlert(); // Notificar al dron que inicie la misión
            Debug.Log($"Dron notificado por Cámara {winner.Key} con {winner.Value * 100:0.00}% de confianza.");
        }
        else
        {
            Debug.LogWarning("No se encontró un DronControlador en la escena.");
        }
    }


    void OnGUI()
    {
        float boxWidth = 300f;
        float boxHeight = 100f;
        float padding = 10f;

        // Crear una caja para el estado del subastador
        GUILayout.BeginArea(new Rect(10, Screen.height - (boxHeight / 2 + padding), boxWidth, boxHeight / 2), GUI.skin.box);
        GUILayout.Label("Estado del Subastador:");
        GUILayout.Label(auctionStatus);
        GUILayout.EndArea();

        // Crear una caja para el estado de las cámaras
        GUILayout.BeginArea(new Rect(10, Screen.height - (2 * boxHeight + 2 * padding), boxWidth, boxHeight * 1.5f), GUI.skin.box);
        GUILayout.Label("Estado de las Cámaras:");

        // Crear un área de scroll para mostrar los estados de todas las cámaras
        Vector2 scrollPosition = Vector2.zero;
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(280), GUILayout.Height(120));
        foreach (var entry in cameraStatuses)
        {
            GUILayout.Label($"{entry.Key}: {entry.Value}");
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
