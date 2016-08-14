using System;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace SocketHandler
{
    public class UDPServer
    {
        private const bool DEBUG = true;

        private Dictionary<IPEndPoint, Action<byte[]>> onReceiveData =
            new Dictionary<IPEndPoint, Action<byte[]>>();

        private int port;
        private UdpClient socket = null;
        private Thread receiveData = null;
        
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

        public UDPServer(int port)
        {
            this.port = port;
            socket = new UdpClient(port);
            receiveData = new Thread(StartReceivingData);
            receiveData.Start();
            _isRunning = true;
            Debug("Started");
        }

        ~UDPServer()
        {
            Stop();
        }
        
        public void ListenToEndPoint(IPEndPoint endpoint, Action<byte[]> onMessage)
        {
            Debug("Now listening on " + endpoint.Address.ToString());
            onReceiveData[endpoint] = onMessage;
        }

        public void ForgetEndPoint(IPEndPoint endpoint)
        {
            onReceiveData.Remove(endpoint);
        }

        private void StartReceivingData()
        {
            // This outer while loop keeps the UDP server going after socket errors
            while (_isRunning)
            {
                try
                {
                    IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, port);
                    while (true)
                    {
                        byte[] buffer = socket.Receive(ref endpoint);
                        if (!onReceiveData.ContainsKey(endpoint)) continue;

                        string bufferString = ((int)buffer[0]).ToString();
                        for (int i = 1; i < buffer.Length; ++i)
                        {
                            bufferString += "," + ((int)buffer[i]).ToString();
                        }
                        Debug(string.Format("Got data: {0}|{1}", buffer.Length, bufferString));

                        onReceiveData[endpoint](buffer);
                        Thread.Sleep(0);
                    }
                }
                catch (SocketException e)
                {
                    Debug("Encountered a socket error when trying to receive data:\n" + e.ToString());
                }
                catch (ThreadAbortException e)
                {
                    if (!_isRunning)
                    {
                        Debug("Receive Data thread shut down successfully.");
                    }
                    else
                    {
                        Debug("Receive Data thread aborted without closing connection");
                        CloseConnection(e);
                    }
                }
                catch (Exception e)
                {
                    Debug("Receive Data thread encountered an error:\n" + e.ToString());
                    CloseConnection(e);
                }
            }
        }

        public void SendData(IPEndPoint endpoint, byte[] data)
        {
            try
            {
                string bufferString = ((int)data[0]).ToString();
                for (int i = 1; i < data.Length; ++i)
                {
                    bufferString += "," + ((int)data[i]).ToString();
                }
                Debug("Sending Data: " + bufferString);
                socket.Send(data, data.Length, endpoint);
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
                Console.WriteLine("UDP SERVER: " + s);
            }
        }
    }
}
