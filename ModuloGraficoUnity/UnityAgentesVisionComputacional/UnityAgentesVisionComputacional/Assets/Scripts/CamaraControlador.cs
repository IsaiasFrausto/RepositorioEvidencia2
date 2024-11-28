using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System.Text;

public class CamaraControlador : MonoBehaviour
{
    [Header("Cámaras")]
    public Camera feedCamera;

    [Header("Configuraciones")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 5000;
    public string cameraName = "Cámara 1";

    private RenderTexture renderTexture;
    private Texture2D texture2D;
    private TcpClient client;
    private NetworkStream stream;

    private bool isProcessingFrame = true; // Controlar el procesamiento
    private string lastStatus = "Esperando conexión...";

    void Start()
    {
        if (feedCamera == null || feedCamera.enabled)
        {
            Debug.LogError("La cámara debe estar asignada y deshabilitada.");
            return;
        }

        try
        {
            client = new TcpClient(serverIP, serverPort);
            stream = client.GetStream();
            renderTexture = new RenderTexture(feedCamera.pixelWidth, feedCamera.pixelHeight, 24);
            feedCamera.targetTexture = renderTexture;
            texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

            lastStatus = $"{cameraName}: Conectado al servidor.";
            UpdateCameraStatus();
        }
        catch (SocketException)
        {
            lastStatus = $"{cameraName}: Error de conexión.";
            UpdateCameraStatus();
        }
    }

    void Update()
    {
        if (client != null && client.Connected && isProcessingFrame)
        {
            StartCoroutine(CaptureAndSendFrame());
        }
    }

    IEnumerator CaptureAndSendFrame()
    {
        yield return new WaitForEndOfFrame();
        feedCamera.Render();

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
                    var confidence = ExtractConfidence(response);
                    SubastadorControlador.bidQueue.Enqueue((cameraName, confidence));
                    isProcessingFrame = false;
                    lastStatus = $"{cameraName}: Amenaza detectada ({confidence * 100:0.00}% confianza).";
                }
                else
                {
                    lastStatus = $"{cameraName}: {response}";
                }

                UpdateCameraStatus();
            }
        }
        catch (SocketException)
        {
            lastStatus = $"{cameraName}: Error de transmisión.";
            UpdateCameraStatus();
        }
    }

    private float ExtractConfidence(string response)
    {
        var split = response.Split(new[] { "Confianza:" }, System.StringSplitOptions.None);
        return split.Length > 1 ? float.Parse(split[1].Trim()) : 0f;
    }

    private void UpdateCameraStatus()
    {
        // Actualizar el estado en el diccionario compartido
        if (SubastadorControlador.cameraStatuses.ContainsKey(cameraName))
        {
            SubastadorControlador.cameraStatuses[cameraName] = lastStatus;
        }
        else
        {
            SubastadorControlador.cameraStatuses.Add(cameraName, lastStatus);
        }
    }

    public void ResetCamera()
    {
        isProcessingFrame = true; // Reactivar procesamiento
        lastStatus = $"{cameraName}: Lista y en espera.";
        UpdateCameraStatus();
    }

    void OnApplicationQuit()
    {
        if (client != null && client.Connected)
        {
            client.Close();
        }
    }

}
