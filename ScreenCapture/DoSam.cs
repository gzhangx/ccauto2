using ccAuto2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;
using Windows.Globalization;


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
                            if (croppedImg.Width < 100 && croppedImg.Height < 40)
                            {
                                StringBuilder sb = new StringBuilder();
                                for (int y = 0; y < croppedImg.Height; y++)
                                {
                                    for (int x = 0; x < croppedImg.Width; x++)
                                    {
                                        Color c= croppedImg.GetPixel(x, y);
                                        var rtn =  c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
                                        sb.Append(rtn).Append(" ");
                                    }
                                    sb.Append("\r\n");
                                }
                                File.WriteAllText(fileName+".txt", sb.ToString());
                                var imgs = SplitCocNumbers(croppedImg);
                                Directory.CreateDirectory(fileName+"_dir");
                                for (int i = 0; i < imgs.Count; i++) {
                                    var img = imgs[i];
                                    img.Save(fileName + "_dir\\" + i + ".png");
                                }
                            }
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

        class XPos
        {
            public int x1;
            public int x2;
            public XPos(int x1, int x2)
            {
                this.x1 = x1;
                this.x2 = x2;
            }
        }
        static List<Bitmap> SplitCocNumbers(Bitmap orig)
        {
            Func<Color, bool> isWhite = (Color c) =>
            {
                return c.R > 0xf0 && c.G > 0xf0 && c.B >= 0xf0;
            };
            bool foundStart = false;
            int startX = -1;
            int minY = 100, maxY = 0;
            List<XPos> all = new List<XPos>();
            for (int x = 0; x < orig.Width; x++)
            {
                int curXminY = 100;
                int curXmaxY = 0;
                int whiteCount = 0;
                int endX = -1;
                for (int y = 0; y < orig.Height; y++)
                {
                    Color c = orig.GetPixel(x, y);                                        
                    if (isWhite(c))
                    {
                        whiteCount++;
                        if (y > curXmaxY) curXmaxY = y;
                        if (y < curXminY) curXminY = y;
                    }                    
                }
                if (!foundStart)
                {
                    if (whiteCount > 2)
                    {
                        foundStart = true;
                        startX = x;
                    }                    
                } else
                {
                    if (whiteCount == 0)
                    {
                        foundStart = false;
                        endX = x;
                    }                    
                }

                if (foundStart)
                {
                    if (curXmaxY > maxY) maxY = curXmaxY;
                    if (curXminY < minY) minY = curXmaxY;
                }
                if (endX > 0)
                {
                    all.Add(new XPos(startX, endX));
                }
            }

            XPos prev = null;
            for (int i = 0; i < all.Count; i++)
            {
                var cur = all[i];
                cur.x1 -= 2;
                if (prev == null)
                {                    
                    if (cur.x1 < 0) cur.x1 = 0;
                } else
                {
                    if (cur.x1 <= prev.x2) cur.x1++;
                }
                var next = i+1 >= all.Count?null: all[i + 1];
                cur.x2 += 2;
                if (next == null)
                {                    
                    if (cur.x2 >= orig.Width ) cur.x2 = orig.Width - 1;
                } else
                {
                    if (cur.x2 >= next.x1) cur.x2--;
                }
                prev = cur;
            }
            

            List<Bitmap> bmps = new List<Bitmap>();
            minY -= 3;
            if (minY < 0) minY = 0;
            maxY += 3;
            if (maxY >= orig.Height) maxY = orig.Height - 1;
            foreach(var cur in all)
            {
                var bmp = CropImage(orig, new Rectangle(cur.x1, minY, cur.x2 - cur.x1,maxY - minY));
                bmps.Add(bmp);
            }
            return bmps;
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
        public static Bitmap CropImage(Bitmap img, Rectangle srcRect)
        {
            Rectangle dstRect = new Rectangle(0,0, srcRect.Width, srcRect.Height);
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
