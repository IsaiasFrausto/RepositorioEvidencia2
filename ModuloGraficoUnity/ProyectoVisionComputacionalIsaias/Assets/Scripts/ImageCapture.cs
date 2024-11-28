/*using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro; // Necesario para TextMeshPro

public class ImageCapture : MonoBehaviour
{
    public Camera cam;                     // Cámara asignada
    public string cameraName;             // Nombre único de la cámara
    public string serverUrl = "http://localhost:5000/detect"; // URL del servidor
    public RectTransform canvasRect;      // Canvas para las bounding boxes
    public GameObject boundingBoxPrefab;  // Prefab de las bounding boxes

    private List<GameObject> boundingBoxes = new List<GameObject>();
    private Dictionary<string, float> previousDetections = new Dictionary<string, float>(); // Objeto detectado -> confianza

    void Start()
    {
        RenderTexture renderTexture = new RenderTexture(416, 416, 16);
        cam.targetTexture = renderTexture;
        StartCoroutine(CaptureAndSend());
    }

    IEnumerator CaptureAndSend()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // Enviar cada 1 segundo

            RenderTexture rt = cam.targetTexture;
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            byte[] imageData = tex.EncodeToJPG();
            Destroy(tex);

            WWWForm form = new WWWForm();
            form.AddBinaryData("image", imageData, "image.jpg", "image/jpeg");

            using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    ProcessDetections(www.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"Error from {cameraName}: {www.error}");
                }
            }
        }
    }

    void ProcessDetections(string jsonResponse)
    {
        // Parsear el JSON recibido del servidor
        DetectionResponse detections = JsonUtility.FromJson<DetectionResponse>(jsonResponse);

        // Si no hay detecciones, no hacer nada
        if (detections.items.Count == 0)
        {
            return;
        }

        // Limpiar bounding boxes anteriores
        foreach (GameObject box in boundingBoxes)
        {
            Destroy(box);
        }
        boundingBoxes.Clear();

        // Detecciones actuales para comparar con las previas
        Dictionary<string, float> currentDetections = new Dictionary<string, float>();

        // Dibujar nuevas bounding boxes
        foreach (DetectionItem detection in detections.items)
        {
            DrawBoundingBox(detection);
            currentDetections[detection.label] = detection.confidence;

            // Si es una nueva detección
            if (!previousDetections.ContainsKey(detection.label))
            {
                Debug.Log($"Camera {cameraName} detected a new object: {detection.label} ({Mathf.RoundToInt(detection.confidence * 100)}%)");
            }
        }

        // Comparar detecciones anteriores con las actuales
        foreach (var prev in previousDetections)
        {
            if (!currentDetections.ContainsKey(prev.Key))
            {
                // El objeto ya no está en el rango de visión
                if (prev.Value > 0.75f)
                {
                    Debug.Log($"Camera {cameraName}: {prev.Key} was detected with high confidence ({Mathf.RoundToInt(prev.Value * 100)}%) but is no longer visible.");
                }
                else
                {
                    Debug.Log($"Camera {cameraName}: {prev.Key} might have been a false alarm (confidence: {Mathf.RoundToInt(prev.Value * 100)}%) and is no longer visible.");
                }
            }
        }

        // Actualizar el estado previo
        previousDetections = currentDetections;
    }

    void DrawBoundingBox(DetectionItem detection)
    {
        // Crear un nuevo objeto para la bounding box
        GameObject box = Instantiate(boundingBoxPrefab, canvasRect);
        boundingBoxes.Add(box);

        // Normalizar las coordenadas y tamaños según el tamaño del Canvas
        float normalizedX = detection.x / 416f * canvasRect.rect.width;
        float normalizedY = (416f - detection.y - detection.height) / 416f * canvasRect.rect.height; // Ajustar el eje Y
        float normalizedWidth = detection.width / 416f * canvasRect.rect.width;
        float normalizedHeight = detection.height / 416f * canvasRect.rect.height;

        // Configurar posición y tamaño
        RectTransform rect = box.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(normalizedX + normalizedWidth / 2, normalizedY - normalizedHeight / 2);
        rect.sizeDelta = new Vector2(normalizedWidth, normalizedHeight);

        // Configurar etiqueta con TextMeshProUGUI
        TextMeshProUGUI label = box.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = $"{detection.label} ({Mathf.RoundToInt(detection.confidence * 100)}%)";
        }
    }

    [System.Serializable]
    public class DetectionResponse
    {
        public List<DetectionItem> items;
    }

    [System.Serializable]
    public class DetectionItem
    {
        public string label;
        public float confidence;
        public float x, y, width, height;
    }
}*/

