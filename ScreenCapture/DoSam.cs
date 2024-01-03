using ccAuto2;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;


namespace ccauto
{
    public class EasyRect
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
    }
    internal class DoSam
    {
        public static void ExecuteSamProcess(EventRequester.RequestAndResult samResult, EasyRect selRect, Action endAction, bool doSam = true)
        {
            samResult.doRequest(tbf =>
            {
                var tmStr = DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
                var fileName = "d:\\segan\\out\\coc\\test" + tmStr + ".png";
                if (selRect != null)
                {
                    using (var srcBmp = Bitmap.FromStream(new MemoryStream(tbf)))
                    {
                        using (var croppedImg = CropImage((Bitmap)srcBmp, new Rectangle(selRect.X, selRect.Y, selRect.Width, selRect.Height),
                            new Rectangle(0, 0, selRect.Width, selRect.Height)))
                        {
                            var stream = new MemoryStream();
                            croppedImg.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                            tbf = stream.ToArray();
                        }
                    }
                }
                File.WriteAllBytes(fileName, tbf);
                if (doSam)
                {
                    var command = "d:\\segan\\testwithfile.bat " + fileName;
                    ExecuteCmd(command);
                    Console.WriteLine("cmd.exe /c " + command);
                }
                endAction();
            });
        }

        public static Bitmap CropImage(Bitmap img, Rectangle srcRect, Rectangle dstRect)
        {
            Bitmap newBmp = new Bitmap((int)dstRect.Width, (int)dstRect.Height);
            using (var g = Graphics.FromImage(newBmp))
            {
                g.DrawImage(img, dstRect, srcRect, GraphicsUnit.Pixel);
            }
            return newBmp;
        }


        static void ExecuteCmd(string command)
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
    }
}
