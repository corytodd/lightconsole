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

            LoggingFactory.InitializeLogFactory();            
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var progress = await DialogManager.ShowProgressAsync(this, "Loading...", "Please Wait...");
            progress.SetIndeterminate();

            var result = await initClientAsync();
            statusLbl.Content = result ? "Connected" : "Error";
            await progress.CloseAsync();
        }

        private async Task<bool> initClientAsync()
        {
            return await Task.Factory.StartNew(() =>
            {

                // TODO parametize via UI
                m_lightClient = new TCPConnected("192.168.0.151");
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
