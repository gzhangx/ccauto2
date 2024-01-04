using Emgu.CV.Face;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Windows.Storage.Streams;

namespace ccauto.Marker
{
    /// <summary>
    /// Interaction logic for MarkerWindow.xaml
    /// </summary>
    public partial class MarkerWindow : Window
    {
        public MarkerWindow()
        {
            InitializeComponent();
        }

        private void btnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //Bitmap bmp = (Bitmap)Bitmap.FromFile(openFileDialog.FileName);  
                using (Stream memory = File.OpenRead(openFileDialog.FileName))
                {
                    memory.Position = 0;
                    BitmapImage bitmapimage = new BitmapImage();
                    bitmapimage.BeginInit();
                    bitmapimage.StreamSource = memory;
                    bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapimage.EndInit();
                    canvImg.Source = bitmapimage;
                }
            }
        }
    }
}
