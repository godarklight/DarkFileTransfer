using System;
using System.Numerics;

namespace DarkFileTransfer.Common
{
    public class MorletWavelet
    {
        Complex[] wavelet;
        public MorletWavelet(int carrier, int length)
        {
            wavelet = new Complex[length];
            for (int i = 0; i < length; i++)
            {
                double phase = (i / ((double)length - 1)) * Math.Tau * carrier;
                double gausX = (i * 2 / ((double)length - 1)) - 1;
                gausX = gausX * 5;
                double gaus = Math.Exp(-(gausX * gausX));
                wavelet[i] = new Complex(Math.Sin(phase) * gaus, Math.Cos(phase) * gaus);
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