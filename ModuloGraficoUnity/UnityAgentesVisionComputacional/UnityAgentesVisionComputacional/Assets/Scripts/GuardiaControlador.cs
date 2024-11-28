using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GuardiaControlador : MonoBehaviour
{
    private int detections = 0; // Veces que las c�maras detectaron amenazas
    private int guardAlerts = 0; // Veces que el dron detect� amenaza
    private int falseAlarms = 0; // Falsas alarmas por el guardia
    private int autoFalseAlarms = 0; // Falsas alarmas autom�ticas por el dron
    private float successRate = 0; // Porcentaje de �xito
    private bool showNotification = false;

    void Update()
    {
        if (showNotification)
        {
            if (Input.GetKeyDown(KeyCode.Y))
            {
                ResolveThreat(true);
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                ResolveThreat(false);
            }
        }
    }

    public void ReceiveNotification(bool isReal)
    {
        detections++; // Siempre incrementa cuando hay una notificaci�n
        if (isReal)
        {
            guardAlerts++; // Incrementar solo si el dron detect� amenaza
            UpdateStatistics();
        }
        else
        {
            autoFalseAlarms++; // Incrementar falsa alarma autom�tica
            UpdateStatistics();
        }
        showNotification = isReal;
    }

    private void ResolveThreat(bool resolvedAsReal)
    {
        if (!resolvedAsReal)
        {
            falseAlarms++; // Incrementar falsas alarmas por el guardia
        }
        UpdateStatistics();

        ResetCamerasAndAuctioneer();

        showNotification = false;
    }

    private void UpdateStatistics()
    {
        // C�lculo del porcentaje de �xito:
        // Se considera �xito cuando el dron o el guardia identifican correctamente una amenaza
        int totalAlerts = guardAlerts + autoFalseAlarms + falseAlarms;
        successRate = totalAlerts > 0
            ? ((float)(guardAlerts) / totalAlerts) * 100f
            : 0f;
    }

    private void ResetCamerasAndAuctioneer()
    {
        var cameras = FindObjectsByType<CamaraControlador>(FindObjectsSortMode.None);
        foreach (var camera in cameras)
        {
            camera.ResetCamera();
        }

        var auctioneer = FindFirstObjectByType<SubastadorControlador>();
        if (auctioneer != null)
        {
            auctioneer.ReceiveNotification();
        }

        Debug.Log("C�maras y subastador restablecidos por el guardia.");
    }

    void OnGUI()
    {
        if (showNotification)
        {
            GUI.Box(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 50, 200, 100), "Amenaza detectada");
            GUI.Label(new Rect(Screen.width / 2 - 90, Screen.height / 2 - 20, 200, 30), "Presiona 'Y' para solucionar");
            GUI.Label(new Rect(Screen.width / 2 - 90, Screen.height / 2, 200, 30), "Presiona 'N' para falsa alarma");
        }

        GUI.Box(new Rect(Screen.width - 210, Screen.height - 160, 200, 150), "Estad�sticas");
        GUI.Label(new Rect(Screen.width - 200, Screen.height - 140, 200, 20), $"Detecciones: {detections}");
        GUI.Label(new Rect(Screen.width - 200, Screen.height - 120, 200, 20), $"Alerta a guardia: {guardAlerts}");
        GUI.Label(new Rect(Screen.width - 200, Screen.height - 100, 200, 20), $"Falsas alarmas: {falseAlarms}");
        GUI.Label(new Rect(Screen.width - 200, Screen.height - 80, 200, 20), $"Falsa alarma autom�tica: {autoFalseAlarms}");
        GUI.Label(new Rect(Screen.width - 200, Screen.height - 60, 200, 20), $"�xito: {successRate:0.00}%");
    }
}
