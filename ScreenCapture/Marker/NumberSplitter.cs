using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace ccauto.Marker
{
    public class NumberSplitter
    {
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
        public static List<Rectangle> SplitCocNumbers(Mat orig)
        {
            Func<byte,byte,byte, bool> isWhite = (c1,c2,c3) =>
            {
                return c1 > 0xf0 && c2 > 0xf0 && c3 > 0xf0;
            };
            bool foundStart = false;
            int startX = -1;
            int minY = 100, maxY = 0;
            List<XPos> all = new List<XPos>();
            var data = orig.GetData();
            for (int x = 0; x < orig.Width; x++)
            {
                int curXminY = 100;
                int curXmaxY = 0;
                int whiteCount = 0;
                int endX = -1;
                for (int y = 0; y < orig.Height; y++)
                {
                    //Color c = orig.GetPixel(x, y);
                    var c1 = (byte)data.GetValue(y, x ,0);
                    var c2 = (byte)data.GetValue(y, x, 1);
                    var c3 = (byte)data.GetValue(y, x, 2);
                    if (isWhite(c1,c2,c3))
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
                }
                else
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
                }
                else
                {
                    if (cur.x1 <= prev.x2) cur.x1++;
                }
                var next = i + 1 >= all.Count ? null : all[i + 1];
                cur.x2 += 2;
                if (next == null)
                {
                    if (cur.x2 >= orig.Width) cur.x2 = orig.Width - 1;
                }
                else
                {
                    if (cur.x2 >= next.x1) cur.x2--;
                }
                prev = cur;
            }


            List<Rectangle> rects = new List<Rectangle>();
            minY -= 3;
            if (minY < 0) minY = 0;
            maxY += 3;
            if (maxY >= orig.Height) maxY = orig.Height - 1;
            foreach (var cur in all)
            {
                var rect = new Rectangle(cur.x1, minY, cur.x2 - cur.x1, maxY - minY);
                rects.Add(rect);
            }
            return rects;
        }
    }
}
