using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
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
        Window parent;
        string configDir = null;
        Env env = new Env();
        public MarkerWindow(Window parent)
        {
            InitializeComponent();
            this.parent = parent;
            configDir = env.getEnv("-dir");
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

        bool pragmaticlyChangingTxtPositin = false;
        void UpdatePosition()
        {
            try
            {
                pragmaticlyChangingTxtPositin = true;
                if (mouseDownP.X < 0) return;
                txtPosition.Text = mouseDownP.X.ToString("0") + "," + ((int)mouseDownP.Y);
                if (curSelRect == null) return;
                txtPosition.Text = curSelRect.X + "," + curSelRect.Y + "," + curSelRect.Width + "," + curSelRect.Height;
            } finally
            {
                pragmaticlyChangingTxtPositin = false;
            }
        }

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
                UpdatePosition();
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
                Point p = mouseE.GetPosition(canvImg);
                var r = PointsToRect(mouseDownP, p);
                mouseE.Handled = true;
                Dispatcher.Invoke(() =>
                {
                    txtInfo.Text = "(" + p.X.ToString("0") + "," + p.Y.ToString("0") + ")";
                });
                if (mouseDownP.X < 0) return;
                if (mouseUpP.X >= 0) return;                
                SelectCropImage(r);
                UpdatePosition();
            };

            for (int i = 0; i < 10; i++)
            {
                cmbKeepItems.Items.Add((i+1).ToString());
            }
            cmbKeepItems.SelectedIndex = 5;
            
            cmbSavedImages.SelectionChanged += (_sne, schangeeve)=>
            {
                var file = cmbSavedImages.SelectedItem.ToString();
                ShowSelectedImage(file);

            };
            initClasses();
            initSavedImageFiles();
            //ShowSelectedImage();

        }        

        void initClasses()
        {
            var lines = File.ReadAllLines(configDir + "\\names.txt");
            foreach (var line in lines)
            {
                cmbClassNames.Items.Add(line.Trim());
            }
            if (cmbClassNames.SelectedIndex < 0) cmbClassNames.SelectedIndex = 0;
        }
        void initSavedImageFiles()
        {
            var files = Directory.EnumerateFiles(configDir+"\\images");
            Regex reg = new Regex("test\\d+-\\d+-\\d+-\\d+.png$");
            foreach (var file in files)
            {
                if (!reg.IsMatch(file)) continue;
                cmbSavedImages.Items.Add(file);
            }
            if (cmbSavedImages.SelectedIndex < 0) cmbSavedImages.SelectedIndex = 0;
        }

        bool SelectCropImage(EasyRect r)
        {
            if (r.X + r.Width >= origImage.Width)
            {
                r.Width = origImage.Width - r.X;
            }
            if (r.Y + r.Height >= origImage.Height)
            {
                r.Height = origImage.Height - r.Y;
            }
            if (r.Width <= 0) return false;
            if (r.Height <= 0) return false;

            curSelRect = r;
            selectedMat = new Mat(origImage, r.toRectangle());
            Canvas.SetLeft(mouseDspRect, r.X);
            Canvas.SetTop(mouseDspRect, r.Y);
            mouseDspRect.Visibility = Visibility.Visible;
            mouseDspRect.Width = r.Width;
            mouseDspRect.Height = r.Height;
            return true;
        }

        void ShowSelectedImage(string fname)
        {
            //Debug Quick
            var fileBytes = File.ReadAllBytes(fname);
            if (fileBytes == null) return;
            origImage = GCvUtils.bufToMat(fileBytes);
            if (origImage.DataPointer  == IntPtr.Zero) {
                MessageBox.Show("Invalid file " + fname);
                return;
            }
            //Bitmap bmp = (Bitmap)Bitmap.FromFile(openFileDialog.FileName);  

            ShowImageFromBytes(fileBytes);

            //SelectCropImage(new EasyRect() { X=235, Y=331, Width=25, Height=24,});
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
            var lists = GCvUtils.templateMatch(selectedMat, origImage, cmbKeepItems.SelectedIndex);
            foreach (var item in lists)
            {
                //Console.WriteLine("doing at "+item.X+"/"+item.Y);
                CvInvoke.Rectangle(newMat, new System.Drawing.Rectangle((int)item.X, (int)item.Y, selectedMat.Width, selectedMat.Height), new Emgu.CV.Structure.MCvScalar(), 1, Emgu.CV.CvEnum.LineType.EightConnected);
                CvInvoke.PutText(newMat, item.val.ToString(), new System.Drawing.Point(item.X, item.Y-10), Emgu.CV.CvEnum.FontFace.HersheyPlain, 1, new Emgu.CV.Structure.MCvScalar(), 2);
            }

            var imgBuf = GCvUtils.MatToBuff(newMat);
            //imgBuf = GCvUtils.MatToBuff(origImage);
            ShowImageFromBytes(imgBuf);
        }

        private void txtPosition_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (pragmaticlyChangingTxtPositin) return;
            var text = txtPosition.Text;
            Regex reg = new Regex("(?<x>\\d+),(?<y>\\d+),(?<w>\\d+),(?<h>\\d+)");
            var match = reg.Match(text);
            if (!match.Success)
            {
                return;
            }
            ;
            
            var r = new EasyRect();
            r.X = int.Parse(match.Groups["x"].Value);
            r.Y = int.Parse(match.Groups["y"].Value);
            r.Width = int.Parse(match.Groups["w"].Value);  
            r.Height = int.Parse(match.Groups["h"].Value); 
            SelectCropImage(r);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            parent.Close();
        }

        private void btnSplitNumber_Click(object sender, RoutedEventArgs e)
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

            var recs = NumberSplitter.SplitCocNumbers(selectedMat);            
            Mat newMat = origImage.Clone();

            if (curSelRect.Width < 200 && curSelRect.Height < 100)
            {
                SaveImageAsPPM(selectedMat, configDir+"/selppm.txt"); 
            }

            var clr = new Emgu.CV.Structure.MCvScalar();
            var lineType = Emgu.CV.CvEnum.LineType.EightConnected;
            foreach (var item in recs)
            {
                //Console.WriteLine("doing at "+item.X+"/"+item.Y);
                CvInvoke.Rectangle(newMat, new System.Drawing.Rectangle((int)item.X + curSelRect.X, (int)item.Y + curSelRect.Y, item.Width, item.Height), clr, 1, lineType);
                //CvInvoke.PutText(newMat, item.val.ToString(), new System.Drawing.Point(item.X, item.Y - 10), Emgu.CV.CvEnum.FontFace.HersheyPlain, 1, new Emgu.CV.Structure.MCvScalar(), 2);
            }

            var imgBuf = GCvUtils.MatToBuff(newMat);
            //imgBuf = GCvUtils.MatToBuff(origImage);
            ShowImageFromBytes(imgBuf);
            txtInfo.Text = "Splited " + recs.Count + " parts";
        }

        public static void SaveImageAsPPM(Mat mat, string fileName)
        {
            StringBuilder sb = new StringBuilder();
            var data = mat.GetData();
            for (int y = 0; y < mat.Height; y++)
            {
                for (int x = 0; x < mat.Width; x++)
                {
                    var cr = (byte)data.GetValue(y, x, 0);
                    var cg = (byte)data.GetValue(y, x, 1);
                    var cb = (byte)data.GetValue(y, x, 2);
                    var rtn = cr.ToString("X2") + cg.ToString("X2") + cb.ToString("X2");
                    sb.Append(rtn).Append(" ");
                }
                sb.Append("\r\n");
            }
            File.WriteAllText(fileName, sb.ToString());
        }
    }
}
