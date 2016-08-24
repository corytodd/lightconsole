using System.Collections.Generic;
using System.Windows.Controls;

namespace LightConsole
{
    /// <summary>
    /// Interaction logic for RoomTabControl.xaml
    /// </summary>
    public partial class RoomTabControl : UserControl
    {
        private List<TabItem> m_controls;

        public RoomTabControl()
        {
            InitializeComponent();

            m_controls = new List<TabItem>();
        }

        public void AddControl(UserControl control, string name)
        {
            TabItem item = new TabItem();
            item.Content = control;
            item.Header = name;

            m_controls.Add(item);
            tabcontrol.Items.Add(item);
        }
    }
}
