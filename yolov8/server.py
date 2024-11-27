"""from flask import Flask, request, jsonify
import cv2
import numpy as np
from ultralytics import YOLO

app = Flask(__name__)
model = YOLO('yolov8s.pt')  # Cargar el modelo YOLOv8

@app.route('/detect', methods=['POST'])
def detect():
    # Verificar que la solicitud contiene un archivo llamado 'image'
    if 'image' not in request.files:
        return jsonify({"error": "No image part in request"}), 400

    # Leer la imagen enviada desde Unity
    file = request.files['image']
    img = np.frombuffer(file.read(), np.uint8)
    img = cv2.imdecode(img, cv2.IMREAD_COLOR)

    # Realizar detección con YOLO
    results = model.predict(img, save=False)
    detections = []

    # Convertir las coordenadas del tensor a un formato escalar
    for box in results[0].boxes:
        # Convertir las coordenadas del tensor a enteros
        xyxy = box.xyxy.cpu().numpy()[0]  # Asegúrate de acceder al primer elemento correctamente
        detections.append({
            "x": int(xyxy[0]),
            "y": int(xyxy[1]),
            "width": int(xyxy[2] - xyxy[0]),
            "height": int(xyxy[3] - xyxy[1]),
            "label": results[0].names[int(box.cls)],  # Clase del objeto detectado
            "confidence": float(box.conf)  # Confianza del modelo
        })

    # Devolver resultados como JSON
    return jsonify(detections)

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)"""
    
    
"""from flask import Flask, request, jsonify
import cv2
import numpy as np
from ultralytics import YOLO

app = Flask(__name__)
model = YOLO('yolov8s.pt')  # Cargar el modelo YOLOv8

@app.route('/detect', methods=['POST'])
def detect():
    # Verificar que la solicitud contiene un archivo llamado 'image'
    if 'image' not in request.files:
        return jsonify({"error": "No image part in request"}), 400

    # Leer la imagen enviada desde Unity
    file = request.files['image']
    img = np.frombuffer(file.read(), np.uint8)
    img = cv2.imdecode(img, cv2.IMREAD_COLOR)

    # Realizar detección con YOLO
    results = model.predict(img, save=False)
    detections = []

    # Convertir las coordenadas del tensor a formato JSON
    for box in results[0].boxes:
        xyxy = box.xyxy.cpu().numpy()[0]
        detections.append({
            "x": int(xyxy[0]),
            "y": int(xyxy[1]),
            "width": int(xyxy[2] - xyxy[0]),
            "height": int(xyxy[3] - xyxy[1]),
            "label": results[0].names[int(box.cls)],
            "confidence": float(box.conf)
        })

    # Envolver el JSON en un objeto raíz
    return jsonify({"items": detections})

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)"""
    
from flask import Flask, request, jsonify
import cv2
import numpy as np
from ultralytics import YOLO

app = Flask(__name__)
model = YOLO('yolov8s.pt')  # Cargar el modelo YOLOv8

@app.route('/detect', methods=['POST'])
def detect():
    # Verificar que la solicitud contiene un archivo llamado 'image'
    if 'image' not in request.files:
        return jsonify({"error": "No image part in request"}), 400

    # Leer la imagen enviada desde Unity
    file = request.files['image']
    img = np.frombuffer(file.read(), np.uint8)
    img = cv2.imdecode(img, cv2.IMREAD_COLOR)

    # Realizar detección con YOLO
    results = model.predict(img, save=False)
    detections = []

    # Procesar las detecciones y estructurarlas en formato JSON
    for box in results[0].boxes:
        xyxy = box.xyxy.cpu().numpy()[0]
        detections.append({
            "x": int(xyxy[0]),                           # Coordenada X inicial
            "y": int(xyxy[1]),                           # Coordenada Y inicial
            "width": int(xyxy[2] - xyxy[0]),            # Ancho del bounding box
            "height": int(xyxy[3] - xyxy[1]),           # Altura del bounding box
            "label": results[0].names[int(box.cls)],    # Etiqueta del objeto detectado
            "confidence": float(box.conf)              # Confianza del modelo
        })

    # Envolver las detecciones en un objeto raíz
    return jsonify({"items": detections})

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)
