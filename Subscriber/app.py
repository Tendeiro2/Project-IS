from flask import Flask, render_template, request
import requests
import xml.etree.ElementTree as ET
import threading
import paho.mqtt.client as mqtt
import urllib3
from datetime import datetime
import subprocess
import sys
from pathlib import Path

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

app = Flask(__name__)

BASE_URL = "https://localhost:44322/api/somiod"
HEADERS = {"Content-Type": "application/xml"}

# Variável para armazenar o estado atual da lâmpada
current_state = None

# Armazenamento de mensagens
mqtt_messages = []
http_messages = []

MQTT_BROKER = "localhost"
MQTT_PORT = 1883 
MQTT_TOPIC = "api/somiod/Lighting/light_bulb" 

# Verifica se a aplicação já existe.
def check_application_exists(application_name):
    response = requests.get(f"{BASE_URL}/{application_name}", headers=HEADERS, verify=False)
    if response.status_code == 200:
        root = ET.fromstring(response.content)
        name = root.find("Name").text
        if name == application_name:
            return True
    return False

# Cria a aplicação se ela ainda não existir.
def create_application():
    application_name = "Lighting"
    if check_application_exists(application_name):
        print(f"A aplicacao '{application_name}' ja existente.")
        return True

    xml_data = f"<Application><Name>{application_name}</Name></Application>"
    response = requests.post(BASE_URL, data=xml_data, headers=HEADERS, verify=False)
    return response.status_code in [200, 201, 409]

# Verifica se o container já existe dentro de uma aplicação.
def check_container_exists(application_name, container_name):
    response = requests.get(f"{BASE_URL}/{application_name}/{container_name}", headers=HEADERS, verify=False)
    if response.status_code == 200:
        root = ET.fromstring(response.content)
        name = root.find("Name").text
        if name == container_name:
            return True
    return False

# Cria o container se ele ainda não existir.
def create_container():
    application_name = "Lighting"
    container_name = "light_bulb"
    if check_container_exists(application_name, container_name):
        print(f"O container '{container_name}' ja existe na aplicacao '{application_name}'.")
        return True

    xml_data = f"<Container><Name>{container_name}</Name></Container>"
    response = requests.post(f"{BASE_URL}/{application_name}", data=xml_data, headers=HEADERS, verify=False)
    return response.status_code in [200, 201, 409]


# Verifica se a notificação já existe dentro de um container.
def check_notification_exists(application_name, container_name, notification_name):
    response = requests.get(f"{BASE_URL}/{application_name}/{container_name}/notification/{notification_name}", headers=HEADERS, verify=False)
    if response.status_code == 200:
        root = ET.fromstring(response.content)
        name = root.find("Name").text
        if name == notification_name:
            return True
    return False

# Cria notificações MQTT e HTTP se ainda não existirem.
def create_notification():
    application_name = "Lighting"
    container_name = "light_bulb"

    notification_name_mqtt = "sub_mqtt"
    if check_notification_exists(application_name, container_name, notification_name_mqtt):
        print(f"A notificacao MQTT '{notification_name_mqtt}' ja existe no container '{container_name}'.")
    else:
        xml_data_mqtt = f"""
        <Notification>
            <Name>{notification_name_mqtt}</Name>
            <Event>1</Event>
            <Endpoint>mqtt://localhost:1883</Endpoint>
            <Enabled>true</Enabled>
        </Notification>
        """
        response_mqtt = requests.post(f"{BASE_URL}/{application_name}/{container_name}", data=xml_data_mqtt, headers=HEADERS, verify=False)
        if response_mqtt.status_code in [200, 201, 409]:
            print(f"Notificação MQTT '{notification_name_mqtt}' criada com sucesso.")
        else:
            print(f"Erro ao criar a notificação MQTT: {response_mqtt.status_code}")

    notification_name_http = "sub_http"
    if check_notification_exists(application_name, container_name, notification_name_http):
        print(f"A notificacao HTTP '{notification_name_http}' ja existe no container '{container_name}'.")
    else:
        xml_data_http = f"""
        <Notification>
            <Name>{notification_name_http}</Name>
            <Event>2</Event>
            <Endpoint>http://localhost:1884</Endpoint>
            <Enabled>true</Enabled>
        </Notification>
        """
        response_http = requests.post(f"{BASE_URL}/{application_name}/{container_name}", data=xml_data_http, headers=HEADERS, verify=False)
        if response_http.status_code in [200, 201, 409]:
            print(f"Notificação HTTP '{notification_name_http}' criada com sucesso.")
        else:
            print(f"Erro ao criar a notificação HTTP: {response_http.status_code}")

