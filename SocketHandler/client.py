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
            self.connection_manager.on_receive_data.append(self._receive_data)
            self.connection_manager.on_close_connection.append(self.close_connection)

        except:
            self.debug("Failed to initialize client socket:\n" + sys.exc_info()[0])
        
    def send_data(self, data):
        self.connection_manager.send_data(data)

    def _receive_data(self, data):
        map(lambda x: x(data), self.on_receive_data)

    def close_connection(self):
        self.connection_manager.on_receive_data.remove(self._receive_data)
        self.connection_manager.on_close_connection.remove(self.close_connection)

        try:
            self.socket.shutdown(socket.SHUT_RDWR)
            self.socket.close()
        except:
            pass

        map(lambda x: x(), self.on_close_connection)

    def debug(self, message):
        if DEBUG:
            print(self.connected_addr + ": " + message)
