using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Util;
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

        Mat origImage = null;
        Mat selectedMat = null;
        private void btnLoadImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new System.Windows.Forms.OpenFileDialog();
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var fileBytes = File.ReadAllBytes(openFileDialog.FileName);
                origImage = GCvUtils.bufToMat(fileBytes);
                //Bitmap bmp = (Bitmap)Bitmap.FromFile(openFileDialog.FileName);  

                ShowImageFromBytes(fileBytes);
            }
        }

        void ShowImageFromBytes(byte[] buf)
        {            
            using (Stream memory = new MemoryStream(buf))
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
                selectedMat = new Mat(origImage, r.toRectangle());
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

        private void btnFindAllSimilar_Click(object sender, RoutedEventArgs e)
        {
            if (origImage == null)
            {
                MessageBox.Show("No image");
                return;
            }
            if (selectedMat == null)
            {
                MessageBox.Show("Nothing selected");
                return;
            }

            Mat newMat = origImage.Clone();
            var lists = GCvUtils.templateMatch(selectedMat, origImage);
            foreach (var item in lists)
            {
                //Console.WriteLine("doing at "+item.X+"/"+item.Y);
                CvInvoke.Rectangle(newMat, new System.Drawing.Rectangle((int)item.X, (int)item.Y, selectedMat.Width, selectedMat.Height), new Emgu.CV.Structure.MCvScalar(), 1, Emgu.CV.CvEnum.LineType.EightConnected);
            }

            var imgBuf = GCvUtils.MatToBuff(newMat);
            //imgBuf = GCvUtils.MatToBuff(origImage);
            ShowImageFromBytes(imgBuf);
        }
    }
}
