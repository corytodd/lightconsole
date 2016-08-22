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
        public static readonly int MinValue = 0;
        public static readonly int MaxValue = 100;

        #region Fields
        private string m_name = string.Empty;
        private int m_min = MinValue + 10;
        private int m_max = MaxValue - 10;
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
            CurrentMinValue = Convert.ToInt32(room.device.level);
            stateToggle.IsChecked = room.device.state.Equals("1");
        }

        public int CurrentMinValue
        {
            get { return m_min; }
            set
            {
                m_min = value;
                NotifyPropertyChanged("CurrentMinValue");
            }
        }

        public int CurrentMaxValue
        {
            get { return m_max; }
            set
            {
                m_max = value;
                NotifyPropertyChanged("CurrentMaxValue");
            }
        }

        private void stateToggle_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            ModifyLight(new ModifyLightArgs(m_name, CurrentMinValue, stateToggle.IsChecked.Value));
        }

        private void stateToggle_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            ModifyLight(new ModifyLightArgs(m_name, CurrentMinValue, stateToggle.IsChecked.Value));
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
