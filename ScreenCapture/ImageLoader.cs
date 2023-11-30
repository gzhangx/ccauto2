using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Documents;

namespace WPFCaptureSample
{
    public class ImageStore
    {
        public string name;
        public Emgu.CV.Mat image;
        public Rectangle rect;
    }
    internal class ImageLoader
    {
        System.Collections.Generic.List<ImageStore> stores = new System.Collections.Generic.List<ImageStore>();
        public ImageLoader() { }

        public void LoadAll()
        {
            var imageDir = "images";
            var files = Directory.GetFiles(imageDir);
            foreach (var file in files)
            {
                var img = Emgu.CV.CvInvoke.Imread(file);
                var matches = new Regex("([A-Za-z_]+)\\((\\d+,\\d+,\\d+,\\d+)\\)").Match(file);
                var name = matches.Groups[1].Value;
                var posStr = matches.Groups[2].Value;
                var poss = posStr.Split(',');
                var rec = new Rectangle(int.Parse(poss[0]), int.Parse(poss[1]),
                    int.Parse(poss[2]), int.Parse(poss[3]));

                var store = new ImageStore
                {
                    name = name,
                    image = img,
                    rect = rec,
                };
                stores.Add(store);
                //var croped= new Mat(img, new Rectangle(10, 10, 100, 20));
                //CvInvoke.Imwrite("test.png", croped);
            }

        }
    }
}
