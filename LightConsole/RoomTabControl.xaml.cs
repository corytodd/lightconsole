using System.Collections.Generic;
using System.Windows.Controls;

namespace LightConsole
{
    /// <summary>
    /// Interaction logic for RoomTabControl.xaml
    /// </summary>
    public partial class RoomTabControl
    {
        private readonly List<TabItem> _mControls;

        public RoomTabControl()
        {
            InitializeComponent();

            _mControls = new List<TabItem>();
        }

        public void AddControl(UserControl control, string name)
        {
            TabItem item = new TabItem
            {
                Content = control,
                Header = name
            };

            _mControls.Add(item);
            tabcontrol.Items.Add(item);
        }
    }
}
