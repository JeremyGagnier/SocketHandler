using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace SocketHandler
{
    public class Server
    {
        private const bool DEBUG = true;
        private const int MAX_INCOMING_CONNECTION_QUEUE = 16;

        private Socket serverSocket = null;
        private Thread listenerThread = null;

        /// <summary>
        /// This gets called when a new connection is made.
        /// Use this to handle multiple client connections.
        /// </summary>
        public Action<Socket> onNewConnection = null;

        /// <summary>
        /// This gets called when the server shuts down.
        /// This helps notify the program if an error happened.
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
        /// Sets up the server socket and begins to listen for connections.
        /// </summary>
        /// <param name="port">The port number to listen on.</param>
        public Server(int port)
        {
            try
            {
                IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress ipAddress = null;
                foreach (IPAddress addr in ipHostInfo.AddressList)
                {
                    if (addr.AddressFamily == AddressFamily.InterNetwork)
                    {
                        ipAddress = addr;
                        break;
                    }
                }
                Debug("Attempting to bind to address " + ipAddress.ToString() + " on port " + port.ToString());
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(localEndPoint);
                serverSocket.Listen(MAX_INCOMING_CONNECTION_QUEUE);
                Debug("Bind successful, now listening.");

                _isRunning = true;

                // Start the ReceiveConnections function
                listenerThread = new Thread(ReceiveConnections);
                listenerThread.Start();
            }
            catch (Exception e)
            {
                Debug("Failed to initialize server socket:\n" + e.ToString());
            }
        }

        ~Server()
        {
            Stop();
        }

        /// <summary>
        /// Ran in a thread to actively acquire new connections to the server.
        /// Passes each new connection into the onNewConnection function for further handling.
        /// </summary>
        private void ReceiveConnections()
        {
            try
            {
                while (true)
                {
                    Socket newConnection = serverSocket.Accept();
                    if (onNewConnection != null)
                    {
                        IPEndPoint remoteIp = newConnection.RemoteEndPoint as IPEndPoint;
                        Debug("Connected to a new user at address " + remoteIp.Address.ToString());
                        onNewConnection(newConnection);
                    }
                    Thread.Sleep(0);
                }
            }
            catch (SocketException e)
            {
                Debug("Encountered a socket error when trying to accept new connections:\n" + e.ToString());
                CloseConnection(e);
            }
            catch (ThreadAbortException)
            {
                Debug("Listener Thread shut down successfully.");
            }
            catch (Exception e)
            {
                Debug("Listener Thread encountered an error:\n" + e.ToString());
                CloseConnection(e);
            }
        }

        /// <summary>
        /// This is called when IsRunning is set to false.
        /// This function will shut down the server socket and stop listening for connections.
        /// </summary>
        private void CloseConnection(Exception e)
        {
            if (!_isRunning) return;
            _isRunning = false;

            Debug("Connection was closed");
            if (onCloseConnection != null)
            {
                onCloseConnection(e);
            }
            listenerThread.Abort();
        }

        static void Debug(string s)
        {
            if (DEBUG)
            {
                Console.WriteLine("SERVER: " + s);
            }
        }
    }
}
