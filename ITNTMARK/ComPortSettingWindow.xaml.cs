using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ITNTCOMMM;
using ITNTUTIL;
using ITNTCOMMON;

namespace ITNTMARK
{
    /// <summary>
    /// ComPortSettingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ComPortSettingWindow : Window
    {
        public int DeviceType = 0;
        public bool saveFlag = false;

        public ComPortSettingWindow(int device)
        {
            InitializeComponent();
            DeviceType = device;
            saveFlag = false;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if(DeviceType == 0)
            {

            }
            else if(DeviceType == 1)
            {

            }
            saveFlag = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
