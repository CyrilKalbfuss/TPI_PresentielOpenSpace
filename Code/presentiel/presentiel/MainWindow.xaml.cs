using Microsoft.Lync.Model;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace presentiel
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly String[,] AvailailityTable = new String[6,2] {
            {"Disponible", "GREEN"}, 
            {"Occupé(e)", "RED"}, 
            {"Ne pas déranger", "RED"}, 
            {"De retour dans quelques minutes", "RED"}, 
            {"Absent(e) du bureau", "RED"}, 
            {"En appel", "ORANGE"}
        };//Array initializer is not static. So I have to use readonly

        private LyncClient skypeClient;
        private serialCom serial;
        private String lastColorSend;//Used to prevent changement to default color's status name due to the arduino response when it changes color
        private bool contactInfoChangedSetUpDone;
        private bool skypeSetUpDone;

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

            skypeSetUpDone = false;
            contactInfoChangedSetUpDone = false;

            //Set up serial communication
            serial = new serialCom();
            serial.DataReceived += ThreadDataReceived;
            serial.Disconnected += arduinoDisconnected;

            lastColorSend = "";

            //Set up app presence chooser
            for (int i=0; i < AvailailityTable.GetLength(0); i++)
            {
                CmbAppPresence.Items.Add(AvailailityTable[i, 0]);
            }

            //Timer setup
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, Properties.Settings.Default.PingFreq);
            dispatcherTimer.Start();
            connectionCheck=eConnectionCheck.Wait;

            //Set up Skype client and events
            skypeSetUp();
            if(!contactInfoChangedSetUpDone)
                CmbAppPresence.SelectedIndex = 0;
        }


        //--Skype handling
        private void skypeSetUp()
        {
            //Set up Skype client and events
            try
            {
                skypeClient = LyncClient.GetClient();
                skypeClient.StateChanged += Skype_StateChanged;//Link stateChanged to Skype_StateChanged
                if (skypeClient.State == ClientState.SignedIn && !contactInfoChangedSetUpDone)
                {
                    skypeClient.Self.Contact.ContactInformationChanged += Skype_ContactInfoChanged;
                    contactInfoChangedSetUpDone = true;
                }
                //Check skype connection
                SkypeSignedCheck(skypeClient.State);

                skypeSetUpDone = true;

                skypeClient.ClientDisconnected += Skype_closing;
            }
            catch{}
        }

        private void Skype_closing(object s, EventArgs e)
        {
            skypeClient = null;
            skypeSetUpDone = false;
        }

        private void Skype_StateChanged(object s, ClientStateChangedEventArgs e)
        {
            //Invoke event in the UI thread
            this.Dispatcher.Invoke(new EventHandler<ClientStateChangedEventArgs>(Skype_StateChangedSync), new object[] { s, e });
        }

        private void Skype_StateChangedSync(object s, ClientStateChangedEventArgs e)
        {
            SkypeSignedCheck(e.NewState);
        }

        //Check if skype is signed in or signed out and adapt the UI
        private void SkypeSignedCheck(ClientState state)
        {
            switch (state)
            {
                case ClientState.SignedIn:
                    //Hide skype disconnected message
                    lblSkypeDisconnected.Visibility = Visibility.Hidden;
                    //Hide App only controls
                    CmbAppPresence.Visibility = Visibility.Hidden;
                    ColorAppPresence.Visibility = Visibility.Hidden;

                    //setup up ContactInfoChanged event if not done
                    if (!contactInfoChangedSetUpDone)
                    {
                        skypeClient.Self.Contact.ContactInformationChanged += Skype_ContactInfoChanged;
                        contactInfoChangedSetUpDone = true;
                    }
                    //update app availability
                    SkypeAvailabilitySync();
                    break;
                case ClientState.SignedOut:
                case ClientState.SigningOut:
                    //Show skype disconnected message
                    lblSkypeDisconnected.Visibility = Visibility.Visible;
                    //Show App only controls
                    CmbAppPresence.Visibility = Visibility.Visible;
                    ColorAppPresence.Visibility = Visibility.Visible;

                    //ContactInformation event handler has been destroyed
                    contactInfoChangedSetUpDone = false;
                    break;
            }
        }

        private void Skype_ContactInfoChanged(object s, ContactInformationChangedEventArgs e)
        {
            //Invoke event in the UI thread
            this.Dispatcher.Invoke(new EventHandler<ContactInformationChangedEventArgs>(Skype_ContactInfoChangedSync), new object[] { s, e });
        }

        private void Skype_ContactInfoChangedSync(object s, ContactInformationChangedEventArgs e)
        {
            //Check if the availability has changed or if it's something else
            if (e.ChangedContactInformation.Contains(ContactInformationType.Activity))
            {
               SkypeAvailabilitySync();
            }
        }

        private void SkypeAvailabilitySync()
        {
            if (lblSkypeDisconnected.Visibility == Visibility.Hidden)//can't check availability if disconnected
            {
                switch ((ContactAvailability)skypeClient.Self.Contact.GetContactInformation(ContactInformationType.Availability))
                {
                    case ContactAvailability.Free:
                        CmbAppPresence.SelectedIndex = 0;
                        break;
                    case ContactAvailability.Busy:
                    case ContactAvailability.BusyIdle:
                        //depend of the activity (busy, on call, etc)
                        if (skypeClient.Self.Contact.GetContactInformation(ContactInformationType.Activity).ToString().StartsWith("En "))
                            CmbAppPresence.SelectedIndex = 5;
                        else
                            CmbAppPresence.SelectedIndex = 1;
                        break;
                    case ContactAvailability.DoNotDisturb:
                        CmbAppPresence.SelectedIndex = 2;
                        break;
                    case ContactAvailability.TemporarilyAway:
                        CmbAppPresence.SelectedIndex = 3;
                        break;
                    case ContactAvailability.Away:
                        CmbAppPresence.SelectedIndex = 4;
                        break;
                }
            }
        }

        //https://blog.thoughtstuff.co.uk/2016/06/skypedevq-updating-skype-for-business-presence-client-sdk/
        private void SkypeUpdateAvailability(ContactAvailability availability)
        {
            Dictionary<PublishableContactInformationType, object> newStatus = new Dictionary<PublishableContactInformationType, object>();
            newStatus.Add(PublishableContactInformationType.Availability, availability);
            skypeClient.Self.BeginPublishContactInformation(newStatus, skypeEndUpdate, skypeClient.Self);
        }
        private void skypeEndUpdate(IAsyncResult res)
        {
            Self self = res.AsyncState as Self;
            self.EndPublishContactInformation(res);
        }


        //--App handling
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
                //Set app presence
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

                //Update Skype presence
                if (lblSkypeDisconnected.Visibility == Visibility.Hidden)
                {
                    switch (CmbAppPresence.SelectedIndex)
                    {
                        case 0:
                            SkypeUpdateAvailability(ContactAvailability.Free);
                            break;
                        case 1:
                        case 5:
                            if (skypeClient.Self.Contact.GetContactInformation(ContactInformationType.Activity).ToString().StartsWith("En "))
                                break;//Do not change the availability if in call or conference
                            SkypeUpdateAvailability(ContactAvailability.Busy);
                            break;
                        case 2:
                            SkypeUpdateAvailability(ContactAvailability.DoNotDisturb);
                            break;
                        case 3:
                            SkypeUpdateAvailability(ContactAvailability.TemporarilyAway);
                            break;
                        case 4:
                            SkypeUpdateAvailability(ContactAvailability.Away);
                            break;
                    }
                }
            }
        }

        private void btnComSwitch_Click(object sender, RoutedEventArgs e)
        {
            //switch serialCom and change button's background image
            if (Properties.Settings.Default.ComMode)
            {
                Properties.Settings.Default.ComMode = false;
                btnComSwitch.Background = new ImageBrush((ImageSource)Resources["USBLogo"]);
            }
            else
            {
                Properties.Settings.Default.ComMode = true;
                btnComSwitch.Background = new ImageBrush((ImageSource)Resources["BTLogo"]);
            }
            serial.changeCOMPort();

            //remove focus
            Keyboard.ClearFocus();
        }

    
        //--Timer event
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            //check connection with arduino
            switch (connectionCheck)
            {
                case eConnectionCheck.PingReceived:
                    connectionCheck = eConnectionCheck.Wait;
                    break;
                case eConnectionCheck.Wait:
                    serial.start();//Start connection if not already done
                    connectionCheck = eConnectionCheck.Disconnected;
                    lblNoConnection.Visibility = Visibility.Visible;
                    break;
                case eConnectionCheck.Disconnected:
                    serial.start();//Start connection if not already done
                    break;
            }

            //setup skype if not already done
            if (!skypeSetUpDone)
                skypeSetUp();
        }

        //--Disconnection event
        private void arduinoDisconnected(object s, EventArgs e)
        {
            this.Dispatcher.Invoke(new EventHandler<EventArgs>(arduinoDisconnectedSync), new object[] { s, e });
        }

        private void arduinoDisconnectedSync(object s, EventArgs e)
        {
            connectionCheck = eConnectionCheck.Disconnected;
            lblNoConnection.Visibility = Visibility.Visible;
        }


        //--Options
        //Show options "page"
        private void btnOptions_Click(object sender, RoutedEventArgs e)
        {
            OptionsView.Visibility = Visibility.Visible;
        }

        //Hide options
        private void BtnCloseOptions_Click(object sender, RoutedEventArgs e)
        {
            OptionsView.Visibility = Visibility.Hidden;
            serial.changeCOMPort();//Update the used COM port. This method also restart the serial thread.
        }


        //--Connection error page
        //Show
        private void lblNoConnection_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            connectionErrorView.Visibility = Visibility.Visible;
        }
        //Hide
        private void btnConnectionError_Click(object sender, RoutedEventArgs e)
        {
            connectionErrorView.Visibility = Visibility.Hidden;
        }
    }
}