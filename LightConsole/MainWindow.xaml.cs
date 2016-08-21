using LightControl;
using MahApps.Metro.Controls;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // TODO parametize via UI
            m_lightClient = new TCPConnected("192.168.0.151");

            m_lightClient.Init();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            m_lightClient.GetState();
            var room = "Office";

            var state = m_lightClient.GetRoomStateByName(room);
            if(state.On)
            {
                m_lightClient.TurnOffRoomByName(room);
            }
            else
            {
                m_lightClient.TurnOnRoomWithLevelByName(room, 60);
            }

        }

    }
}
