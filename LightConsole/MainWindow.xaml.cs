using LightControl;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace LightConsole
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private TCPConnected m_lightClient;
        private ProgressDialogController m_progress;
        private string m_gateway;
        private GHLightsClient m_ghClient;

        public MainWindow()
        {
            InitializeComponent();

            settingsChild.OnSettingsChanged += SettingsChild_OnSettingsChanged;

            m_gateway = Properties.Settings.Default.Gateway;

            LoggingFactory.InitializeLogFactory();

            // Fire up the GH listener
            m_ghClient = new GHLightsClient(
                Properties.Settings.Default.ProjectID,
                Properties.Settings.Default.SubscriptionID,
                Properties.Settings.Default.TopicID);
            m_ghClient.OnGHLightsEvent += M_ghClient_OnGHLightsEvent;
        }

        private void M_ghClient_OnGHLightsEvent(object sender, GHEventArgs e)
        {
            Control_OnModifyRequested(this, new ModifyLightArgs("Office", e.Level, e.Mode == OnMode.Off));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(m_gateway))
            {
                await InitClientAsync();
            }
            else
            {
                statusLbl.Content = "Unconfigured: Gateway required (File->Configure)";
            }
        }


        /// <summary>
        /// Configure TCP lighting client. Returns true if the initialization
        /// was successful. Possible failure states:
        /// * Failed to find host
        /// * Bad token and host is not in sync mode
        /// </summary>
        /// <returns>bool success</returns>
        private async Task<bool> InitClientAsync()
        {
            m_progress = await DialogManager.ShowProgressAsync(
                this,
                "Loading...",
                "Please Wait...");
            m_progress.SetIndeterminate();

            if (m_lightClient != null)
            {
                m_lightClient.OnRoomDiscovered -= M_lightClient_OnRoomDiscovered;
                m_lightClient.OnRoomStateChanged -= M_lightClient_OnRoomStateChanged;
            }

                m_lightClient = new TCPConnected(m_gateway);
                m_lightClient.OnRoomDiscovered += M_lightClient_OnRoomDiscovered;
                m_lightClient.OnRoomStateChanged += M_lightClient_OnRoomStateChanged;

            bool connected = false;
            try
            {
                connected = await m_lightClient.InitAsync();
                statusLbl.Content = string.Format("Connected to {0}", m_gateway);
            }
            catch (NotInSyncModeException e)
            {
                statusLbl.Content = e.Message;
                await m_progress.CloseAsync();
            }
            catch(TCPGatewayUnavailable e)
            {
                statusLbl.Content = e.Message;
                await m_progress.CloseAsync();
            }


            return connected;
        }

        #region Event Handlers
        private void M_lightClient_OnRoomStateChanged(object sender, RoomEventArgs e)
        {
            // @TODO
        }

        private void M_lightClient_OnRoomDiscovered(object sender, RoomEventArgs e)
        {
            DoOnUIThread(() =>
            {
                RoomControl control = new RoomControl(e.Room);
                control.OnModifyRequested += Control_OnModifyRequested;
                roomTabs.AddControl(control, e.Room.name);

            });

            m_progress.CloseAsync();
        }

        /// <summary>
        /// Handle light control changes requests
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Control_OnModifyRequested(object sender, ModifyLightArgs e)
        {
            Task.Factory.StartNew(() =>
                {
                    if (e.On)
                    {
                        m_lightClient.TurnOffRoomByName(e.Name);
                    }
                    else
                    {
                        m_lightClient.TurnOnRoomWithLevelByName(e.Name, e.Level);
                    }
                });
        }

        /// <summary>
        /// Reapply any settings from the settings file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SettingsChild_OnSettingsChanged(object sender, EventArgs e)
        {
            Topmost = Properties.Settings.Default.OnTop;

            if(m_gateway.Equals(Properties.Settings.Default.Gateway)) {
                return;
            }

            if(!string.IsNullOrEmpty(Properties.Settings.Default.Gateway))
            {
                m_gateway = Properties.Settings.Default.Gateway;
                await InitClientAsync();
            } 

        }
        #endregion



        #region Click Listeners
        /// <summary>
        /// Terminate application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuQuit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Launch settings window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuConfig_Click(object sender, RoutedEventArgs e)
        {
            settingsChild.IsOpen = true;
        }
        #endregion


        /// <summary>
        /// Runs the specified action on the UI thread. Please be sure 
        /// to mark your delegates as async before passing them to this function.
        /// </summary>
        /// <param name="action"></param>
        private void DoOnUIThread(Action action)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(action);
            }
            else
            {
                action.Invoke();
            }
        }
    }
}
