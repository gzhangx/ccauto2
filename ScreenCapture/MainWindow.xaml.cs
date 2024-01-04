//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using CaptureSampleCore;
using ccauto;
using ccauto.Marker;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;



namespace ccAuto2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {       
        private CocCapture cocCapture = new CocCapture();        

        private Window imgWin = new Window();
        EventRequester.RequestAndResult gameResult, samResult;
        EasyRect curSelRect = null;
        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            // Force graphicscapture.dll to load.
            var picker = new GraphicsCapturePicker();
#endif            

            imgWin.Width= 20;
            imgWin.Height= 20;
            imgWin.Show();
            gameResult = cocCapture.registerNewEvent("gameResult");
            samResult = cocCapture.registerNewEvent("samResult");
            //imgWin.Hide();

            cocCapture.Init(gameResult, buf =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (cocCapture._needToDie) return;
                    using (MemoryStream memory = new MemoryStream(buf))
                    {
                        memory.Position = 0;
                        BitmapImage bitmapimage = new BitmapImage();
                        bitmapimage.BeginInit();
                        bitmapimage.StreamSource = memory;
                        bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapimage.EndInit();
                        canvImg.Source = bitmapimage;
                    }
                });
            }, delMs =>
            {
                Dispatcher.Invoke(() =>
                {
                    int toNext = delMs / 1000;
                    if (toNext < 0) toNext = 0;
                    this.Title = "Next Cap " + toNext;
                });
            });
        }


        private async void PickerButton_Click(object sender, RoutedEventArgs e)
        {
            cocCapture.StopCapture();
            WindowComboBox.SelectedIndex = -1;
            await cocCapture.StartPickerCaptureAsync(this);
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mouseDownP.X = -1;
            Console.WriteLine("Window loaded reset ouse p");                               
            
            cocCapture.init(imgWin, 0);
            //don't start capture initially
            //InitWindowListAndStart();
            canvImg.MouseLeftButtonDown += (s,mouseE)=>
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

            //Debug Quick
            btnMarkerWin_Click(null,null);
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
            r.Y = (int) p1.Y;
            r.Height = (int)(p2.Y - p1.Y);
            if (r.Height <0)
            {
                r.Y = (int)p2.Y;
                r.Height = -r.Height;
            }
            //Console.WriteLine("X=" + r.X.ToString("0.0") + " y=" + r.Y.ToString("0.0")+ " width="+r.Width.ToString("0") + " h="+r.Height.ToString("0"));
            return r;
        }

        Point mouseDownP;
        Point mouseUpP;

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            var curText = StopButton.Content as string;
            const string StopCapture = "Stop Capturing";
            if (cocCapture.isStarted())
            {
                cocCapture.StopCapture();
                WindowComboBox.SelectedIndex = -1;
                StopButton.Content = "Start Capture";                
            }
            else
            {
                InitWindowListAndStart();
                StopButton.Content = StopCapture;
            }
        }

        private void WindowComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var process = (Process)comboBox.SelectedItem;

            if (process != null)
            {
                
            }
        }

        
        private bool InitWindowListAndStart()
        {
            var error = cocCapture.InitWindowListAndStart();
            if ("OK".Equals(error))
            {
                return true;
            }
            MessageBox.Show(error);
            return false;
            
        }


       
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            bool started = cocCapture.isStarted();
            if (!started)
            {
                if (!InitWindowListAndStart()) return;
            }
            btnCaptureAndSeg.IsEnabled = false;
            new Thread(() =>
            {
                DoSam.ExecuteSamProcess(samResult, curSelRect , () =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (!started)
                        {
                            cocCapture.StopCapture();
                        }
                        btnCaptureAndSeg.IsEnabled = true;
                    }));
                });
            }).Start();            
            //ConvertToBitmap(sample.Visual);            
        }
      

        bool checkMouse = false;
        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            //new ImageLoader().LoadAll();
            //Task.Run(() =>
            {
                
                var wnd = Win32Helper.FindWindow(null, CocCapture.BSAP_WindowName);
                Win32Helper.SetForegroundWindow(wnd);
                System.Threading.Thread.Sleep(1000);
                //System.Windows.Forms.SendKeys.SendWait("DDD");

                Win32Helper.SendKey('W', 0, true);
                System.Threading.Thread.Sleep(100);
                Win32Helper.SendKey('W', 0, false);

                Win32Helper.Rect rect = new Win32Helper.Rect();
                Win32Helper.GetWindowRect(wnd, ref rect);
                Win32Helper.SetCursorPos(rect.Left, rect.Top);
            }
            //);

            if (!checkMouse)
            {
                checkMouse = true;
                Task.Run(() =>
                {
                    int count = 0;
                    while(checkMouse)
                    {
                        System.Threading.Thread.Sleep(100);
                        Win32Helper.POINT pt = new Win32Helper.POINT();
                        Win32Helper.GetCursorPos(out pt);

                        
                        count++;
                        if (count >  10) {
                            pt.X++;
                            count = 0;
                            Win32Helper.SetCursorPos(pt.X, pt.Y);
                        }
                        Dispatcher.Invoke(() =>
                        {
                            txtInfo.Text = "pt" + pt.X + "," + pt.Y;
                        });
                    }

                });
            } else
            {
                checkMouse = false;
            }

        }

        private void btnProcessCoc_Click(object sender, RoutedEventArgs e)
        {
            cocCapture.ProcessCOC = !cocCapture.ProcessCOC;

            btnProcessCoc.Content = cocCapture.ProcessCOC ? "Stop Process COC" : "Start Process COC";


        }

        private void btnCaptureOnly_Click(object sender, RoutedEventArgs e)
        {
            bool started = cocCapture.isStarted();
            if (!started)
            {
                if (!InitWindowListAndStart()) return;
            }
            btnCaptureAndSeg.IsEnabled = false;
            gameResult.doRequest(bf => { });
            new Thread(() =>
            {
                DoSam.ExecuteSamProcess(samResult, curSelRect, () =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (!started)
                        {
                            cocCapture.StopCapture();
                        }
                        btnCaptureAndSeg.IsEnabled = true;
                    }));
                }, false);
            }).Start();

        }

        private void btnMarkerWin_Click(object sender, RoutedEventArgs e)
        {
            var win = new MarkerWindow(this);
            win.Show();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cocCapture.Dispose();
            imgWin.Close();
        }
    }
}
