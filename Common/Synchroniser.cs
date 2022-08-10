using System;
using System.Numerics;
using System.IO;

namespace DarkFileTransfer.Common
{
    public class Synchroniser
    {
        private double freqOffset = 0.0;
        private double skewOffset = 0.0;
        private SyncState state = SyncState.DESYNC;
        public double[] buffer = new double[32768];
        public double[] buffer2 = new double[32768];
        int bufferOffset = 0;
        int bufferPos = 0;
        long totalPos = 0;
        Decoder decoder;
        Wavelet waveletSync = new SineWavelet(12, 512);
        int frameNumber = 0;

        public bool Completed
        {
            private set;
            get;
        }

        public Synchroniser(Decoder decoder)
        {
            Completed = false;
            this.decoder = decoder;
        }

        public void ReceiveData(byte[] inputData)
        {
            double[] copy = PcmConvert.ConvertPCMToDouble(inputData, inputData.Length);
            Array.Copy(copy, 0, buffer, bufferPos, copy.Length);
            bufferPos += copy.Length;
            totalPos += copy.Length;

            if (state == SyncState.DESYNC && bufferPos >= 512)
            {
                bufferOffset = bufferPos - 512;
                Complex val = waveletSync.Convolute(buffer, bufferOffset);
                if (val.Magnitude > 0.1)
                {
                    state = SyncState.FREQ_SYNCED;
                }
                else if (bufferPos > 16384)
                {
                    Array.Copy(buffer, 512, buffer2, 0, buffer2.Length - 512);
                    double[] temp = buffer;
                    buffer = buffer2;
                    buffer2 = temp;
                    bufferPos -= 512;
                    bufferOffset -= 512;
                }
            }

            if (state == SyncState.FREQ_SYNCED && bufferPos - bufferOffset > 2048)
            {
                int bestPos = 0;
                double lowestError = double.PositiveInfinity;
                for (int i = 0; i < 512; i++)
                {
                    double error = 0;
                    //Try to match half of the guard interval
                    for (int j = 0; j < 32; j++)
                    {
                        error += Math.Abs(buffer[i + j + bufferOffset] - buffer[i + j + bufferOffset + 512]);
                    }
                    if (error < lowestError)
                    {
                        lowestError = error;
                        bestPos = i;
                    }
                }
                if (lowestError < 0.05)
                {
                    bufferOffset += bestPos;
                    Complex[] fftTest = GetFFTFromDoubleArray(buffer, bufferOffset, 512);

                    if (fftTest[8].Magnitude > 0.5 && fftTest[12].Magnitude > 0.5 && fftTest[16].Magnitude > 0.5)
                    {
                        state = SyncState.GUARD_SYNCED;
                    }
                    else
                    {
                        state = SyncState.DESYNC;
                    }
                }
                else
                {
                    state = SyncState.DESYNC;
                }
            }

            if (state == SyncState.GUARD_SYNCED && bufferPos - bufferOffset > 8192)
            {
                int newOffset = 0;
                double lowestError = double.PositiveInfinity;
                Complex[] fftin = new Complex[512];
                for (int i = 0; i < 64; i++)
                {
                    for (int j = 0; j < 512; j++)
                    {
                        fftin[j] = buffer[bufferOffset + i + j];
                    }
                    Complex[] fft = FFT.CalcFFT(fftin);
                    double thisError = GetPilotError(fft);
                    if (thisError < lowestError)
                    {
                        lowestError = thisError;
                        newOffset = i;
                    }
                }
                if (lowestError < 0.05)
                {
                    bufferOffset += newOffset;
                    state = SyncState.SYMBOL_SYNCED;
                    while (bufferOffset > 576)
                    {
                        Complex[] fftback = GetFFTFromDoubleArray(buffer, bufferOffset - 576, 512);
                        double pilotError = GetPilotError(fftback);
                        if (pilotError < 0.05)
                        {
                            bufferOffset -= 576;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    state = SyncState.DESYNC;
                }
            }

            while (state == SyncState.SYMBOL_SYNCED && (bufferPos - bufferOffset) > 1024)
            {
                Complex[] fftin = new Complex[512];
                for (int i = 0; i < 512; i++)
                {
                    fftin[i] = new Complex(buffer[bufferOffset + i], 0);
                }
                Complex[] fft = FFT.CalcFFT(fftin);
                if (fft[8].Magnitude < 0.1 || fft[12].Magnitude < 0.1 || fft[16].Magnitude < 0.1)
                {
                    state = SyncState.DESYNC;
                    break;
                }

                //Bufferflip
                Array.Copy(buffer, 576, buffer2, 0, bufferPos - 576);
                bufferPos -= 576;
                double[] temp = buffer;
                buffer = buffer2;
                buffer2 = temp;

                //Fix the rotation of the carriers
                double thisError = CalculatePhaseError(fft[8].Phase, fft[16].Phase) / 8.0;
                double offset = CalculatePhaseError(0, fft[12].Phase) - thisError * 12;
                for (int i = 0; i < 512; i++)
                {
                    Complex rotate = Complex.FromPolarCoordinates(1, thisError * i + offset);
                    fft[i] = fft[i] * rotate;
                }
                decoder.ProcessFrame(fft);
                ZeroUnusedCarriers(fft);
                FFTDebug.WriteHalfComplexArrayToFile(fft, $"frame/{frameNumber}.csv");
                Complex[] ampPhase = GetFFTAmpPhase(fft);
                ZeroUnusedCarriers(ampPhase);
                FFTDebug.WriteHalfComplexArrayToFile(ampPhase, $"frame/{frameNumber}.csv-ap");
                frameNumber++;
            }
        }

        private double CalculatePhaseError(double a, double b)
        {
            double retVal = a - b;
            if (retVal > Math.PI)
            {
                retVal -= Math.Tau;
            }
            if (retVal < -Math.PI)
            {
                retVal += Math.Tau;
            }
            return retVal;
        }

        private Complex[] GetFFTFromDoubleArray(double[] input, int index, int length)
        {
            Complex[] fftin = new Complex[length];
            for (int i = 0; i < length; i++)
            {
                fftin[i] = input[index + i];
            }
            return FFT.CalcFFT(fftin);
        }

        private Complex[] GetFFTAmpPhase(Complex[] input)
        {
            Complex[] retVal = new Complex[input.Length];
            for (int i = 0; i < retVal.Length; i++)
            {
                retVal[i] = new Complex(input[i].Magnitude, input[i].Phase);
            }
            return retVal;
        }

        private void ZeroUnusedCarriers(Complex[] input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                bool wipe = false;
                if (i % 2 == 1)
                {
                    wipe = true;
                }
                if (i < 32 && i % 4 != 0 || i == 0 || i == 4)
                {
                    wipe = true;
                }
                if (i > 150)
                {
                    wipe = true;
                }
                if (wipe)
                {
                    input[i] = Complex.Zero;
                }
            }
        }

        private double GetPilotError(Complex[] fft)
        {
            double thisError = Math.Abs(CalculatePhaseError(fft[8].Phase, fft[12].Phase));
            thisError += Math.Abs(CalculatePhaseError(fft[12].Phase, fft[16].Phase));
            return thisError;
        }
    }
}