using LightControl;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace LightConsole
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private TcpConnected _mLightClient;
        private ProgressDialogController _mProgress;
        private string _mGateway;

        public MainWindow()
        {
            InitializeComponent();

            settingsChild.OnSettingsChanged += SettingsChild_OnSettingsChanged;

            _mGateway = Properties.Settings.Default.Gateway;

            LoggingFactory.InitializeLogFactory();            
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_mGateway))
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
            _mProgress = await this.ShowProgressAsync("Loading...",
                "Please Wait...");
            _mProgress.SetIndeterminate();

            if (_mLightClient != null)
            {
                _mLightClient.OnRoomDiscovered -= M_lightClient_OnRoomDiscovered;
                _mLightClient.OnRoomStateChanged -= M_lightClient_OnRoomStateChanged;
            }

                _mLightClient = new TcpConnected(_mGateway);
                _mLightClient.OnRoomDiscovered += M_lightClient_OnRoomDiscovered;
                _mLightClient.OnRoomStateChanged += M_lightClient_OnRoomStateChanged;

            bool connected = false;
            try
            {
                connected = await _mLightClient.InitAsync();
                statusLbl.Content = $"Connected to {_mGateway}";
            }
            catch (NotInSyncModeException e)
            {
                statusLbl.Content = e.Message;
                await _mProgress.CloseAsync();
            }
            catch(TcpGatewayUnavailable e)
            {
                statusLbl.Content = e.Message;
                await _mProgress.CloseAsync();
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
            DoOnUiThread(() =>
            {
                RoomControl control = new RoomControl(e.Room);
                control.OnModifyRequested += Control_OnModifyRequested;
                roomTabs.AddControl(control, e.Room.Name);

            });

            _mProgress.CloseAsync();
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
                        _mLightClient.TurnOffRoomByName(e.Name);
                    }
                    else
                    {
                        _mLightClient.TurnOnRoomWithLevelByName(e.Name, e.Level);
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

            if(_mGateway.Equals(Properties.Settings.Default.Gateway)) {
                return;
            }

            if(!string.IsNullOrEmpty(Properties.Settings.Default.Gateway))
            {
                _mGateway = Properties.Settings.Default.Gateway;
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
        private void DoOnUiThread(Action action)
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
