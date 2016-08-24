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

        public MainWindow()
        {
            InitializeComponent();

            settingsChild.OnSettingsChanged += SettingsChild_OnSettingsChanged;

            LoggingFactory.InitializeLogFactory();            
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

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var progress = await DialogManager.ShowProgressAsync(this, "Loading...", "Please Wait...");
            progress.SetIndeterminate();

            if (!string.IsNullOrEmpty(Properties.Settings.Default.Gateway))
            {
                var result = await initClientAsync(Properties.Settings.Default.Gateway);
                statusLbl.Content = result ? "Connected" : "Error";
            }
            else
            {
                statusLbl.Content = "Unconfigured: Gateway required (File->Configure)";                               
            }
            await progress.CloseAsync();
        }

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

        /// <summary>
        /// Configure TCP lighting client. Returns true if the initialization
        /// was successful. Possible failure states:
        /// * Failed to find host
        /// * Bad token and host is not in sync mode
        /// </summary>
        /// <param name="gateway">URI or IP of gateway</param>
        /// <returns>bool success</returns>
        private async Task<bool> initClientAsync(string gateway)
        {
            return await Task.Factory.StartNew(() =>
            {

                // TODO parametize via UI
                m_lightClient = new TCPConnected(gateway);
                m_lightClient.OnRoomDiscovered += M_lightClient_OnRoomDiscovered;
                m_lightClient.OnRoomStateChanged += M_lightClient_OnRoomStateChanged;

                return m_lightClient.Init().Result;
            });
        }

        private void M_lightClient_OnRoomStateChanged(object sender, RoomEventArgs e)
        {
            // @todo
            M_lightClient_OnRoomDiscovered(sender, e);
        }

        private void M_lightClient_OnRoomDiscovered(object sender, RoomEventArgs e)
        {
            DoOnUIThread(() =>
            {
                MetroTabItem tab = new MetroTabItem();
                tab.Header = e.Room.name;

                RoomControl rc = new RoomControl(e.Room);
                rc.OnModifyRequested += Rc_OnModifyRequested;                                
                tab.Content = rc;

                roomTabs.Items.Add(tab);
            });
        }

        private async void Rc_OnModifyRequested(object sender, ModifyLightArgs e)
        {
            await Task.Factory.StartNew(() =>
            {
                if (e.On)
                {
                    m_lightClient.TurnOnRoomWithLevelByName(e.Name, e.Level);
                }
                else
                {
                    m_lightClient.TurnOffRoomByName(e.Name);
                }
            });
        }


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
