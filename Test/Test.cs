using System;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Test
{
    public class Test
    {
        private HttpClient Client { get; set; } = new HttpClient();

        [Fact]
        async void ResponseCheck()
        {
            var length = 3;
            var tasks = new Task<HttpResponseMessage>[length];
            var responses = new HttpResponseMessage[length];


            for (var i = 0; i < tasks.Length; i++)
            {
                using (var stream = File.Open("C:\\Users\\Monopolis\\Desktop\\download.png", FileMode.Open,
                    FileAccess.Read, FileShare.Read))
                {
                    var streamContent = new StreamContent(stream);
                    await streamContent.LoadIntoBufferAsync();
                    tasks[i] = Client.PostAsync("http://localhost:8080/process/grayscale/0,00,500,400", streamContent);
                }
                Thread.Sleep(100);
            }
            for (var i = 0; i < tasks.Length; i++)
            {
                var task = tasks[i];
                responses[i] = await task;
            }
            foreach (var response in responses)
                Console.Out.WriteLine(response);
        }
    }
}