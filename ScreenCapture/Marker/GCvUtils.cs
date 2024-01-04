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

        public static List<Point> templateMatch(Mat template, Mat img, float threasHold = 0.9f, Mat mask = null)
        {
            List<Point> points = new List<Point>();
            Mat res = new Mat();
            CvInvoke.MatchTemplate(img, template, res, Emgu.CV.CvEnum.TemplateMatchingType.Ccoeff, mask);
            var thres = res.GetData();
            for (int y = 0; y < thres.GetLength(0); y++)
            {
                for (int x = 0; x < thres.GetLength(1); x++)
                {
                    var value = (float)thres.GetValue(y, x);

                    if (value > threasHold)
                    {
                        points.Add(new Point(x, y));
                    }
                }
            }
            return points;
        }
    }
}
