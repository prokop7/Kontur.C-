using System;
using System.CodeDom;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer
{
    internal class AsyncHttpServer : IDisposable
    {
        public AsyncHttpServer()
        {
            _listener = new HttpListener();
        }

        public void Start(string prefix)
        {
            lock (_listener)
            {
                if (_isRunning) return;

                _listener.Prefixes.Clear();
                _listener.Prefixes.Add(prefix);
                _listener.Start();

                _listenerThread = new Thread(Listen)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Highest
                };
                _listenerThread.Start();

                _isRunning = true;
            }
        }

        public void Stop()
        {
            lock (_listener)
            {
                if (!_isRunning) return;

                _listener.Stop();

                _listenerThread.Abort();
                _listenerThread.Join();

                _isRunning = false;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            Stop();

            _listener.Close();
        }

        private void Listen()
        {
            while (true)
            {
                try
                {
                    if (_listener.IsListening)
                    {
                        var context = _listener.GetContext();
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
                    Console.Out.WriteLine(error);
                }
            }
        }

        private static int _numRequests = 0;
        private static SpinLock _spinLock = new SpinLock(false);
        private static bool _lock;


        private static bool CheckContentLength(HttpListenerContext listenerContext)
        {
            if (listenerContext.Request.ContentLength64 / 1024 <= 200) return true;
            listenerContext.Response.StatusCode = 400;
            listenerContext.Response.Close();
            return false;
        }

        private static bool PreHandler(HttpListenerContext listenerContext)
        {
            _spinLock.Enter(ref _lock);
            if (_numRequests >= 2)
            {
                _lock = false;
                _spinLock.Exit();
                listenerContext.Response.StatusCode = 429;
                listenerContext.Response.Close();
                return false;
            }
            _numRequests++;
            _lock = false;
            Console.Out.WriteLine(_numRequests);
            _spinLock.Exit();
            return CheckContentLength(listenerContext);
        }

        private static void PostHandle(HttpListenerContext listenerContext)
        {
            _spinLock.Enter(ref _lock);
            _numRequests--;
            _lock = false;
            _spinLock.Exit();
        }

        private static async Task HandleContextAsync(HttpListenerContext listenerContext)
        {
            if (!PreHandler(listenerContext))
                return;
            var asyncHandler = new AsyncHandler();
            await Task.Delay(10000);
            if (!asyncHandler.Handle(listenerContext))
                return;
            PostHandle(listenerContext);
        }


        private readonly HttpListener _listener;

        private Thread _listenerThread;
        private bool _disposed;
        private volatile bool _isRunning;
    }
}