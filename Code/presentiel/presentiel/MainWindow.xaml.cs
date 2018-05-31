using Microsoft.Lync.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace presentiel
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly String[,] AvailailityTable = new String[7,2] {
            {"Disponible", "GREEN" }, 
            {"Occupé(e)", "RED"}, 
            {"Ne pas déranger", "RED"}, 
            {"De retour dans quelques minutes", "RED"}, 
            {"Absent(e) du bureau", "RED"}, 
            {"Apparaître absent(e)", "RED"}, 
            {"En appel", "ORANGE"}
        };//Array initializer is not static. So I have to use readonly

        private Client skypeClient;
        private serialCom serial;
        private String lastColorSend;//Used to prevent changement to default color's status name due to the arduino response when it changes color

        //Check Arduino availability periodically (timer)
        private DispatcherTimer dispatcherTimer;
        private enum eConnectionCheck
        {
            Wait,
            PingReceived,
            Disconnected
        };
        eConnectionCheck connectionCheck;

        public MainWindow()
        {
            InitializeComponent();

            //Set up Skype client and events
            skypeClient = LyncClient.GetClient();
            skypeClient.StateChanged += Skype_StateChanged;//Link stateChanged to Skype_StateChanged
            skypeClient.Self.Contact.ContactInformationChanged += Skype_ContactInfoChanged;

            //Set up app presence chooser
            for(int i=0; i < AvailailityTable.GetLength(0); i++)
            {
                CmbAppPresence.Items.Add(AvailailityTable[i, 0]);
            }

            //Set up serial communication
            serial = new serialCom();
            serial.DataReceived += ThreadDataReceived;

            lastColorSend = "";
            //AppPresence.Visibility = Visibility.Hidden;
            //Check if Skype is signed-in
            /*if (skypeClient.State == ClientState.SignedIn)
            {
                changeAppPresence();
            }*/

            //Timer setup
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, Properties.Settings.Default.PingFreq);
            dispatcherTimer.Start();
            connectionCheck=eConnectionCheck.Wait;
        }

        //Skype handling
        private void Skype_StateChanged(object s, ClientStateChangedEventArgs e)
        {
            if (e.NewState == ClientState.SignedIn)
            {
                //Hide skype disconnected message
                lblSkypeDisconnected.Visibility = Visibility.Hidden;
                //Hide App only controls
                CmbAppPresence.Visibility = Visibility.Hidden;
                ColorAppPresence.Visibility = Visibility.Hidden;
            }
            else if (e.NewState == ClientState.SignedOut)
            {
                //Show skype disconnected message
                lblSkypeDisconnected.Visibility = Visibility.Visible;
                //Show App only controls
                CmbAppPresence.Visibility = Visibility.Visible;
                ColorAppPresence.Visibility = Visibility.Visible;
            }
        }



        private void Skype_ContactInfoChanged(object s, ContactInformationChangedEventArgs e)
        {
            //Check if the availability has changed or if it's something else
            if (e.ChangedContactInformation.Contains(ContactInformationType.Activity))
            {
                
                //changeAppPresence();
            }
        }

        //App presence handling
        private void changeAppPresence()
        {
            switch (skypeClient.Self.Contact.GetContactInformation(ContactInformationType.Activity))
            {
                case ContactAvailability.Free:
                    CmbAppPresence.SelectedItem = "Disponible";
                    break;
                case ContactAvailability.Busy:
                    CmbAppPresence.SelectedItem = "Occupé(e)";
                    break;
                case ContactAvailability.DoNotDisturb:
                    CmbAppPresence.SelectedItem = "Ne pas déranger";
                    break;
                case ContactAvailability.TemporarilyAway:
                    CmbAppPresence.SelectedItem = "De retour dans quelques minutes";
                    break;
                case ContactAvailability.Away:
                    CmbAppPresence.SelectedItem = "Absent(e) du bureau";
                    break;
                default:
                    CmbAppPresence.SelectedItem = "Autre";
                    break;
            }
        }

        //App presence selector: Selected item changed. Send new color to Arduino.
        private void AppPresence_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Send the color that correspond to the new availability
            if (CmbAppPresence.SelectedIndex < AvailailityTable.GetLength(0))
            {
                //Default indicator color is blue
                Color color = Color.FromRgb(0, 0, 255);//default blue
                if (CmbAppPresence.SelectedIndex >= 0)
                {
                    serial.Send(AvailailityTable[CmbAppPresence.SelectedIndex, 1]);
                    lastColorSend = AvailailityTable[CmbAppPresence.SelectedIndex, 1];
                    
                    //change color indicator
                    switch (AvailailityTable[CmbAppPresence.SelectedIndex, 1])
                    {
                        case "GREEN":
                            color = Color.FromRgb(0, 255, 0);
                            break;
                        case "ORANGE":
                            color = Color.FromRgb(255, 64, 0);
                            break;
                        case "RED":
                            color = Color.FromRgb(255, 0, 0);
                            break;
                    }
                }
                ColorAppPresence.Fill = new SolidColorBrush(color);
            }
        }

        //Received message handling
        void ThreadDataReceived(object s, DataEventArgs e)
        {
            // Note: this method is called in the thread context, thus we must
            // use Invoke to talk to UI controls. So invoke a method on our
            // thread.
            this.Dispatcher.Invoke(new EventHandler<DataEventArgs>(ThreadDataReceivedSync), new object[] { s, e });

        }

        void ThreadDataReceivedSync(object s, DataEventArgs e)
        {
            if (e.Data != lastColorSend)
            {
                switch (e.Data)
                {
                    case "PING":
                        serial.Send("PONG");
                        //Set as ping received and hide no connection label
                        connectionCheck = eConnectionCheck.PingReceived;
                        lblNoConnection.Visibility = Visibility.Hidden;
                        break;
                    case "PONG":
                        break;
                    case "BLUE"://disconnected
                        CmbAppPresence.Text = "Libre";
                        break;
                    case "GREEN":
                    case "ORANGE":
                    case "RED":
                        int index = 0;
                        for (index = 0; index < AvailailityTable.GetLength(0); index++)
                        {
                            if (AvailailityTable[index, 1].Equals(e.Data))
                                break;
                        }
                        CmbAppPresence.SelectedIndex = index;
                        break;
                }
            }
        }

        //Timer event
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            switch (connectionCheck)
            {
                case eConnectionCheck.PingReceived:
                    connectionCheck = eConnectionCheck.Wait;
                    break;
                case eConnectionCheck.Wait:
                    connectionCheck = eConnectionCheck.Disconnected;
                    lblNoConnection.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}