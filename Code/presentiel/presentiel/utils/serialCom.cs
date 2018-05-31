using System;
using System.IO.Ports;
using System.Threading;

namespace presentiel
{
    class serialCom
    {
        //Serial communication thread
        private Thread serialThread;
        private volatile String send;
        private bool close;
        private int COMPort;

        public event EventHandler<DataEventArgs> DataReceived;

        public serialCom()
        {
            //Serial com thread setup
            send = "";
            COMPort = Properties.Settings.Default.UsbCOM;
            serialThread = new Thread(runSerial);
            serialThread.Start();
        }

        //Switch between USB and Bluetooth serial communication
        public void toggleComMode()
        {
            stop();
            if (Properties.Settings.Default.ComMode)//true = bluetooth
                COMPort = Properties.Settings.Default.BluetoothCOM;
            else
                COMPort = Properties.Settings.Default.UsbCOM;
            serialThread = new Thread(runSerial);
            serialThread.Start();
        }

        public void Send(String message)
        {
            send = message;
        }

        public void stop()
        {
            close = true;
        }

        private void runSerial()
        {
            close = false;

            SerialPort serialPort = new SerialPort("COM"+COMPort, 9600);
            serialPort.Open();
            //Firstly send a PING to initiate both connection state
            serialPort.WriteLine("PING");

            String received = "";

            while (!close)
            {
                //Read incoming message
                if (serialPort.BytesToRead > 0)
                    received = serialPort.ReadLine();

                //Send an enventual message
                if (send != "")
                {
                    serialPort.WriteLine(send);
                    send = "";
                }

                //Transfer received message to UI thread via an event
                if (received != "" && DataReceived != null)
                {
                    received=received.TrimEnd(new char[] { '\r', '\n' });//remove endlin characters
                    DataReceived(this, new DataEventArgs(received));
                    received = "";
                }
            }
        }
    }

    public class DataEventArgs : EventArgs
    {
        public string Data { get; private set; }

        public DataEventArgs(string data) { Data = data; }
    }
}
