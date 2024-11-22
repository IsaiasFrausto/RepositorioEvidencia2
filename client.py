import socket
import cv2
import time
from agents import VigilanciaModel

class CamaraCliente:
    def __init__(self, video_path, server_host='127.0.0.1', server_port=5000):
        self.video_path = video_path
        self.server_host = server_host
        self.server_port = server_port
        self.client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    def conectar(self):
        self.client_socket.connect((self.server_host, self.server_port))
        
    def enviar_frame(self, frame):
        """ Codifica y envía un frame al servidor y recibe la respuesta """
        _, encoded_image = cv2.imencode('.jpg', frame)
        image_data = encoded_image.tobytes()

        # Codificar el tamaño como una cadena y enviar
        data_len = f"{len(image_data):07}".encode('utf-8')  # 7 dígitos, rellenados con ceros
        self.client_socket.sendall(data_len)
        self.client_socket.sendall(image_data)

        # Recibir respuesta del servidor
        respuesta = self.client_socket.recv(1024).decode('utf-8')
        return respuesta

    def procesar_video(self):
        """ Procesa un video cuadro por cuadro """
        cap = cv2.VideoCapture(self.video_path)
        amenaza_detectada = False

        while cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                break

            # Enviar el frame al servidor para análisis
            respuesta = self.enviar_frame(frame)
            print(f"Servidor respondió: {respuesta}")

            if "Amenaza detectada" in respuesta:
                amenaza_detectada = True
                print("Cámara: Amenaza detectada. Alertando al Dron.")
                break

        cap.release()
        return amenaza_detectada

class DronCliente:
    def __init__(self, video_path, server_host='127.0.0.1', server_port=5000):
        self.video_path = video_path
        self.server_host = server_host
        self.server_port = server_port
        self.client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    def conectar(self):
        self.client_socket.connect((self.server_host, self.server_port))

    def enviar_frame(self, frame):
        """ Codifica y envía un frame al servidor y recibe la respuesta """
        _, encoded_image = cv2.imencode('.jpg', frame)
        image_data = encoded_image.tobytes()

        # Codificar el tamaño como una cadena y enviar
        data_len = f"{len(image_data):07}".encode('utf-8')  # 7 dígitos, rellenados con ceros
        self.client_socket.sendall(data_len)
        self.client_socket.sendall(image_data)

        # Recibir respuesta del servidor
        respuesta = self.client_socket.recv(1024).decode('utf-8')
        return respuesta


    def patrullar(self):
        """ Simula un patrullaje de 20 segundos analizando un video """
        cap = cv2.VideoCapture(self.video_path)
        amenaza_detectada = False
        start_time = time.time()

        while cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                break

            # Enviar el frame al servidor para análisis
            respuesta = self.enviar_frame(frame)
            print(f"Dron recibió respuesta del servidor: {respuesta}")

            if "Amenaza detectada" in respuesta:
                amenaza_detectada = True
                print("Dron: Amenaza detectada. Alertando al Guardia.")
                break

            # Termina si pasan 20 segundos
            if time.time() - start_time > 20:
                print("Dron: Patrullaje completado. No se detectó amenaza.")
                break

        cap.release()
        return amenaza_detectada

class Guardia:
    def validar_alerta(self, alerta_dron):
        """ Valida manualmente si la alarma fue real o falsa """
        print(f"Guardia: Validando alerta del Dron: {alerta_dron}")
        decision = input("¿La alarma es real? (Y/n): ").strip().lower()
        if decision == "y":
            print("Guardia: Alarma confirmada como REAL.")
        else:
            print("Guardia: Alarma marcada como FALSA.")

# Simulación del sistema
if __name__ == "__main__":
    # Modelo de vigilancia (simula los agentes)
    model = VigilanciaModel()
    model.setup()

    # Cámara procesando un video inicial"C:\\Users\\luisd\\Downloads\\Isaias - Vision\\AgentesPython\\videos\\carretera.mp4"
    camara = CamaraCliente(video_path="C:\\Users\\luisd\\Downloads\\Isaias - Vision\\AgentesPython\\videos\\carros.mp4")
    camara.conectar()
    amenaza_detectada = camara.procesar_video()

    if amenaza_detectada:
        # Alertar al Dron
        print("Cámara: Amenaza detectada. Pasando alerta al Dron.")
        dron = DronCliente(video_path="C:\\Users\\luisd\\Downloads\\Isaias - Vision\\AgentesPython\\videos\\carretera.mp4")
        dron.conectar()
        amenaza_dron = dron.patrullar()

        if amenaza_dron:
            # Alertar al Guardia
            guardia = Guardia()
            guardia.validar_alerta(alerta_dron="Amenaza detectada por el Dron.")
        else:
            print("Dron: Patrullaje completado. Alerta inicial marcada como FALSA ALARMA.")
    else:
        print("Cámara: No se detectaron amenazas en el video inicial.")
