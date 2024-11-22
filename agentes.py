# Implementación de los agentes en Python utilizando la librería agentpy

import agentpy as ap

class Dron(ap.Agent):
    """ Agente Dron """
    
    def setup(self):
        self.position = (0, 0)  # Posición inicial
        self.state = "Idle"  # Estado inicial: Idle
        
    def despegar(self):
        self.state = "Flying"
        self.log(f"Despegue completado. Estado: {self.state}")
        
    def patrullar(self):
        if self.state == "Flying":
            self.log("Patrullando...")
        else:
            self.log("No puede patrullar si no está volando.")
            
    def aterrizar(self):
        if self.state == "Flying":
            self.state = "Landed"
            self.log(f"Aterrizaje completado. Estado: {self.state}")
        else:
            self.log("No puede aterrizar si no está volando.")
            
    def detectar_amenaza(self, amenaza):
        self.log(f"Detectada amenaza: {amenaza}")
        return f"Amenaza detectada: {amenaza}"
    
    def enviar_alerta(self):
        self.log("Enviando alerta al guardia...")

class Camara(ap.Agent):
    """ Agente Cámara """
    
    def setup(self):
        self.position = (0, 0)  # Posición inicial
        self.state = "Monitoring"  # Estado inicial: Monitoring
        
    def detectar_movimiento(self):
        self.log("Detectando movimiento...")
        return "Movimiento detectado"
    
    def enviar_alerta_dron(self):
        self.log("Enviando alerta al dron...")

class Guardia(ap.Agent):
    """ Agente Guardia """
    
    def setup(self):
        self.position = (0, 0)  # Posición inicial
        self.state = "Idle"  # Estado inicial: Idle
        
    def tomar_control_dron(self, dron):
        self.state = "Controlling Drone"
        self.log(f"Tomando control del dron. Estado: {self.state}")
        
    def validar_alerta(self, alerta):
        self.log(f"Validando alerta: {alerta}")
        return f"Alerta validada: {alerta}"
    
    def activar_alarma(self):
        self.log("Alarma activada.")

# Preparación de la simulación
class VigilanciaModel(ap.Model):
    """ Modelo que representa el sistema de vigilancia """
    
    def setup(self):
        # Crear agentes
        self.dron = Dron(self)
        self.camara = Camara(self)
        self.guardia = Guardia(self)
        
    def step(self):
        # Ejemplo de interacción
        movimiento = self.camara.detectar_movimiento()
        self.camara.enviar_alerta_dron()
        self.dron.despegar()
        self.dron.patrullar()
        amenaza = self.dron.detectar_amenaza("Intruso detectado")
        self.dron.enviar_alerta()
        self.guardia.tomar_control_dron(self.dron)
        self.guardia.validar_alerta(amenaza)
        self.guardia.activar_alarma()
        self.dron.aterrizar()

# Crear y ejecutar el modelo
model = VigilanciaModel()
model.run()

# Este código establece los agentes y simula sus interacciones según los diagramas proporcionados.
# Se puede extender para integrarse con las imágenes de Unity en los siguientes pasos.
