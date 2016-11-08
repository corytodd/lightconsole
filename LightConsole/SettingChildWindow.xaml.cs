using System;

namespace LightConsole
{
    /// <summary>
    /// Interaction logic for SettingChildWindow.xaml
    /// </summary>
    public partial class SettingChildWindow
    {
        public SettingChildWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Raised when settings are changed by the user
        /// </summary>
        public event EventHandler<EventArgs> OnSettingsChanged;

        protected void SettingsChanged()
        {
            EventHandler<EventArgs> handler = OnSettingsChanged;
            handler?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Save nothing, exit settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void settingsCancelBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            IsOpen = false;
        }

        /// <summary>
        /// Save current settings values as the new settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void settingsSaveBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // TODO validate
            Properties.Settings.Default.Gateway = settingGatewayTxt.Text;
            if (settingsOnTopToggle.IsChecked != null)
                Properties.Settings.Default.OnTop = settingsOnTopToggle.IsChecked.Value;

            SettingsChanged();

            Properties.Settings.Default.Save();

            IsOpen = false;
        }

        /// <summary>
        /// Apply styles and tweaks once window is fully loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChildWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            settingGatewayTxt.SelectAll();
        }
    }
}
