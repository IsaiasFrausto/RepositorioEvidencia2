from owlready2 import *
import agentpy as ap
import threading
import queue
import time
import random
import socket
import cv2

# Ontología
onto = get_ontology("file:///security_system.owl")

with onto:
    class Agent(Thing):
        pass

    class Alert(Thing):
        pass

    class has_alert(FunctionalProperty, ObjectProperty):
        domain = [Agent]
        range = [Alert]

    class alert_level(FunctionalProperty, DataProperty):
        domain = [Alert]
        range = [int]

    class is_resolved(FunctionalProperty, DataProperty):
        domain = [Alert]
        range = [bool]

    class has_id(FunctionalProperty, DataProperty):
        domain = [Agent]
        range = [int]

    class has_position(FunctionalProperty, DataProperty):
        domain = [Agent]
        range = [str]

    class Camera(Agent): # Camara en la ontología
        pass

    class Drone(Agent): # Dron en la ontología
        pass

    class Guard(Agent): # Guardia en la ontología
        pass

    class Auctioneer(Agent):  # Subastador en la ontología
        pass

onto.save("security_system.owl")

# Colas separadas
bids_queue = queue.Queue()
alerts_queue = queue.Queue()
confirmations_queue = queue.Queue()
reset_queues = [queue.Queue() for _ in range(2)]  # Una cola para cada cámara

# Estado compartido del sistema
system_state = {"status": "idle"}  # Estados: idle, alert_processing, alert_resolved

class CameraAgent(ap.Agent, threading.Thread):
    def __init__(self, model, agent_id, server_host, server_port, video_path):
        ap.Agent.__init__(self, model)
        threading.Thread.__init__(self)
        self.id = agent_id
        self.server_host = server_host
        self.server_port = server_port
        self.video_path = video_path
        self.active = True

    def setup(self):
        self.onto_instance = onto.Camera(has_id=self.id)
        self.onto_instance.has_position = f"{random.randint(0, 100)},{random.randint(0, 100)}"

    def run(self):
        while self.model.system_running:
            if self.active and system_state["status"] == "idle":
                print(f"Cámara {self.id}: Procesando video para enviar al servidor.")
                amenaza_detectada, seguridad = self.procesar_video()
                if amenaza_detectada:
                    bids_queue.put({"type": "bid", "sender": self.id, "security": seguridad})
                self.active = False
            self.receive_reset_messages()
            time.sleep(1)

    def procesar_video(self):
        cap = cv2.VideoCapture(self.video_path)
        try:
            while cap.isOpened():
                ret, frame = cap.read()
                if not ret:
                    break
                respuesta = self.enviar_frame(frame)
                print(f"Cámara {self.id} recibió respuesta: {respuesta}")
                if "Amenaza detectada" in respuesta:
                    _, seguridad = respuesta.split("Confianza:")
                    return True, float(seguridad.strip())
            return False, 0.0
        finally:
            cap.release()

    def enviar_frame(self, frame):
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as client_socket:
            client_socket.connect((self.server_host, self.server_port))
            _, encoded_image = cv2.imencode('.jpg', frame)
            image_data = encoded_image.tobytes()
            data_len = f"{len(image_data):07}".encode('utf-8')
            client_socket.sendall(data_len)
            client_socket.sendall(image_data)
            return client_socket.recv(1024).decode('utf-8')

    def receive_reset_messages(self):
        try:
            while not reset_queues[self.id].empty():
                reset_queues[self.id].get_nowait()
                self.active = True
        except queue.Empty:
            pass



class AuctioneerAgent(ap.Agent, threading.Thread):
    def __init__(self, model):
        ap.Agent.__init__(self, model)
        threading.Thread.__init__(self)

    def setup(self):
        """ Inicializa el Subastador en la ontología """
        self.onto_instance = onto.Auctioneer(has_id=self.id)
        self.bids = []  # Almacén de ofertas

    def run(self):
        """ Ciclo principal del Subastador """
        while self.model.system_running:
            if system_state["status"] == "idle":
                self.collect_bids()
                if self.bids:
                    system_state["status"] = "alert_processing"
                    winner = self.determine_winner()
                    print(f"Subastador: Cámara ganadora es {winner['sender']} con seguridad {winner['security']:.2f}")
                    alerts_queue.put({"type": "alert", "sender": "Subastador", "winner": winner})
                    self.bids = []
            time.sleep(5)

    def collect_bids(self):
        """ Recoge ofertas de las cámaras """
        try:
            while not bids_queue.empty():
                message = bids_queue.get_nowait()
                if message["type"] == "bid":
                    self.bids.append(message)
        except queue.Empty:
            pass

    def determine_winner(self):
        """ Determina el ganador de la subasta """
        max_bid = max(self.bids, key=lambda x: x["security"])
        winners = [bid for bid in self.bids if bid["security"] == max_bid["security"]]
        return random.choice(winners)  # En caso de empate, selecciona uno al azar


