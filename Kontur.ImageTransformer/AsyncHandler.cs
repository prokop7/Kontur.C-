using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Text.RegularExpressions;

namespace Kontur.ImageTransformer
{
    public static class AsyncHandler
    {
        private static readonly IAlgorithm _threshold = new Threshold();
        private static readonly IAlgorithm _greyscale = new Greyscale();
        private static readonly IAlgorithm _sepia = new Sepia();

        private enum MethodType
        {
            Grayscale,
            Sepia,
            Threshold,
            None
        }

        public static bool Handle(HttpListenerContext listenerContext)
        {
            var (method, x, y, width, height, threshold) = ParseUrl(listenerContext.Request.RawUrl);
            if (method == MethodType.None)
            {
                listenerContext.Response.StatusCode = 400;
                listenerContext.Response.Close();
                return false;
            }
            var bitmap = GetBitmap(listenerContext, x, y, width, height);
            if (bitmap == null) return false;

            var(bitmapData, ptr, bytes, rgbValues) = GetData(bitmap);

            Process(method, rgbValues, threshold);

            StoreData(listenerContext, bitmap, bitmapData, ptr, bytes, rgbValues);
            return true;
        }

        private static (MethodType, int, int, int, int, int) ParseUrl(string url)
        {
            var pattern =
                new Regex("/process/(grayscale|threshold(\\(\\d+\\))|sepia)/(-?\\d+),(-?\\d+),(-?\\d+),(-?\\d+)");
            try
            {
                var match = pattern.Match(url);
                MethodType methodType;
                var threshold = 0;
                switch (match.Groups[1].Value)
                {
                    case "grayscale":
                        methodType = MethodType.Grayscale;
                        break;
                    case "threshold":
                        methodType = MethodType.Threshold;
                        threshold = Convert.ToInt32(match.Groups[2].Value);
                        if (threshold < 0 || threshold > 100)
                            return (MethodType.None, 0, 0, 0, 0, 0);
                        break;
                    case "sepia":
                        methodType = MethodType.Sepia;
                        break;
                    default:
                        methodType = MethodType.None;
                        break;
                }
                var x = Convert.ToInt32(match.Groups[3].Value);
                var y = Convert.ToInt32(match.Groups[4].Value);
                var width = Convert.ToInt32(match.Groups[5].Value);
                var height = Convert.ToInt32(match.Groups[6].Value);
                return (methodType, x, y, width, height, threshold);
            }
            catch (Exception e)
            {
                return (MethodType.None, 0, 0, 0, 0, 0);
            }
        }

        private static Bitmap GetBitmap(HttpListenerContext listenerContext, int x, int y, int width, int height)
        {
            if (listenerContext.Request.ContentLength64 == 0)
            {
                listenerContext.Response.StatusCode = 400;
                listenerContext.Response.Close();
                return null;
            }
            var init = new Bitmap(listenerContext.Request.InputStream);
            var x1 = Math.Min(x, x + width);
            var x2 = Math.Max(x, x + width);
            var y1 = Math.Min(y, y + height);
            var y2 = Math.Max(y, y + height);
            Bound(ref x1, maxBound: init.Width);
            Bound(ref x2, maxBound: init.Width);
            Bound(ref y1, maxBound: init.Height);
            Bound(ref y2, maxBound: init.Height);

            void Bound(ref int value, int minBound = 0, int maxBound = 0)
            {
                value = Math.Min(value, maxBound);
                value = Math.Max(value, minBound);
            }

            width = x2 - x1;
            height = y2 - y1;
            if (width * height == 0)
            {
                listenerContext.Response.StatusCode = 204;
                listenerContext.Response.Close();
                return null;
            }
            var ret = init.Clone(new Rectangle(x1, y1, width, height), init.PixelFormat);
            init.Dispose();
            return ret;
        }

        private static (BitmapData, IntPtr, int, byte[]) GetData(Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite,
                bitmap.PixelFormat, new BitmapData());
            var ptr = bitmapData.Scan0;
            var bytes = Math.Abs(bitmapData.Stride) * bitmap.Height;
            var rgbValues = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
            return (bitmapData, ptr, bytes, rgbValues);
        }

        private static void Process(MethodType listenerContext, byte[] rgbValues, int threshold)
        {
            switch (listenerContext)
            {
                case MethodType.Grayscale:
                    _greyscale.Process(rgbValues);
                    break;
                case MethodType.Sepia:
                    _sepia.Process(rgbValues);
                    break;
                case MethodType.Threshold:
                    _threshold.Process(rgbValues, threshold);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(listenerContext), listenerContext, null);
            }
        }

        private static void StoreData(HttpListenerContext listenerContext, Bitmap bitmap, BitmapData bitmapData,
            IntPtr ptr,
            int bytes, byte[] rgbValues)
        {
            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
            bitmap.UnlockBits(bitmapData);
            bitmap.Save(listenerContext.Response.OutputStream, ImageFormat.Png);
            listenerContext.Response.ContentType = "image/png";
            listenerContext.Response.OutputStream.Close();
            listenerContext.Response.StatusCode = (int) HttpStatusCode.OK;
        }
    }
}