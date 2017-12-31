using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Kontur.ImageTransformer
{
    public class Sepia : IAlgorithm
    {
        public void Process(byte[] rgbValues, int x = 0)
        {
            var height = rgbValues.Length / 4;
            Parallel.For(0, height, new ParallelOptions { MaxDegreeOfParallelism = 16 }, i =>
            {
                var j = i * 4;
                var (oldR, oldG, oldB) = (rgbValues[j + 2], rgbValues[j + 1], rgbValues[j]);
                rgbValues[j] = (byte) Math.Min(255, oldR * .272 + oldG * .534 + oldB * .131);
                rgbValues[j + 1] = (byte) Math.Min(255, oldR * .349 + oldG * .686 + oldB * .168);
                rgbValues[j + 2] = (byte) Math.Min(255, oldR * .393 + oldG * .769 + oldB * .189);
            });
        }
    }
}