using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro; // Necesario para TextMeshPro

public class ImageCapture : MonoBehaviour
{
    public Camera cam;                     // Cámara asignada
    public string cameraName;             // Nombre único de la cámara
    public string serverUrl = "http://localhost:5000/detect"; // URL del servidor
    public RectTransform canvasRect;      // Canvas para las bounding boxes
    public GameObject boundingBoxPrefab;  // Prefab de las bounding boxes

    private List<GameObject> boundingBoxes = new List<GameObject>();
    private Dictionary<string, float> previousDetections = new Dictionary<string, float>(); // Objeto detectado -> confianza

    void Start()
    {
        RenderTexture renderTexture = new RenderTexture(416, 416, 16);
        cam.targetTexture = renderTexture;
        StartCoroutine(CaptureAndSend());
    }

    IEnumerator CaptureAndSend()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f); // Enviar cada 1 segundo

            RenderTexture rt = cam.targetTexture;
            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            byte[] imageData = tex.EncodeToJPG();
            Destroy(tex);

            WWWForm form = new WWWForm();
            form.AddBinaryData("image", imageData, "image.jpg", "image/jpeg");

            using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    ProcessDetections(www.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"Error from {cameraName}: {www.error}");
                }
            }
        }
    }

    void ProcessDetections(string jsonResponse)
    {
        // Parsear el JSON recibido del servidor
        DetectionResponse detections = JsonUtility.FromJson<DetectionResponse>(jsonResponse);

        // Si no hay detecciones, no hacer nada
        if (detections.items.Count == 0)
        {
            return;
        }

        // Limpiar bounding boxes anteriores
        foreach (GameObject box in boundingBoxes)
        {
            Destroy(box);
        }
        boundingBoxes.Clear();

        // Detecciones actuales para comparar con las previas
        Dictionary<string, float> currentDetections = new Dictionary<string, float>();

        // Dibujar nuevas bounding boxes
        foreach (DetectionItem detection in detections.items)
        {
            DrawBoundingBox(detection);
            currentDetections[detection.label] = detection.confidence;

            // Si es una nueva detección
            if (!previousDetections.ContainsKey(detection.label))
            {
                Debug.Log($"Camera {cameraName} detected a new object: {detection.label} ({Mathf.RoundToInt(detection.confidence * 100)}%)");
            }
        }

        // Comparar detecciones anteriores con las actuales
        foreach (var prev in previousDetections)
        {
            if (!currentDetections.ContainsKey(prev.Key))
            {
                // El objeto ya no está en el rango de visión
                if (prev.Value > 0.75f)
                {
                    Debug.Log($"Camera {cameraName}: {prev.Key} was detected with high confidence ({Mathf.RoundToInt(prev.Value * 100)}%) but is no longer visible.");
                }
                else
                {
                    Debug.Log($"Camera {cameraName}: {prev.Key} might have been a false alarm (confidence: {Mathf.RoundToInt(prev.Value * 100)}%) and is no longer visible.");
                }
            }
        }

        // Actualizar el estado previo
        previousDetections = currentDetections;
    }

    void DrawBoundingBox(DetectionItem detection)
    {
        // Crear un nuevo objeto para la bounding box
        GameObject box = Instantiate(boundingBoxPrefab, canvasRect);
        boundingBoxes.Add(box);

        // Normalizar las coordenadas y tamaños según el tamaño del Canvas
        float normalizedX = detection.x / 416f * canvasRect.rect.width;
        float normalizedY = (416f - detection.y - detection.height) / 416f * canvasRect.rect.height; // Ajustar el eje Y
        float normalizedWidth = detection.width / 416f * canvasRect.rect.width;
        float normalizedHeight = detection.height / 416f * canvasRect.rect.height;

        // Configurar posición y tamaño
        RectTransform rect = box.GetComponent<RectTransform>();
        rect.localRotation = Quaternion.identity; // Corregir rotación
        rect.localScale = Vector3.one;           // Asegurar escala
        rect.anchoredPosition = new Vector2(normalizedX + normalizedWidth / 2, normalizedY - normalizedHeight / 2);
        rect.sizeDelta = new Vector2(normalizedWidth, normalizedHeight);

        // Configurar etiqueta con TextMeshProUGUI
        TextMeshProUGUI label = box.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = $"{detection.label} ({Mathf.RoundToInt(detection.confidence * 100)}%)";
        }
    }

    [System.Serializable]
    public class DetectionResponse
    {
        public List<DetectionItem> items;
    }

    [System.Serializable]
    public class DetectionItem
    {
        public string label;
        public float confidence;
        public float x, y, width, height;
    }
}

