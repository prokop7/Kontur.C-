using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace Kontur.ImageTransformer
{
    public interface IAlgorithm
    {
        void Process(byte[] rgbValues, int x = 0);
    }
}