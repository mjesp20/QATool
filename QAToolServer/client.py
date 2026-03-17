import socket

HOST = "127.0.0.1"  # The server's hostname or IP address
PORT = 65432  # The port used by the server

with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
    s.connect((HOST, PORT))
    s.sendall(b'{"PlayerPosition":{"x":52.8,"y":-3.55000949,"z":-11.95},"PlayerVelocity":{"x":0.0,"y":0.0,"z":0.0},"PlayerCamera":{"x":-1.54295677E-07,"y":0.9021408,"z":-0.4314419},"type":"Movement","time":0.130836383,"playerID":1,"args":{"Health":100.0,"CollectedBlobs":null,"jumps":null}}')