class DroneAgent(ap.Agent, threading.Thread):
    def __init__(self, model, server_host, server_port, video_path):
        ap.Agent.__init__(self, model)
        threading.Thread.__init__(self)
        self.server_host = server_host
        self.server_port = server_port
        self.video_path = video_path
        self.alert_received = None  # Variable para guardar la alerta recibida

    def setup(self):
        """ Inicializa el dron en la ontología """
        self.onto_instance = onto.Drone(has_id=self.id)
        self.onto_instance.has_position = f"{random.randint(0, 100)},{random.randint(0, 100)}"

    def run(self):
        """ Patrulla tras recibir una alerta """
        while self.model.system_running:
            self.receive_alerts()
            if self.alert_received:
                print(f"Dron: Analizando video tras alerta de Cámara {self.alert_received['sender']}")
                amenaza_detectada = self.patrullar()
                if amenaza_detectada:
                    confirmations_queue.put({"type": "confirm", "sender": "Drone", "alert": self.alert_received})
                else:
                    print("Dron: Falsa alarma. Resolviendo alerta.")
                    self.reset_cameras()
                    system_state["status"] = "alert_resolved"
                self.alert_received = None
            time.sleep(1)

    def receive_alerts(self):
        """ Recoge mensajes del subastador """
        try:
            while not alerts_queue.empty():
                message = alerts_queue.get_nowait()
                if message["type"] == "alert":
                    self.alert_received = message["winner"]
        except queue.Empty:
            pass

    def patrullar(self):
        """ Analiza el video del dron en busca de amenazas durante un máximo de 20 segundos """
        cap = cv2.VideoCapture(self.video_path)
        start_time = time.time()  # Registrar el tiempo de inicio
        amenaza_detectada = False  # Bandera para indicar si se detectó una amenaza

        try:
            while cap.isOpened():
                ret, frame = cap.read()
                if not ret:
                    break

                # Enviar frame al servidor para análisis
                respuesta = self.enviar_frame(frame)
                print(f"Dron recibió respuesta: {respuesta}")
                if "Amenaza detectada" in respuesta:
                    amenaza_detectada = True
                    break  # Detener patrullaje si se detecta una amenaza

                # Detener patrullaje después de 20 segundos
                if time.time() - start_time > 20:
                    print("Dron: Patrullaje completado. No se detectó amenaza.")
                    break

            return amenaza_detectada
        finally:
            cap.release()


    def enviar_frame(self, frame):
        """ Envía un frame al servidor para análisis """
        with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as client_socket:
            client_socket.connect((self.server_host, self.server_port))
            _, encoded_image = cv2.imencode('.jpg', frame)
            image_data = encoded_image.tobytes()
            data_len = f"{len(image_data):07}".encode('utf-8')
            client_socket.sendall(data_len)
            client_socket.sendall(image_data)
            return client_socket.recv(1024).decode('utf-8')

    def reset_cameras(self):
        """ Envía mensajes para reiniciar las cámaras """
        for camera_id in range(len(reset_queues)):
            reset_queues[camera_id].put({"type": "reset_camera"})
            print(f"Dron: Enviando mensaje de reinicio a Cámara {camera_id}.")



class GuardAgent(ap.Agent, threading.Thread):
    def __init__(self, model):
        ap.Agent.__init__(self, model)
        threading.Thread.__init__(self)
        self.alerts_to_validate = []  # Inicializa la lista de alertas

    def run(self):
        """Procesa alertas confirmadas"""
        while self.model.system_running:
            self.receive_confirmations()
            for alert in self.alerts_to_validate:
                print(f"Guardia: Validando alerta confirmada de Cámara {alert['sender']}.")
                decision = input("¿Es real? (y/n): ").strip().lower()
                if decision == "y":
                    print("Guardia: Confirmando alerta como REAL.")
                else:
                    print("Guardia: Marcando alerta como FALSA.")
                self.alerts_to_validate.remove(alert)
                self.reset_cameras()
                system_state["status"] = "alert_resolved"

    def receive_confirmations(self):
        """Recoge mensajes del dron"""
        try:
            while not confirmations_queue.empty():
                message = confirmations_queue.get_nowait()
                if message["type"] == "confirm":
                    self.alerts_to_validate.append(message["alert"])
        except queue.Empty:
            pass

    def reset_cameras(self):
        """Envía mensajes para reiniciar las cámaras"""
        for camera_id in range(len(reset_queues)):
            reset_queues[camera_id].put({"type": "reset_camera"})
            print(f"Guardia: Enviando mensaje de reinicio a Cámara {camera_id}.")
