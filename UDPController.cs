using System;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

using UnityEngine;

namespace SocketHandler
{
    public class UDPController
    {
        public static bool DEBUG = false;

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

        private void CloseConnection(Exception e)
        {
            if (!_isRunning)
            {
                return;
            }
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
                //Console.WriteLine("UDP " + endpoint.Address.ToString() + ": " + s);
                UnityEngine.Debug.Log("UDP " + endpoint.Address.ToString() + ": " + s);
            }
        }
    }
}
