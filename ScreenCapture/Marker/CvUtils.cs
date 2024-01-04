using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ccauto.Marker
{
    public class CvUtils
    {
        public static Mat bufToMat(byte[] buf)
        {
            Mat omat = new Mat();
            CvInvoke.Imdecode(buf, Emgu.CV.CvEnum.ImreadModes.Color, omat);
            return omat;
        }
    }
}
