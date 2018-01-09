using System.Threading.Tasks;

namespace Kontur.ImageTransformer
{
    internal class Threshold : IAlgorithm
    {
        public void Process(byte[] rgbValues, int x)
        {
            var height = rgbValues.Length / 4;
            Parallel.For(0, height, new ParallelOptions { MaxDegreeOfParallelism = 16 }, i =>
            {
                var j = i * 4;
                var intensity = (byte) ((rgbValues[j] + rgbValues[j + 1] + rgbValues[j + 2]) / 3);
                intensity = (byte) (intensity >= 255 * x / 100.0 ? 255 : 0);
                rgbValues[j] = intensity;
                rgbValues[j + 1] = intensity;
                rgbValues[j + 2] = intensity;
            });
        }
    }
}