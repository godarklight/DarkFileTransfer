
using System;
using System.Numerics;

namespace DarkFileTransfer.Common
{
    public static class FFT
    {
        //https://youtu.be/h7apO7q16V0?t=1334
        public static Complex[] CalcFFT(Complex[] p)
        {
            int n = p.Length;
            if (n == 1)
            {
                return p;
            }
            //Calculate omega
            Complex e = new Complex(Math.E, 0);
            Complex exponent = Math.Tau * Complex.ImaginaryOne / n;
            Complex w = Complex.Pow(e, exponent);

            //Split lists
            Complex[] pe = new Complex[n / 2];
            Complex[] po = new Complex[n / 2];
            for (int i = 0; i < n / 2; i++)
            {
                pe[i] = p[i * 2];
                po[i] = p[i * 2 + 1];
            }

            //Split fft
            Complex[] ye = CalcFFT(pe);
            Complex[] yo = CalcFFT(po);

            //Calculate
            Complex[] y = new Complex[n];
            for (int j = 0; j < n / 2; j++)
            {
                Complex wj = Complex.Pow(w, j);
                y[j] = ye[j] + wj * yo[j];
                y[j + n / 2] = ye[j] - wj * yo[j];
            }

            return y;
        }

        private static Complex[] CalcIFFTReal(Complex[] p)
        {
            int n = p.Length;
            if (n == 1)
            {
                return p;
            }
            //Calculate omega
            Complex e = new Complex(Math.E, 0);
            Complex exponent = -Math.Tau * Complex.ImaginaryOne / n;
            Complex w = Complex.Pow(e, exponent);

            //Split lists
            Complex[] pe = new Complex[n / 2];
            Complex[] po = new Complex[n / 2];
            for (int i = 0; i < n / 2; i++)
            {
                pe[i] = p[i * 2];
                po[i] = p[i * 2 + 1];
            }

            //Split fft
            Complex[] ye = CalcIFFTReal(pe);
            Complex[] yo = CalcIFFTReal(po);

            //Calculate
            Complex[] y = new Complex[n];
            for (int j = 0; j < n / 2; j++)
            {
                Complex wj = Complex.Pow(w, j);
                y[j] = ye[j] + wj * yo[j];
                y[j + n / 2] = ye[j] - wj * yo[j];
            }

            return y;
        }

        public static Complex[] CalcIFFT(Complex[] p)
        {
            Complex[] y = CalcIFFTReal(p);
            //Normalise
            for (int i = 0; i < p.Length; i++)
            {
                y[i] = y[i] * (1 / (double)p.Length);
            }
            return y;
        }
    }
}
