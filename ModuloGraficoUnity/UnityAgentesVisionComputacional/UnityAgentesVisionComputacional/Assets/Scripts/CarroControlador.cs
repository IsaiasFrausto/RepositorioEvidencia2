using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarroControlador : MonoBehaviour
{
    public List<GameObject> carPrefabs; // Lista de prefabs de carros
    public Transform spawnPoint; // Punto de inicio del camino
    public Transform endPoint; // Punto final del camino
    public float carSpeed = 5f; // Velocidad del carro
    public float rotationSpeed = 5f; // Velocidad de rotación del carro

    private GameObject currentCar; // Referencia al carro actual

    private void Update()
    {
        // Detectar si se presiona la tecla 'P' y no hay un carro actual
        if (Input.GetKeyDown(KeyCode.P) && currentCar == null)
        {
            GenerateCar();
        }

        // Detectar si se presiona la tecla 'K' para eliminar el carro actual
        if (Input.GetKeyDown(KeyCode.K) && currentCar != null)
        {
            DestroyCar();
        }
    }

    private void OnGUI()
    {
        // Mostrar el mensaje en la GUI
        float screenWidth = Screen.width;
        GUILayout.BeginArea(new Rect(screenWidth - 210, 10, 200, 60), GUI.skin.box);
        GUILayout.Label("Presiona 'P' para generar carro");
        GUILayout.Label("Presiona 'K' para eliminar carro");
        GUILayout.EndArea();
    }

    private void GenerateCar()
    {
        if (carPrefabs.Count == 0)
        {
            Debug.LogError("No hay modelos de carros en la lista de prefabs.");
            return;
        }

        // Seleccionar un modelo de carro aleatoriamente
        int randomIndex = Random.Range(0, carPrefabs.Count);
        GameObject carPrefab = carPrefabs[randomIndex];

        // Instanciar el carro en el punto de inicio
        currentCar = Instantiate(carPrefab, spawnPoint.position, spawnPoint.rotation);

        // Iniciar el movimiento del carro
        StartCoroutine(MoveCar(currentCar));
    }

    private IEnumerator MoveCar(GameObject car)
    {
        while (car != null && Vector3.Distance(car.transform.position, endPoint.position) > 0.2f)
        {
            // Dirección hacia el punto final
            Vector3 direction = (endPoint.position - car.transform.position).normalized;

            // Mover el carro hacia el punto final
            car.transform.position = Vector3.MoveTowards(
                car.transform.position,
                endPoint.position,
                Mathf.Min(carSpeed * Time.deltaTime, Vector3.Distance(car.transform.position, endPoint.position))
            );

            // Calcular la rotación hacia el punto final
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                if (Quaternion.Angle(car.transform.rotation, targetRotation) > 1f)
                {
                    car.transform.rotation = Quaternion.Slerp(car.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }

            // Esperar hasta el siguiente frame
            yield return null;
        }

        // Destruir el carro al llegar al final del camino
        if (car != null)
        {
            Destroy(car);
            currentCar = null; // Permitir generar un nuevo carro
        }
    }

    private void DestroyCar()
    {
        if (currentCar != null)
        {
            Destroy(currentCar);
            currentCar = null; // Liberar la referencia al carro
            Debug.Log("Carro eliminado manualmente.");
        }
    }
}
