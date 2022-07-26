using System;
using System.Numerics;

namespace DarkFileTransfer.Common
{
    public class MorletWavelet : Wavelet
    {
        Complex[] wavelet;
        public MorletWavelet(double carrier, int length)
        {
            wavelet = new Complex[length];
            Complex add = Complex.Zero;
            for (int i = 0; i < length; i++)
            {
                double lengthM1 = length - 1.0;
                double time = (i - length / 2) / lengthM1;
                double sigma = 4 / (Math.Tau * carrier);
                Complex topPart = (Complex.ImaginaryOne * Math.Tau * carrier * time) - (0.5 * Math.Pow(time / sigma, 2.0));
                wavelet[i] = Complex.Exp(topPart);
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