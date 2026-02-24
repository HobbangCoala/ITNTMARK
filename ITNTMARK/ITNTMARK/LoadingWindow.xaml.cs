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
using WpfAnimatedGif;

namespace ITNTMARK
{
    /// <summary>
    /// LoadingWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LoadingWindow : Window
    {
        public event Action Canceled;

        public LoadingWindow()
        {
            InitializeComponent();
            LoadGif();
        }

        private void LoadGif()
        {
            var uri = new Uri("pack://application:,,,/Image/loading_2.gif");
            var image = new BitmapImage(uri);
            ImageBehavior.SetAnimatedSource(gifImage, image);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Canceled?.Invoke();
        }
    }
}
