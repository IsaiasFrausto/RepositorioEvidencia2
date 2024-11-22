import agentpy as ap

class Dron(ap.Agent):
    def setup(self):
        self.position = (0, 0)
        self.state = "Idle"

    def analizar_alerta(self, alerta):
        """ Procesa alertas recibidas """
        if alerta == "Amenaza detectada":
            self.state = "Responding"
            self.log("Amenaza confirmada. Tomando acción.")
        else:
            self.state = "Patrolling"
            self.log("Área despejada.")

class Camara(ap.Agent):
    def setup(self):
        self.state = "Monitoring"

    def capturar_imagen(self):
        """ Simula captura de imagen """
        self.log("Capturando imagen del área...")
        return "frame_placeholder"

    def enviar_alerta(self, alerta):
        self.log(f"Enviando alerta: {alerta}")
        return alerta

class Guardia(ap.Agent):
    def setup(self):
        self.state = "Idle"

    def tomar_decision(self, alerta):
        if alerta == "Amenaza detectada":
            self.state = "Responding"
            self.log("Alerta validada. Activando alarma.")
        else:
            self.state = "Idle"
            self.log("Área despejada.")

class VigilanciaModel(ap.Model):
    def setup(self):
        self.dron = Dron(self)
        self.camara = Camara(self)
        self.guardia = Guardia(self)

    def analizar_frame(self, alerta):
        self.dron.analizar_alerta(alerta)
        self.guardia.tomar_decision(alerta)
