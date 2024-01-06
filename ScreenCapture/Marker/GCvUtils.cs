using Emgu.CV;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ccauto.Marker
{
    public class GCvUtils
    {
        public static Mat bufToMat(byte[] buf)
        {
            Mat omat = new Mat();
            CvInvoke.Imdecode(buf, Emgu.CV.CvEnum.ImreadModes.Color, omat);
            return omat;
        }

        public static byte[] MatToBuff(Mat mat, string ext = ".bmp")
        {
            var buffer = new VectorOfByte();
            CvInvoke.Imencode(ext, mat, buffer);
            var bytes = buffer.ToArray();
            return bytes;
        }

        public class MatchPoints
        {
            public int X;
            public int Y;
            public long val;
            public MatchPoints(int x, int y, float val)
            {
                X = x;
                Y = y;
                this.val = (long)val;
            }

            public System.Drawing.Point ToPoint()
            {
                return new System.Drawing.Point(X, Y);
            }
        }
        public static List<MatchPoints> templateMatch(Mat template, Mat img, float threashold, int keep = 20, Mat mask = null)
        {
            List<MatchPoints> points = new List<MatchPoints>();
            Mat res = new Mat();            
            CvInvoke.MatchTemplate(img, template, res, Emgu.CV.CvEnum.TemplateMatchingType.Sqdiff, mask);
            var thres = res.GetData();
            int TooCloseDist = template.Width / 2;
            if (TooCloseDist < 2) TooCloseDist = 2;

            List<MatchPoints> removes = new List<MatchPoints>();
            for (int y = 0; y < thres.GetLength(0); y++)
            {
                for (int x = 0; x < thres.GetLength(1); x++)
                {
                    var value = (float)thres.GetValue(y, x);
                    value = Math.Abs(value);
                    if (value > threashold) continue;
                    if(points.Count == 0) points.Add(new MatchPoints(x,y,value));                                        
                    if (points.Last().val > value || points.Count < keep)
                    {

                        bool tooClose = false;
                        removes.Clear();
                        foreach (var item in points)
                        {
                            if (Math.Abs(item.X - x) <= TooCloseDist
                                && Math.Abs(item.Y - y) <= TooCloseDist)
                            {
                                if (item.val <= value)
                                {
                                    tooClose = true;
                                    break;
                                } else
                                {
                                    removes.Add(item);
                                }
                            }
                        }
                        foreach (var item in removes)
                        {
                            points.Remove(item);
                        }
                        if (tooClose) continue;
                        if (points.Count > keep)
                            points.RemoveAt(points.Count - 1);
                        points.Add(new MatchPoints(x, y, value));
                        points.Sort((a, b) => { 
                            return (int)(a.val - b.val);
                        });
                        
                    }                                                            
                }
            }
            Console.Write("====>");
            foreach (var item in points)
            {
                Console.Write("(" + item.X + "," + item.Y + ")=" + item.val);
                Console.Write(",");
            }
            Console.WriteLine();
            return points;
        }
    }
}
