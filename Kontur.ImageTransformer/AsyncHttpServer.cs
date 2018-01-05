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
                        _spinLock.Enter(ref _lock);
                        if (_numRequests >= 50)
                        {
                            Console.Out.WriteLine(_numRequests + "=");
                            _lock = false;
                            _spinLock.Exit();
                            context.Response.StatusCode = 429;
                            context.Response.Close();
                            continue;
                        }
                        _numRequests++;
                        Console.Out.WriteLine(_numRequests + "+");
                        _lock = false;
                        _spinLock.Exit();
                        Task.Run(() => HandleContextAsync(context));
                    }
                    else Thread.Sleep(0);
                }
                catch (ThreadAbortException e)
                {
                    Console.Out.WriteLine(e);
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
            if (!CheckContentLength(listenerContext))
            {
                Console.Out.WriteLine("=");
                return false;
            }
            return true;
        }

        private static void PostHandle(HttpListenerContext listenerContext)
        {
            _spinLock.Enter(ref _lock);
            Console.Out.WriteLine(_numRequests + "-");
            _numRequests--;
            _lock = false;
            _spinLock.Exit();
        }

        private static async Task HandleContextAsync(HttpListenerContext listenerContext)
        {
            if (!PreHandler(listenerContext))
                return;
            try
            {
                var asyncHandler = new AsyncHandler();
                asyncHandler.Handle(listenerContext);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                PostHandle(listenerContext);
            }
        }


        private readonly HttpListener _listener;

        private Thread _listenerThread;
        private bool _disposed;
        private volatile bool _isRunning;
    }
}