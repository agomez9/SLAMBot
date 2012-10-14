using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Drawing;
using System.IO;
using System.Windows.Threading;

namespace SLAMBotServer
{
    /// <summary>
    /// Interaction logic for ExampleKinect.xaml
    /// </summary>
    public partial class ExampleKinect : Window
    {
        public ExampleKinect()
        {
            InitializeComponent();   
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public void LoadImage(byte[] image)
        {
            var bitmapImage = new BitmapImage();
            Stream stream = new MemoryStream(image);
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze();            
            image1.Dispatcher.Invoke(DispatcherPriority.Send, new Action(delegate() { image1.Source = bitmapImage; }));
        }
    }
}
