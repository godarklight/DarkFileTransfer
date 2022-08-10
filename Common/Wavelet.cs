using System;
using System.Numerics;

namespace DarkFileTransfer.Common
{
    public interface Wavelet
    {
        public Complex Convolute(double[] input, int offset);
    }
}