using System.Diagnostics;
using System.Drawing;

namespace Kontur.ImageTransformer
{
    public interface IAlgrorithm
    {
        void Process(byte[] rgbValues, int x);
    }
}