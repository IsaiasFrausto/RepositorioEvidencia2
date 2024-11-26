import sys
import socket
import logging
import cv2
import numpy as np
from ultralytics import YOLO
from threading import Thread, Event as ThreadEvent

# Cargar modelo YOLO
model = YOLO('yolov8s.pt')

# Clases a considerar como amenaza
CLASSES_DE_AMENAZA = ['car', 'bus', 'truck']

def procesar_frame(buffer):
    """ Procesar el frame para detectar autobuses o carros. """
    nparr = np.frombuffer(buffer, np.uint8)
    img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)

    # Realizar detección con YOLO
    results = model.predict(img)
    detecciones = results[0].boxes

    # Filtrar clases de amenaza
    for box in detecciones:
        class_id = int(box.cls[0])  # Obtener ID de la clase
        class_name = model.names[class_id]  # Obtener nombre de la clase
        if class_name in CLASSES_DE_AMENAZA:
            return f"Amenaza detectada: {class_name}"
    
    return "Área despejada"

def handle_client(client_socket, addr):
    """ Manejar la conexión con un cliente. """
    logger = logging.getLogger("handle_client")
    logger.info(f"Cliente conectado: {addr}")

    while True:
        try:
            # Recibir el tamaño del frame en bytes (7 bytes máximo)
            data = client_socket.recv(7)
            if not data:
                break

            # Decodificar el tamaño usando UTF-8 y convertirlo a entero
            try:
                data_len = int(data.decode('utf-8').strip())
            except ValueError:
                logger.error(f"Error decodificando tamaño del frame: {data}")
                break

            buffer = b''

            # Leer el resto de los datos hasta completar el tamaño esperado
            while len(buffer) < data_len:
                fragment = client_socket.recv(data_len - len(buffer))
                if not fragment:
                    break
                buffer += fragment

            # Procesar el frame recibido
            alerta = procesar_frame(buffer)
            logger.info(f"Alerta procesada: {alerta}")
            print(f"Alerta procesada: {alerta}")

            # Enviar la respuesta al cliente
            client_socket.sendall(alerta.encode('utf-8'))

        except Exception as e:
            logger.error(f"Error manejando cliente {addr}: {e}")
            break

    client_socket.close()
    logger.info(f"Cliente desconectado: {addr}")

def socket_server(exit_event):
    """ Manejo del servidor principal. """
    logger = logging.getLogger("socket_server")

    HOST = '127.0.0.1'
    PORT = 5000

    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server_socket.bind((HOST, PORT))
    server_socket.listen(5)
    server_socket.settimeout(1)  # Timeout para evitar bloqueos indefinidos

    logger.info(f"Servidor escuchando en {HOST}:{PORT}")
    threads = []

    try:
        while not exit_event.is_set():
            try:
                client_socket, addr = server_socket.accept()
                thread = Thread(target=handle_client, args=(client_socket, addr))
                threads.append(thread)
                thread.start()
            except socket.timeout:
                continue
    except KeyboardInterrupt:
        logger.info("Interrupción manual detectada. Cerrando el servidor...")
        exit_event.set()

    # Cerrar el socket del servidor
    logger.info("Cerrando servidor...")
    server_socket.close()

    # Esperar a que todos los hilos terminen
    for thread in threads:
        thread.join()

    logger.info("Servidor completamente cerrado.")

if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)

    exit_event = ThreadEvent()

    # Iniciar el servidor en un hilo separado
    server_thread = Thread(target=socket_server, args=(exit_event,))
    server_thread.start()

    try:
        while True:
            # Esperar la entrada del usuario para cerrar el servidor
            if input("Presiona 'q' para cerrar el servidor:\n").strip().lower() == 'q':
                exit_event.set()
                break
    except KeyboardInterrupt:
        exit_event.set()

    # Esperar a que el servidor termine
    server_thread.join()

    # Destruir ventanas de OpenCV
    cv2.destroyAllWindows()
    logging.info("Aplicación cerrada correctamente.")