# Lida com notificações HTTP recebidas no endpoint.
@app.route("/notify_http", methods=["POST"])
def notify_http():
    global http_messages
    try:
        data = request.data.decode("utf-8")
        root = ET.fromstring(data)
        record = root.find("Record")
        if record is not None:
            record_id = record.find("Id").text
            record_name = record.find("Name").text
            content = record.find("Content").text
            timestamp = record.find("CreationDateTime").text

            formatted_date = datetime.fromisoformat(timestamp).strftime("%d/%m/%Y %H:%M:%S")

            formatted_message = f"{record_id} {record_name.split('_')[0]} - {content} - {formatted_date}"

            if formatted_message not in http_messages:
                http_messages.append(formatted_message)
                http_messages = http_messages[-20:]
                print(f"Mensagem HTTP formatada adicionada: {formatted_message}")

        return "Notification received via HTTP", 200
    except ET.ParseError as e:
        print(f"Erro ao parsear a mensagem HTTP: {e}")
        return "Invalid XML format", 400


@app.route("/mqtt-messages", methods=["GET"])
def get_mqtt_messages():
    return {"messages": mqtt_messages}, 200

@app.route("/http-messages", methods=["GET"])
def get_http_messages():
    return {"messages": http_messages}, 200

def setup_resources():
    if create_application():
        create_container()
        create_notification()

@app.route("/", methods=["GET"])
def index():
    global current_state
    return render_template("index.html", message=None, state=current_state)

@app.route("/", methods=["POST"])
def root_notify():
    return notify_http()

# Rota para receber notificações e atualizar o estado da lâmpada
@app.route("/notify", methods=["POST"])
def notify():
    global current_state

    data = request.data
    root = ET.fromstring(data)
    content = root.find("Content").text

    if content == "on":
        current_state = "on"
    elif content == "off":
        current_state = "off"

    return "Notification received", 200

@app.route("/state", methods=["GET"])
def get_state():
    global current_state
    return {"state": current_state}, 200


# Função para tratar mensagens recebidas via MQTT
def on_message(client, userdata, msg):
    global mqtt_messages
    print(f"on_message chamado para payload: {msg.payload.decode('utf-8')}")
    payload = msg.payload.decode("utf-8")
    
    try:
        root = ET.fromstring(payload)
        record = root.find("Record")
        if record is not None:
            record_id = record.find("Id").text
            record_name = record.find("Name").text
            content = record.find("Content").text
            timestamp = record.find("CreationDateTime").text

            formatted_date = datetime.fromisoformat(timestamp).strftime("%d/%m/%Y %H:%M:%S")

            formatted_message = f"{record_id} {record_name.split('_')[0]} - {content} - {formatted_date}"

            if formatted_message not in mqtt_messages: 
                mqtt_messages.append(formatted_message)
                mqtt_messages = mqtt_messages[-20:]
                print(f"Mensagem formatada adicionada: {formatted_message}")

            global current_state
            if content == "on":
                current_state = "on"
            elif content == "off":
                current_state = "off"
        else:
            print("Formato de mensagem inesperado. Nenhum 'Record' encontrado.")
    except ET.ParseError as e:
        print(f"Erro ao parsear a mensagem XML: {e}")

# Verifica e instala dependências do arquivo requirements.txt.
def check_and_install_requirements(requirements_file="requirements.txt"):
    try:
        requirements_path = Path(requirements_file)
        if not requirements_path.exists():
            raise FileNotFoundError(f"Arquivo {requirements_file} não encontrado.")
        
        with open(requirements_path, "r") as file:
            packages = [line.strip() for line in file if line.strip()]

        for package in packages:
            try:
                __import__(package.split('==')[0].split('>=')[0].strip())
            except ImportError:
                print(f"Pacote {package} não está instalado. Instalando agora...")
                subprocess.check_call([sys.executable, "-m", "pip", "install", package])
        print("Todas as dependências foram verificadas e instaladas.")
    except Exception as e:
        print(f"Erro ao verificar ou instalar dependências: {e}")
        sys.exit(1)


# Configuração do cliente MQTT em uma thread separada
def mqtt_thread():
    client = mqtt.Client()
    client.on_message = on_message
    try:
        client.connect(MQTT_BROKER, MQTT_PORT, 60)
        client.subscribe(MQTT_TOPIC, qos=0)
        print(f"Inscrito no tópico: {MQTT_TOPIC}")
        client.loop_forever()
    except Exception as e:
        print(f"Erro ao conectar ao broker MQTT: {e}")

if __name__ == "__main__":
    check_and_install_requirements()
    setup_resources()

    # Cria thread separada para o endpoint HTTP
    http_listener = threading.Thread(
        target=lambda: app.run(port=1884, debug=False, use_reloader=False)
    )
    http_listener.daemon = True
    http_listener.start()

    # Configura o servidor MQTT
    mqtt_listener = threading.Thread(target=mqtt_thread)
    mqtt_listener.daemon = True
    mqtt_listener.start()

    app.run(debug=False)

