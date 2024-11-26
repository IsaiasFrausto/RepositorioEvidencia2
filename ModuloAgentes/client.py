from agents import CameraAgent, AuctioneerAgent, DroneAgent, GuardAgent, system_state
import agentpy as ap
import time
import os

class SecurityModel(ap.Model):
    """ Modelo de simulación del sistema de seguridad """

    def setup(self):
        """ Configuración inicial del modelo """
        self.system_running = True
        self.server_host = '127.0.0.1'
        self.server_port = 5000

        # Rutas relativas a los videos
        base_video_path = os.path.join(os.path.dirname(__file__), 'videos')
        self.video_paths = [os.path.join(base_video_path, 'carros.mp4'), os.path.join(base_video_path, 'carros.mp4')]

        self.cameras = [CameraAgent(self, agent_id=i, server_host=self.server_host,
                                    server_port=self.server_port, video_path=self.video_paths[i])
                        for i in range(len(self.video_paths))]
        self.auctioneer = AuctioneerAgent(self)
        self.drone = DroneAgent(self, server_host=self.server_host,
                                server_port=self.server_port, video_path=os.path.join(base_video_path, 'carros.mp4'))
        self.guard = GuardAgent(self)

    def run_simulation(self):
        """ Manejo principal del modelo """
        # Iniciar agentes
        for camera in self.cameras:
            camera.start()
        self.auctioneer.start()
        self.drone.start()
        self.guard.start()

        try:
            while True:
                if system_state["status"] == "alert_resolved":
                    print("Sistema: Alerta resuelta. Listo para una nueva subasta.")
                    system_state["status"] = "idle"
                time.sleep(1)
        except KeyboardInterrupt:
            print("\nDeteniendo el sistema...")
            self.stop_agents()

        self.wait_agents()

    def stop_agents(self):
        """ Detiene todos los agentes """
        self.system_running = False  # Detener el sistema
        for camera in self.cameras:
            camera.running = False
        self.auctioneer.running = False
        self.drone.running = False
        self.guard.running = False

    def wait_agents(self):
        """ Espera a que todos los hilos terminen """
        for camera in self.cameras:
            camera.join()
        self.auctioneer.join()
        self.drone.join()
        self.guard.join()


if __name__ == "__main__":
    parameters = {'steps': 100}
    model = SecurityModel(parameters)
    model.setup()
    model.run_simulation()
