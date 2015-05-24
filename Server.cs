using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace SocketHandler
{
    public class Server
    {
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

        private bool isRunning = false;
        /// <summary>
        /// Set this to false to shut down the server.
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
                if (!isRunning && serverSocket != null)
                {
                    CloseConnection(null);
                }
            }
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
                //IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);

                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(localEndPoint);
                serverSocket.Listen(MAX_INCOMING_CONNECTION_QUEUE);

                isRunning = true;

                // Start the ReceiveConnections function
                listenerThread = new Thread(ReceiveConnections);
                listenerThread.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to initialize server socket:");
                Console.WriteLine(e);
            }
        }

        ~Server()
        {
            IsRunning = false;
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
                        onNewConnection(newConnection);
                    }
                    Thread.Sleep(0);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("Encountered a socket error when trying to accept new connections:");
                Console.WriteLine(e);
                CloseConnection(e);
            }
            catch (ThreadAbortException)
            {
                Console.WriteLine("Listener Thread shut down successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Listener Thread encountered an error:");
                Console.WriteLine(e);
                CloseConnection(e);
            }
        }

        /// <summary>
        /// This is called when IsRunning is set to false.
        /// This function will shut down the server socket and stop listening for connections.
        /// </summary>
        private void CloseConnection(Exception e)
        {
            if (listenerThread != null)
            {
                listenerThread.Abort();
                listenerThread = null;
            }
            if (serverSocket != null)
            {
                serverSocket.Shutdown(SocketShutdown.Both);
                serverSocket.Close();
                serverSocket = null;
            }
            if (onCloseConnection != null)
            {
                onCloseConnection(e);
            }
        }
    }
}
