using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace SocketHandler
{
    class Controller
    {
        public const int MAXIMUM_MESSAGE_LENGTH = 1024;

        /// <summary>
        /// This will be called when the socket receives data.
        /// </summary>
        public Action<int, byte[]> onReceiveData = null;

        /// <summary>
        /// Gets called when the controller is shut down.
        /// This helps notify the program if an error happened or if the connection was terminated.
        /// </summary>
        public Action<Exception> onCloseConnection = null;

        private Socket socket = null;
        private Thread receiveData = null;

        private bool isRunning = false;
        /// <summary>
        /// Set this to false to shut down the controller.
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
                if (!isRunning && receiveData != null)
                {
                    CloseConnection(null);
                }
            }
        }

        /// <summary>
        /// This class will start a thread to actively read data, putting it through the onReceivedData Action.
        /// This class should also be used to send data through the socket.
        /// </summary>
        /// <param name="s">The socket you wish to read/write on.</param>
        public Controller(Socket s)
        {
            socket = s;

            receiveData = new Thread(StartReceivingData);
            receiveData.Start();
        }

        ~Controller()
        {
            IsRunning = false;
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
                    byte[] buffer = new byte[MAXIMUM_MESSAGE_LENGTH];
                    int numBytes = socket.Receive(buffer);

                    onReceiveData(numBytes, buffer);
                    Thread.Sleep(0);
                }
            }
            catch(SocketException e)
            {
                Console.WriteLine("Encountered a socket error when trying to recieve data:");
                Console.WriteLine(e);
                CloseConnection(e);
            }
            catch (ThreadAbortException)
            {
                Console.WriteLine("Receive Data thread shut down successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Receive Data thread encountered an error:");
                Console.WriteLine(e);
                CloseConnection(e);
            }
        }

        /// <summary>
        /// Sends data through the socket.
        /// </summary>
        /// <param name="data">The byte data to send through the socket.</param>
        /// <param name="length">How many bytes from the data parameter to send through.</param>
        public void SendData(byte[] data, int length)
        {
            try
            {
                if (length > MAXIMUM_MESSAGE_LENGTH)
                {
                    Console.WriteLine("Trying to send data larger than the maximum message length!!!");
                }
                socket.Send(data, length, SocketFlags.None);
            }
            catch (SocketException e)
            {
                Console.WriteLine("Encountered a socket error when trying to send data. Likely caused by a disconnect.");
                CloseConnection(e);
            }
            catch (Exception e)
            {
                Console.WriteLine("Encountered an unexpected error when trying to send data:");
                Console.WriteLine(e);
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
            if (receiveData != null)
            {
                receiveData.Abort();
                receiveData = null;
            }
            if (onCloseConnection != null)
            {
                onCloseConnection(e);
            }
        }
    }
}
