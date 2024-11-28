using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Linq;

public class DronControlador : MonoBehaviour
{
    [Header("Cámara del Dron")]
    public Camera droneCamera;

    [Header("Configuraciones")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 5000;

    [Header("Movimiento del Dron")]
    public Transform[] waypoints; // Puntos del recorrido
    public float speed = 5f;
    private int currentWaypointIndex = 0;

    private Vector3 stationPosition; // Posición inicial del dron
    private bool returningToStation = false; // Indica si el dron está regresando a la estación

    private RenderTexture renderTexture;
    private Texture2D texture2D;
    private TcpClient client;
    private NetworkStream stream;

    private string lastStatus = "Esperando alerta...";
    private bool isProcessingFrame = false; // Controla si el dron está enviando video al servidor
    private bool isOnMission = false;     // Controla si el dron está en una misión activa
    private bool threatDetected = false; // Indica si ya se detectó una amenaza

    void Start()
    {
        try
        {
            client = new TcpClient(serverIP, serverPort);
            stream = client.GetStream();
            renderTexture = new RenderTexture(droneCamera.pixelWidth, droneCamera.pixelHeight, 24);
            droneCamera.targetTexture = renderTexture;
            texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

            // Guardar la posición inicial como estación
            stationPosition = transform.position;

            lastStatus = "Dron conectado al servidor.";
        }
        catch (SocketException)
        {
            lastStatus = "Error al conectar al servidor.";
        }
    }

    void Update()
    {
        if (isOnMission)
        {
            if (!returningToStation)
            {
                MoveAlongPath();
            }
            else
            {
                ReturnToStation();
            }

            if (client != null && client.Connected && !isProcessingFrame && isOnMission && !threatDetected)
            {
                StartCoroutine(CaptureAndSendFrame());
            }
        }
    }

    private void MoveAlongPath()
    {
        if (currentWaypointIndex < waypoints.Length)
        {
            Transform targetWaypoint = waypoints[currentWaypointIndex];
            transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, speed * Time.deltaTime);

            Vector3 direction = (targetWaypoint.position - transform.position).normalized;
            if (direction != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(direction);

            if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.2f)
            {
                currentWaypointIndex++;
            }
        }
        else
        {
            returningToStation = true;
            lastStatus = "Dron: Regresando a la estación. " + (threatDetected ? "Amenaza detectada." : "No se detectaron amenazas.");
        }
    }

    private void ReturnToStation()
    {
        transform.position = Vector3.MoveTowards(transform.position, stationPosition, speed * Time.deltaTime);

        Vector3 direction = (stationPosition - transform.position).normalized;
        if (direction != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(direction);

        if (Vector3.Distance(transform.position, stationPosition) < 0.2f)
        {
            EndMission();
        }
    }

    IEnumerator CaptureAndSendFrame()
    {
        isProcessingFrame = true;
        yield return new WaitForEndOfFrame();

        droneCamera.Render();

        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();
        RenderTexture.active = null;

        byte[] imageBytes = texture2D.EncodeToJPG();

        try
        {
            if (stream.CanWrite)
            {
                string imageSizeString = imageBytes.Length.ToString("D7");
                byte[] sizeBytes = Encoding.UTF8.GetBytes(imageSizeString);
                stream.Write(sizeBytes, 0, sizeBytes.Length);
                stream.Write(imageBytes, 0, imageBytes.Length);

                byte[] buffer = new byte[256];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (response.Contains("Amenaza detectada"))
                {
                    lastStatus = "Dron: Amenaza detectada. Continuando recorrido.";
                    threatDetected = true;
                }
                else
                {
                    lastStatus = "Dron: " + response;
                }
            }
        }
        catch (SocketException)
        {
            lastStatus = "Error de transmisión del dron.";
        }

        isProcessingFrame = false;
    }

    private void EndMission()
    {
        isOnMission = false;
        currentWaypointIndex = 0;
        returningToStation = false;

        var guard = FindFirstObjectByType<GuardiaControlador>();
        if (guard != null)
        {
            Debug.Log("Se notifica al guardia como " + threatDetected);
            guard.ReceiveNotification(threatDetected);
        }

        if (!threatDetected)
        {
            ResetCamerasAndAuctioneer();
        }

        threatDetected = false;
    }

    private void ResetCamerasAndAuctioneer()
    {
        List<string> cameraKeys = new List<string>(SubastadorControlador.cameraStatuses.Keys);
        foreach (var cameraKey in cameraKeys)
        {
            SubastadorControlador.cameraStatuses[cameraKey] = $"{cameraKey}: Restablecida y lista.";
            var camera = FindObjectsOfType<CamaraControlador>().FirstOrDefault(c => c.cameraName == cameraKey);
            if (camera != null)
            {
                camera.ResetCamera();
            }
        }

        var auctioneer = FindFirstObjectByType<SubastadorControlador>();
        if (auctioneer != null)
        {
            auctioneer.ReceiveNotification();
        }

        Debug.Log("Cámaras y subastador restablecidos por el dron.");
    }

    public void ReceiveAlert()
    {
        if (!isOnMission)
        {
            isOnMission = true;
            currentWaypointIndex = 0;
            returningToStation = false;
            lastStatus = "Dron: Misión iniciada. Despegando.";
        }
    }

    void OnGUI()
    {
        float boxWidth = 300f;
        float boxHeight = 100f;
        float padding = 10f;

        // Crear una caja para el estado del dron
        GUILayout.BeginArea(new Rect(10, Screen.height - 3 * (boxHeight + padding), boxWidth, boxHeight), GUI.skin.box);
        GUILayout.Label("Estado del Dron:");
        GUILayout.Label(lastStatus);
        GUILayout.EndArea();
    }

    void OnApplicationQuit()
    {
        if (client != null && client.Connected)
        {
            client.Close();
        }
    }
}
