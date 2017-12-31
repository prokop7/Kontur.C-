using System.Diagnostics;
using System.Drawing;

namespace Kontur.ImageTransformer
{
    public interface IAlgorithm
    {
        void Process(byte[] rgbValues, int x=0);
    }
}