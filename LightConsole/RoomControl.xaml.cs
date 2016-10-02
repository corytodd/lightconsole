using LightControl;
using System;
using System.ComponentModel;
using System.Windows.Controls;

namespace LightConsole
{
    /// <summary>
    /// Interaction logic for RoomControl.xaml
    /// </summary>
    public partial class RoomControl : UserControl, INotifyPropertyChanged
    {
        private static readonly int DIMMER_MIN = 0;
        private static readonly int DIMMER_MAX = 100;

        #region Fields
        private string m_name = string.Empty;
        private int m_value;
        #endregion

        /// <summary>
        /// Raised when a modify request is made by the user
        /// </summary>
        public event EventHandler<ModifyLightArgs> OnModifyRequested;

        public RoomControl(Room room)
        {
            InitializeComponent();
                       
            DataContext = this;
            m_name = room.name;
            CurrentValue = Convert.ToInt32(room.device.level);
            stateToggle.IsChecked = room.device.state.Equals("1");

            MinValue = MinValue + 10;
        }

        public int MinValue
        {
            get { return DIMMER_MIN; }
            private set { }
        }

        public int MaxValue
        {
            get { return DIMMER_MAX; }
            private set { }
        }

        public int CurrentValue
        {
            get { return m_value; }
            set
            {
                m_value = value;
                NotifyPropertyChanged("CurrentValue");
            }
        }

        private void stateToggle_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            ModifyLight(new ModifyLightArgs(m_name, CurrentValue, !stateToggle.IsChecked.Value));
        }

        private void stateToggle_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            ModifyLight(new ModifyLightArgs(m_name, CurrentValue, !stateToggle.IsChecked.Value));
        }

        protected virtual void ModifyLight(ModifyLightArgs e)
        {
            EventHandler<ModifyLightArgs> handler = OnModifyRequested;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

    }
}
