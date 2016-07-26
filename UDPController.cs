using System;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace SocketHandler
{
    public class UDPController
    {
        public const bool DEBUG = true;

        /// <summary>
        /// This will be called when the socket receives data.
        /// </summary>
        public Action<byte[]> onReceiveData = null;

        private UdpClient socket = null;
        private Thread receiveData = null;
        private IPEndPoint endpoint = null;

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
        /// This class will start a thread to actively read data, putting it through the onReceivedData Action.
        /// This class should also be used to send data through the socket.
        /// </summary>
        /// <param name="s">The socket you wish to read/write on.</param>
        public UDPController(Socket tcpSocket)
        {
            endpoint = (IPEndPoint)tcpSocket.RemoteEndPoint;
            socket = new UdpClient((IPEndPoint)tcpSocket.LocalEndPoint);
            socket.Connect(endpoint);
            receiveData = new Thread(StartReceivingData);
            receiveData.Start();
            _isRunning = true;
            Debug("Started new UDP controller");
        }

        ~UDPController()
        {
            Stop();
        }

        /// <summary>
        /// This function is ran in a thread and will actively receive incoming data.
        /// </summary>
        private void StartReceivingData()
        {
            try
            {
                while (true)
                {
                    byte[] buffer = socket.Receive(ref endpoint);

                    string data = Encoding.Unicode.GetString(buffer);
                    string bufferString = ((int)buffer[0]).ToString();
                    for (int i = 1; i < buffer.Length; ++i)
                    {
                        bufferString += "," + ((int)buffer[i]).ToString();
                    }
                    Debug(string.Format("{0}: {1}|{2}", data, buffer.Length, bufferString));

                    onReceiveData(buffer);
                    Thread.Sleep(0);
                }
            }
            catch (SocketException e)
            {
                Debug("Encountered a socket error when trying to receive data:\n" + e.ToString());
                CloseConnection(e);
            }
            catch (ThreadAbortException)
            {
                Debug("Receive Data thread shut down successfully.");
            }
            catch (Exception e)
            {
                Debug("Receive Data thread encountered an error:\n" + e.ToString());
                CloseConnection(e);
            }
        }

        /// <summary>
        /// Sends data through the socket.
        /// </summary>
        /// <param name="data">The byte data to send through the socket.</param>
        public void SendData(byte[] data)
        {
            try
            {
                string bufferString = ((int)data[0]).ToString();
                for (int i = 1; i < data.Length; ++i)
                {
                    bufferString += "," + ((int)data[i]).ToString();
                }
                Debug("Sending Data: " + bufferString);
                socket.Send(data, data.Length);
            }
            catch (SocketException e)
            {
                Debug("Encountered a socket error when trying to send data. Likely caused by a disconnect.");
                CloseConnection(e);
            }
            catch (Exception e)
            {
                Debug("Encountered an unexpected error when trying to send data:\n" + e.ToString());
                CloseConnection(e);
            }
        }

        /// <summary>
        /// Shuts down the connection manager.
        /// All this needs to do is close the running thread and call onCloseConnection.
        /// </summary>
        /// <param name="e">The exception that caused this function to be called. Can be null.</param>
        private void CloseConnection(Exception e)
        {
            if (!_isRunning) return;
            _isRunning = false;

            Debug("Connection was closed");
            try
            {
                socket.Close();
            }
            catch (Exception)
            {
            }
            receiveData.Abort();
        }

        private void Debug(string s)
        {
            if (DEBUG)
            {
                Console.WriteLine("UDP " + endpoint.Address.ToString() + ": " + s);
            }
        }
    }
}
