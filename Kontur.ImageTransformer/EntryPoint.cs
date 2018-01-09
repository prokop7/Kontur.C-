using System;

namespace Kontur.ImageTransformer
{
    public class EntryPoint
    {
        public static void Main(string[] args)
        {
            using (var server = new AsyncHttpServer())
            {
                server.Start("http://+:8080/");
                Console.Out.WriteLine("Started");
                while (true)
                    Console.ReadKey(true);
            }
        }
    }
}
