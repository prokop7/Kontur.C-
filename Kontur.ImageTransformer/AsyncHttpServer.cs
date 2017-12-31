using System;
using System.CodeDom;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer
{
    internal class AsyncHttpServer : IDisposable
    {
        public AsyncHttpServer()
        {
            listener = new HttpListener();
        }

        public void Start(string prefix)
        {
            lock (listener)
            {
                if (isRunning) return;

                listener.Prefixes.Clear();
                listener.Prefixes.Add(prefix);
                listener.Start();

                listenerThread = new Thread(Listen)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Highest
                };
                listenerThread.Start();

                isRunning = true;
            }
        }

        public void Stop()
        {
            lock (listener)
            {
                if (!isRunning) return;

                listener.Stop();

                listenerThread.Abort();
                listenerThread.Join();

                isRunning = false;
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            Stop();

            listener.Close();
        }

        private void Listen()
        {
            while (true)
            {
                try
                {
                    if (listener.IsListening)
                    {
                        var context = listener.GetContext();
                        Task.Run(() => HandleContextAsync(context));
                    }
                    else Thread.Sleep(0);
                }
                catch (ThreadAbortException)
                {
                    return;
                }
                catch (Exception error)
                {
                    // TODO: log errors
                }
            }
        }

        private async Task HandleContextAsync(HttpListenerContext listenerContext)
        {
            try
            {
                var bitmap = new Bitmap(listenerContext.Request.InputStream);
                Stopwatch stopwatch = Stopwatch.StartNew();
                var bitmapData = bitmap.LockBits(new Rectangle(0,0,bitmap.Width, bitmap.Height),ImageLockMode.ReadWrite, bitmap.PixelFormat, new BitmapData());
                var threshold = new Threshold();
                var greyscale = new Greyscale();
                var sepia = new Sepia();
                var ptr = bitmapData.Scan0;
                int bytes  = Math.Abs(bitmapData.Stride) * bitmap.Height;
                byte[] rgbValues = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);
                Regex pattern = new Regex("/t(\\d+)");
                switch (listenerContext.Request.RawUrl)
                {
                    case "/s":
                        sepia.Process(rgbValues);
                        break;
                    case "/g":
                        greyscale.Process(rgbValues);
                        break;
                    default:
                        if (Regex.IsMatch(listenerContext.Request.RawUrl, "/t(\\d+)"))
                        {
                            Match match = pattern.Match(listenerContext.Request.RawUrl);
                            var s = match.Groups[1].Value;
                            threshold.Process(rgbValues, int.Parse(s));
                        }
                        break;
                }
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
                bitmap.UnlockBits(bitmapData);
                stopwatch.Stop();
                bitmap.Save(listenerContext.Response.OutputStream, ImageFormat.Png);
                listenerContext.Response.OutputStream.Close();
                listenerContext.Response.StatusCode = (int) HttpStatusCode.OK;
                Console.WriteLine(stopwatch.Elapsed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
//            using (var writer = new StreamWriter(listenerContext.Response.OutputStream))
//                writer.WriteLine("Hello, world!");
        }

        private readonly HttpListener listener;

        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;
    }
}