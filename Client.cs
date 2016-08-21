using System;
using System.Net;
using System.Net.Sockets;

using UnityEngine;

namespace SocketHandler
{
    public class Client
    {
        public static bool DEBUG = false;

        private Controller connectionManager = null;

        public Socket socket = null;

        /// <summary>
        /// Gets called when the connection manager's onReceiveData function is called.
        /// Use this to handle incoming client data.
        /// </summary>
        public Action<string> onReceiveData
        {
            get
            {
                return connectionManager.onReceiveData;
            }
            set
            {
                connectionManager.onReceiveData = value;
            }
        }

        /// <summary>
        /// Gets called when the client socket is shut down.
        /// This helps notify the program if an error happened or if the connection was terminated.
        /// </summary>
        public Action<Exception> onCloseConnection = null;

        private bool _isRunning = false;
        public bool isRunning
        {
            get
            {
                return _isRunning;
            }
        }

        public void Stop()
        {
            CloseConnection(null);
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

                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                socket.Connect(remoteEP);

                connectionManager = new Controller(socket);
                connectionManager.onCloseConnection += CloseConnection;

                _isRunning = true;
            }
            catch (Exception e)
            {
                Debug("Failed to initialize client socket:\n" + e.ToString());
            }
        }

        ~Client()
        {
            Stop();
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
        /// </summary>
        private void CloseConnection(Exception e)
        {
            if (!_isRunning)
            {
                return;
            }
            _isRunning = false;

            if (onCloseConnection != null)
            {
                onCloseConnection(e);
            }
            connectionManager.Stop();
        }

        private void Debug(string s)
        {
            if (DEBUG)
            {
                //Console.WriteLine("CLIENT: " + s);
                UnityEngine.Debug.Log("CLIENT: " + s);
            }
        }
    }
}
