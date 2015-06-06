import socket
import sys
from controller import Controller

class Client(object):
    DEBUG = True

    def __init__(self, ip, port):
        try:
            self.on_receive_data = []
            self.on_close_connection = []

            self.socket = socket.socket()
            self.socket.connect((ip, port))

            self.connection_manager = Controller(self.socket)
            self.connection_manager.on_receive_data.append(on_receive_data)
            self.connection_manager.on_close_connection.append(close_connection)

        except:
            self.debug("Failed to initialize client socket:\n" + sys.exc_info()[0])
        
    def debug(self, message):
        if DEBUG:
            print(self.connected_addr + ": " + message)
