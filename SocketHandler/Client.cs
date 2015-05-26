using System;
using System.Net;
using System.Net.Sockets;

namespace SocketHandler
{
    public class Client
    {
        public const bool DEBUG = true;

        private Socket clientSocket = null;
        private Controller connectionManager = null;

        /// <summary>
        /// Gets called when the connection manager's onReceiveData function is called.
        /// Use this to handle incoming client data.
        /// </summary>
        public Action<string> onReceiveData = null;

        /// <summary>
        /// Gets called when the client socket is shut down.
        /// This helps notify the program if an error happened or if the connection was terminated.
        /// </summary>
        public Action<Exception> onCloseConnection = null;

        private bool isRunning = false;
        /// <summary>
        /// Set this to false to shut down the client connection.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return isRunning;
            }
            set
            {
                isRunning = value;
                if (!isRunning && clientSocket != null)
                {
                    CloseConnection(null);
                }
            }
        }

        /// <summary>
        /// Client sockets will automatically make a connection with a server socket.
        /// From there you can actively send and receive data.
        /// </summary>
        /// <param name="ip">The remote IP address to connect to.</param>
        /// <param name="port">The port to connect on.</param>
        public Client(IPAddress ip, int port)
        {
            try
            {
                //IPHostEntry host = Dns.GetHostEntry("progressiongames.servegame.org");
                IPEndPoint remoteEP = new IPEndPoint(ip, port);

                Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                clientSocket.Connect(remoteEP);

                connectionManager = new Controller(clientSocket);
                connectionManager.onReceiveData += onReceiveData;
                connectionManager.onCloseConnection += CloseConnection;

                isRunning = true;
            }
            catch (Exception e)
            {
                Debug("Failed to initialize client socket:\n" + e.ToString());
            }
        }

        ~Client()
        {
            IsRunning = false;
        }

        /// <summary>
        /// Sends data through the socket.
        /// </summary>
        /// <param name="data">The byte data to send through the socket.</param>
        /// <param name="length">How many bytes from the data parameter to send through.</param>
        public void SendData(string message)
        {
            connectionManager.SendData(message);
        }

        /// <summary>
        /// Shuts down the client connection.
        /// This cleans up the controller and the socket, as well as calls onCloseConnection.
        /// </summary>
        private void CloseConnection(Exception e)
        {
            if (connectionManager != null)
            {
                connectionManager.onReceiveData -= onReceiveData;
                connectionManager.onCloseConnection -= CloseConnection;
                connectionManager.IsRunning = false;
                connectionManager = null;
            }
            if (clientSocket != null)
            {
                try
                {
                    clientSocket.Shutdown(SocketShutdown.Both);
                    clientSocket.Close();
                    clientSocket = null;
                }
                catch (Exception)
                {
                }
            }
            if (onCloseConnection != null)
            {
                onCloseConnection(e);
            }
        }

        private void Debug(string s)
        {
            if (DEBUG)
            {
                Console.WriteLine("CLIENT: " + s);
            }
        }
    }
}
