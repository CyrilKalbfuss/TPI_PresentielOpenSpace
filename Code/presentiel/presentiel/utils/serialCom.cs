using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using System.Windows;

namespace presentiel
{
    class serialCom
    {
        //Serial communication thread
        private Thread serialThread;
        private volatile List<String> send;
        private bool close;
        private int COMPort;
        public bool connected { get; private set; }//tell if the serial is connected or not

        public event EventHandler<DataEventArgs> DataReceived;
        public event EventHandler<EventArgs> Disconnected;

        public serialCom()
        {
            //Serial com thread setup
            connected = false;
            send = new List<string>();
            COMPort = Properties.Settings.Default.UsbCOM;
            start();
        }

        //Switch between USB and Bluetooth serial communication
        public void switchComMode()
        {
            stop();
            if (Properties.Settings.Default.ComMode)//true = bluetooth
                COMPort = Properties.Settings.Default.BluetoothCOM;
            else
                COMPort = Properties.Settings.Default.UsbCOM;
            start();
        }

        public void Send(String message)
        {
            send.Add(message);
        }

        public void start()
        {
            if (serialThread != null)
            {
                stop();
                while (serialThread.IsAlive)
                    Thread.Sleep(10);
            }

            if (!connected)
            {
                serialThread = new Thread(runSerial);
                serialThread.Start();
            }
        }
        public void stop()
        {
            close = true;
        }

        private void runSerial()
        {
            close = false;

            SerialPort serialPort = new SerialPort("COM"+COMPort, 9600);
            try
            {
                serialPort.Open();
            }catch
            {
                connected = false;
                close = true;

                //Launch disonnection event
                Disconnected(this, null);

                return;
            }

            connected = true;

            //Firstly send a PING to initiate both connection state
            serialPort.WriteLine("PING");

            String received = "";

            while (!close)
            {
                //Read incoming message
                if (serialPort.BytesToRead > 0)
                    received = serialPort.ReadLine();

                //Send an enventual message
                if (send.Count>0)
                {
                    foreach(String msg in send)
                    {
                        try
                        {
                            serialPort.WriteLine(msg);
                        }
                        catch
                        {
                            connected = false;
                            close = true;

                            //Launch disonnection event
                            Disconnected(this, null);

                            return;
                        }
                    }
                    send.Clear();
                }

                //Transfer received message to UI thread via an event
                if (received != "" && DataReceived != null)
                {
                    received=received.TrimEnd(new char[] { '\r', '\n' });//remove endlin characters
                    DataReceived(this, new DataEventArgs(received));
                    received = "";
                }
            }
            try
            {
                serialPort.Close();
            }
            catch { }
            connected = false;
        }
    }

    public class DataEventArgs : EventArgs
    {
        public string Data { get; private set; }

        public DataEventArgs(string data) { Data = data; }
    }
}
