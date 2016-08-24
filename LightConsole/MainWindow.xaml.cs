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

        public MainWindow()
        {
            InitializeComponent();

            settingsChild.OnSettingsChanged += SettingsChild_OnSettingsChanged;

            LoggingFactory.InitializeLogFactory();            
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            m_progress = await DialogManager.ShowProgressAsync(
                this,
                "Loading...",
                "Please Wait...");
            m_progress.SetIndeterminate();


            if (!string.IsNullOrEmpty(Properties.Settings.Default.Gateway))
            {
                var result = await InitClientAsync(Properties.Settings.Default.Gateway);
                statusLbl.Content = result ? "Connected" : "Error";
            }
            else
            {
                statusLbl.Content = "Unconfigured: Gateway required (File->Configure)";
                await m_progress.CloseAsync();
            }


        }


        /// <summary>
        /// Configure TCP lighting client. Returns true if the initialization
        /// was successful. Possible failure states:
        /// * Failed to find host
        /// * Bad token and host is not in sync mode
        /// </summary>
        /// <param name="gateway">URI or IP of gateway</param>
        /// <returns>bool success</returns>
        private async Task<bool> InitClientAsync(string gateway)
        {

            m_lightClient = new TCPConnected(gateway);
            m_lightClient.OnRoomDiscovered += M_lightClient_OnRoomDiscovered;
            m_lightClient.OnRoomStateChanged += M_lightClient_OnRoomStateChanged;

            return await m_lightClient.InitAsync();            
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
        private void SettingsChild_OnSettingsChanged(object sender, EventArgs e)
        {
            Topmost = Properties.Settings.Default.OnTop;
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
