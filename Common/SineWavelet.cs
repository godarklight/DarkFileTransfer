using System;
using System.Numerics;

namespace DarkFileTransfer.Common
{
    //The "slow" fourier transform for a single bin
    public class SineWavelet : Wavelet
    {
        Complex[] wavelet;
        public SineWavelet(double carrier, int length)
        {
            wavelet = new Complex[length];
            for (int i = 0; i < length; i++)
            {
                double phase = (i / (double)length) * Math.Tau * carrier;
                wavelet[i] = new Complex(Math.Cos(phase), Math.Sin(phase));
            }
        }

        public Complex Convolute(double[] input, int offset)
        {
            Complex retVal = Complex.Zero;
            for (int i = 0; i < wavelet.Length; i++)
            {
                retVal += input[offset + i] * wavelet[i];
            }
            return retVal;
        }
    }
}