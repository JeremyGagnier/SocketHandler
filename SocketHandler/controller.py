import socket
import threading
import time
import sys

class Controller(object):
    DEBUG = True

    def __init__(self, sock):
        self.on_receive_data = []
        self.on_close_connection = []

        self.socket = sock
        self.connected_addr = sock.getpeername()[0]
        self._alive = True
        self.receive_data = threading.Thread(target=self.start_receiving_data)
        self.receive_data.start()
        self.debug("Started a new socket controller")

    def start_receiving_data(self):
        try:
            message = ""
            while self._alive:
                buffer = self.socket.recv(16)
                data = buffer.decode("UTF-16")

                found_end_line = True
                while found_end_line:
                    found_end_line = False
                    for i in range(len(data)):
                        if data[i] == chr(0):
                            message += data[:i]
                            data = data[i+1:]
                            self.debug("Received Message: " + message)
                            map(lambda x: x(message), on_receive_data)
                            message = ""
                            found_end_line = True
                            break

                message += data
                time.sleep(0)

        except socket.error:
            self.debug("Encountered a socket error when trying to receive data:\n" + sys.exc_info()[0])
            self.close_connection()

        except:
            self.debug("Receive data thread encountered an error:\n" + sys.exc_info()[0])
            self.close_connection()

    def send_data(self, data):
        try:
            self.debug("Sending Data: " + data)
            data = data + chr(0)
            self.socket.sendall(data.encode("UTF-16"))

        except socket.error:
            self.debug("Encountered a socket error when trying to send data. Likely caused by a disconnect.")
            self.close_connection()

        except:
            self.debug("Encountered an unexpected error when trying to send data:\n" + sys.exc_info()[0])
            self.close_connection()

    def close_connection(self):
        self.debug("Connection was closed")
        self._alive = False
        map(lambda x: x(), on_close_connection)

    def debug(self, message):
        if DEBUG:
            print(self.connected_addr + ": " + message)
