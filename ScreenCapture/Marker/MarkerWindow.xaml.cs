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
using Point = System.Windows.Point;

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



        static EasyRect PointsToRect(Point p1, Point p2)
        {
            EasyRect r = new EasyRect();
            r.X = (int)p1.X;
            r.Width = (int)(p2.X - p1.X);
            if (r.Width < 0)
            {
                r.X = (int)p2.X;
                r.Width = -r.Width;
            }
            r.Y = (int)p1.Y;
            r.Height = (int)(p2.Y - p1.Y);
            if (r.Height < 0)
            {
                r.Y = (int)p2.Y;
                r.Height = -r.Height;
            }
            //Console.WriteLine("X=" + r.X.ToString("0.0") + " y=" + r.Y.ToString("0.0")+ " width="+r.Width.ToString("0") + " h="+r.Height.ToString("0"));
            return r;
        }

        Point mouseDownP;
        Point mouseUpP;
        EasyRect curSelRect = null;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            canvImg.MouseLeftButtonDown += (s, mouseE) =>
            {
                Point p = mouseE.GetPosition(canvImg);
                mouseDownP = p;
                mouseUpP.X = -1;
                canvImg.CaptureMouse();
                mouseE.Handled = true;
                mouseDspRect.Width = 0; mouseDspRect.Height = 0;
                mouseDspRect.Visibility = Visibility.Visible;
            };
            canvImg.MouseLeftButtonUp += (s, mouseE) =>
            {
                Point p = mouseE.GetPosition(canvImg);
                mouseUpP = p;
                Console.WriteLine("release mouse cap");
                canvImg.ReleaseMouseCapture();
                mouseE.Handled = true;
            };
            canvImg.MouseRightButtonUp += (s, mouseE) =>
            {
                mouseUpP.X = -1;
                mouseDownP.X -= 1;
                mouseDspRect.Visibility = Visibility.Collapsed;
                curSelRect = null;
            };

            canvImg.MouseMove += (s, mouseE) =>
            {
                if (mouseDownP.X < 0) return;
                if (mouseUpP.X >= 0) return;
                Point p = mouseE.GetPosition(canvImg);
                var r = PointsToRect(mouseDownP, p);
                if (r.Width <= 0) return;
                if (r.Height <= 0) return;
                curSelRect = r;
                Canvas.SetLeft(mouseDspRect, r.X);
                Canvas.SetTop(mouseDspRect, r.Y);
                mouseDspRect.Width = r.Width;
                mouseDspRect.Height = r.Height;
                var brush = new System.Windows.Media.SolidColorBrush();
                brush.Color = System.Windows.Media.Colors.Red;
                brush.Opacity = 0.5;
                mouseE.Handled = true;
            };
        }
    }
}
