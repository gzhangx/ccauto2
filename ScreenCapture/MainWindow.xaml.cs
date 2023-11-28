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
using Composition.WindowsRuntimeHelpers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.UI.Composition;


namespace WPFCaptureSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, BasicCapture.AskCopy
    {        
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
            InitWindowList();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            creator.StopCapture();
            WindowComboBox.SelectedIndex = -1;
        }

        private void WindowComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = (ComboBox)sender;
            var process = (Process)comboBox.SelectedItem;

            if (process != null)
            {
                creator.StopCapture();
                var hwnd = process.MainWindowHandle;
                try
                {
                    this.creator.StartHwndCapture(hwnd);
                }
                catch (Exception)
                {
                    Debug.WriteLine($"Hwnd 0x{hwnd.ToInt32():X8} is not valid for capture!");
                    processes.Remove(process);
                    comboBox.SelectedIndex = -1;
                }
            }
        }


        private void InitWindowList()
        {
            if (ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8))
            {
                var pp = Process.GetProcesses();
                var processesWithWindows = from p in Process.GetProcesses()
                                           where !string.IsNullOrWhiteSpace(p.MainWindowTitle) && WindowEnumerationHelper.IsWindowValidForCapture(p.MainWindowHandle)
                                           && string.Equals(p.MainWindowTitle, "BlueStacks App Player")
                                           select p;
                processes = new ObservableCollection<Process>(processesWithWindows);
                WindowComboBox.ItemsSource = processes;
                if (processes.Count == 1)
                {
                    WindowComboBox.SelectedIndex = 0;
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
            btnTest.IsEnabled = false;
            //ConvertToBitmap(sample.Visual);
            captureOne = true;
        }

        async void BasicCapture.AskCopy.doSurface(IDirect3DSurface surface)
        {
            if (captureOne)
            {
                captureOne = false;
                var tbf = await MainCaptureCreator.ConvertSurfaceToPngCall(surface);                
                File.WriteAllBytes("d:\\segan\\input\\test.png", tbf);
                await Dispatcher.BeginInvoke(new Action(() =>
                {
                    btnTest.IsEnabled = true;
                }));
            }
        }
    }
}
