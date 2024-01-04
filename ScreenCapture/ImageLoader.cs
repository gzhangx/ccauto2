using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Markup;

namespace ccAuto2
{
    public class ImageStore
    {
        public string name { get; private set; }
        public Emgu.CV.Mat image { get; private set; }
        public Rectangle rect { get; private set; }
        public ImageStore(string name, Mat image, Rectangle rect)
        {
            this.name = name;
            this.image = image;
            this.rect = rect;
        }
    }
    internal class ImageLoader
    {
        public System.Collections.Generic.List<ImageStore> stores = new System.Collections.Generic.List<ImageStore>();
        public ImageLoader() { }

        public void LoadAll()
        {
            var imageDir = "images";
            var files = Directory.GetFiles(imageDir);
            foreach (var file in files)
            {
                var img = Emgu.CV.CvInvoke.Imread(file);
                var matches = new Regex("([A-Za-z0-9_]+)\\((\\d+,\\d+,\\d+,\\d+)\\)").Match(file);
                var name = matches.Groups[1].Value;
                var posStr = matches.Groups[2].Value;
                var poss = posStr.Split(',');
                var rec = new Rectangle(int.Parse(poss[0]), int.Parse(poss[1]),
                    int.Parse(poss[2]), int.Parse(poss[3]));

                var store = new ImageStore(name, img, rec);                
                stores.Add(store);
                //var croped= new Mat(img, new Rectangle(10, 10, 100, 20));
                //CvInvoke.Imwrite("test.png", croped);
            }

        }


        public static double CompareToMat(Mat src, ImageStore stored)
        {
            if (stored.rect.Location.X > src.Size.Width) return 0;
            if (stored.rect.Location.Y > src.Size.Height) return 0;
            var cropSrc = new Mat(src, stored.rect);
            var output = new Mat();
            CvInvoke.Compare(cropSrc, stored.image, output, Emgu.CV.CvEnum.CmpType.Equal);
            var r = new Mat();
            CvInvoke.CvtColor(output, r, Emgu.CV.CvEnum.ColorConversion.Rgb2Gray);
            int diffs = CvInvoke.CountNonZero(r);
            return diffs*1.0/(stored.rect.Width*stored.rect.Height);
        }

        public double CompareFromArray(byte[] data, ImageStore stored)
        {
            Mat omat = bufToMat(data);            
            return CompareToMat(omat, stored);
        }
        public static Mat bufToMat(byte[] buf)
        {
            return ccauto.Marker.CvUtils.bufToMat(buf);
        }
    }
}
