using System.Drawing;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer
{
    public class Greyscale: IAlgorithm
    {
        public void Process(byte[] rgbValues, int x = 0)
        {
            var height = rgbValues.Length / 4;
            Parallel.For(0, height, new ParallelOptions { MaxDegreeOfParallelism = 16 }, i =>
            {
                var j = i * 4;
                byte intensity = (byte) ((rgbValues[j] + rgbValues[j + 1] + rgbValues[j + 2]) / 3);
                rgbValues[j] = intensity;
                rgbValues[j + 1] = intensity;
                rgbValues[j + 2] = intensity;
            });
        }
    }
}