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
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;



namespace WPFCaptureSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, BasicCapture.AskCopy
    {
        const string BSAP_WindowName = "BlueStacks App Player";
        private ObservableCollection<Process> processes;

        private MainCaptureCreator creator = new MainCaptureCreator();

        public MainWindow()
        {
            InitializeComponent();

#if DEBUG
            // Force graphicscapture.dll to load.
            var picker = new GraphicsCapturePicker();
#endif
        }

        private async void PickerButton_Click(object sender, RoutedEventArgs e)
        {
            creator.StopCapture();
            WindowComboBox.SelectedIndex = -1;
            await creator.StartPickerCaptureAsync();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //var interopWindow = new WindowInteropHelper(this);
            //hwnd = interopWindow.Handle;            
            var presentationSource = PresentationSource.FromVisual(this);
            double dpiX = 1.0;
            double dpiY = 1.0;
            if (presentationSource != null)
            {
                dpiX = presentationSource.CompositionTarget.TransformToDevice.M11;
                dpiY = presentationSource.CompositionTarget.TransformToDevice.M22;
            }
            var controlsWidth = (float)(ControlsGrid.ActualWidth * dpiX);
            creator.init(this, controlsWidth, this);
            //InitComposition(controlsWidth + 100);
            //InitComposition(0);
            InitWindowListAndStart();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            var curText = StopButton.Content as string;
            if (String.Equals(curText, "Stop Capture"))
            {
                creator.StopCapture();
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
            if (ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8))
            {
                var pp = Process.GetProcesses();
                var processesWithWindows = from p in Process.GetProcesses()
                                           where !string.IsNullOrWhiteSpace(p.MainWindowTitle) && WindowEnumerationHelper.IsWindowValidForCapture(p.MainWindowHandle)
                                           && string.Equals(p.MainWindowTitle, BSAP_WindowName)
                                           select p;
                processes = new ObservableCollection<Process>(processesWithWindows);
                WindowComboBox.ItemsSource = processes;
                if (processes.Count == 1)
                {
                    WindowComboBox.SelectedIndex = 0;
                    creator.StopCapture();
                    var hwnd = processesWithWindows.First().MainWindowHandle;
                    try
                    {
                        creator.StartHwndCapture(hwnd);
                        StopButton.Content = "Stop Capture";
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine($"Hwnd 0x{hwnd.ToInt32():X8} is not valid for capture!");                        
                    }
                } 
            }
            else
            {
                WindowComboBox.IsEnabled = false;
            }
        }


        

                


        

        bool captureOne = false;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            btnCaptureAndSeg.IsEnabled = false;
            //ConvertToBitmap(sample.Visual);
            captureOne = true;
        }

        async void BasicCapture.AskCopy.doSurface(IDirect3DSurface surface)
        {
            if (captureOne)
            {
                captureOne = false;
                var tbf = await MainCaptureCreator.ConvertSurfaceToPngCall(surface);
                var tmStr = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
                var fileName = "d:\\segan\\out\\test\\test" + tmStr + ".png";
                File.WriteAllBytes(fileName, tbf);
                var command = "d:\\segan\\testwithfile.bat " + fileName;
                ExecuteCmd(command);
                Console.WriteLine("cmd.exe /c " + command);
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    btnCaptureAndSeg.IsEnabled = true;
                }));
            }
        }

        void ExecuteCmd(string command)
        {
            int ExitCode;
            ProcessStartInfo ProcessInfo;
            Process Process;

            ProcessInfo = new ProcessStartInfo("cmd.exe", "/c " + command);
            ProcessInfo.CreateNoWindow = false;
            ProcessInfo.UseShellExecute = true;

            Process = Process.Start(ProcessInfo);
            Process.WaitForExit();

            ExitCode = Process.ExitCode;
            //Process.Close();
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            //Task.Run(() =>
            {
                
                var wnd = Win32Helper.FindWindow(null, BSAP_WindowName);
                Win32Helper.SetForegroundWindow(wnd);
                System.Threading.Thread.Sleep(3000);
                //System.Windows.Forms.SendKeys.SendWait("DDD");
                for (int i = 0; i < 3; i++)
                {
                    Console.WriteLine("sending");
                    Win32Helper.SendKey('D');
                    System.Threading.Thread.Sleep(1000);
                    Console.WriteLine("Done sending");
                }
            }
            //);
            

        }
    }
}
