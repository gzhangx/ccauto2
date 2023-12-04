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
            gameResult = cocCapture.creator.registerNewEvent("gameResult");
            samResult = cocCapture.creator.registerNewEvent("samResult");
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
            });
        }


        private async void PickerButton_Click(object sender, RoutedEventArgs e)
        {
            cocCapture.creator.StopCapture();
            WindowComboBox.SelectedIndex = -1;
            await cocCapture.creator.StartPickerCaptureAsync(this);
        }

        double dpiX = 1.0;
        double dpiY = 1.0;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //var interopWindow = new WindowInteropHelper(this);
            //hwnd = interopWindow.Handle;            
            var presentationSource = PresentationSource.FromVisual(this);
            
            if (presentationSource != null)
            {
                dpiX = presentationSource.CompositionTarget.TransformToDevice.M11;
                dpiY = presentationSource.CompositionTarget.TransformToDevice.M22;
            }
            var controlsWidth = (float)(ControlsGrid.ActualWidth * dpiX);
            controlsWidth = 0;
            cocCapture.creator.init(imgWin, controlsWidth);
            //InitComposition(controlsWidth + 100);
            //InitComposition(0);
            InitWindowListAndStart();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            var curText = StopButton.Content as string;
            if (String.Equals(curText, "Stop Capture"))
            {
                cocCapture.creator.StopCapture();
                WindowComboBox.SelectedIndex = -1;
                StopButton.Content = "Start Capture";                
            }
            else
            {
                InitWindowListAndStart();
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

        
        private void InitWindowListAndStart()
        {
            var error = cocCapture.InitWindowListAndStart();
            if ("OK".Equals(error))
            {
                return;
            }
            MessageBox.Show(error);
            
        }


       
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            btnCaptureAndSeg.IsEnabled = false;
            DoSam.ExecuteSamProcess(samResult, () =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    btnCaptureAndSeg.IsEnabled = true;
                }));
            });
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


       

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            cocCapture.Dispose();
            imgWin.Close();
        }
    }
}
