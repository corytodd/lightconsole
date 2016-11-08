using LightControl;
using System;
using System.ComponentModel;

namespace LightConsole
{
    /// <summary>
    /// Interaction logic for RoomControl.xaml
    /// </summary>
    public partial class RoomControl : INotifyPropertyChanged
    {
        private const int DimmerMin = 0;
        private const int DimmerMax = 100;

        #region Fields
        private readonly string _mName;
        private int _mValue;
        #endregion

        /// <summary>
        /// Raised when a modify request is made by the user
        /// </summary>
        public event EventHandler<ModifyLightArgs> OnModifyRequested;

        public RoomControl(Room room)
        {
            InitializeComponent();
                       
            DataContext = this;
            _mName = room.Name;
            CurrentValue = Convert.ToInt32(room.Device.Level);
            stateToggle.IsChecked = room.Device.State.Equals("1");
        }

        public int MinValue => DimmerMin;

        public int MaxValue => DimmerMax;

        public int CurrentValue
        {
            get { return _mValue; }
            set
            {
                _mValue = value;
                NotifyPropertyChanged("CurrentValue");
            }
        }

        private void stateToggle_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            ModifyLight(new ModifyLightArgs(_mName, CurrentValue, stateToggle.IsChecked != null && !stateToggle.IsChecked.Value));
        }

        private void stateToggle_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            ModifyLight(new ModifyLightArgs(_mName, CurrentValue, stateToggle.IsChecked != null && !stateToggle.IsChecked.Value));
        }


        private void levelSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // only update slider if light is on
            if (stateToggle.IsChecked != null && stateToggle.IsChecked.Value)
            {
                ModifyLight(new ModifyLightArgs(_mName, CurrentValue, !stateToggle.IsChecked.Value));
            }
        }

        protected virtual void ModifyLight(ModifyLightArgs e)
        {
            var handler = OnModifyRequested;
            handler?.Invoke(this, e);
        }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
