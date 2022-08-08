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
        public double[] errors = new double[8192];
        int bufferOffset = 0;
        int bufferPos = 0;
        long totalPos = 0;
        Decoder decoder;
        MorletWavelet waveletSync = new MorletWavelet(16, 512);

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

            if (state == SyncState.DESYNC && bufferPos > 1024)
            {
                Complex val = waveletSync.Convolute(buffer, 0);
                if (val.Magnitude < 0.2)
                {
                    Array.Copy(buffer, 1024, buffer2, 0, buffer2.Length - 1024);
                    double[] temp = buffer;
                    buffer = buffer2;
                    buffer2 = temp;
                    bufferPos -= 1024;
                }
                else
                {
                    state = SyncState.FREQ_SYNCED;
                }
            }

            if (state == SyncState.FREQ_SYNCED && bufferPos > 2048)
            {
                int bestPos = 0;
                double lowestError = double.PositiveInfinity;
                for (int i = 0; i < 1024; i++)
                {
                    double error = 0;
                    //Try to match half of the guard interval
                    for (int j = 0; j < 32; j++)
                    {
                        error += Math.Abs(buffer[i + j] - buffer[i + j + 512]);
                    }
                    errors[i] = error;
                    if (error < lowestError)
                    {
                        lowestError = error;
                        bestPos = i;
                    }
                }
                if (lowestError < 0.05)
                {
                    bufferOffset = bestPos;
                    Complex[] fftTest1 = GetFFTFromDoubleArray(buffer, bestPos, 512);

                    if (fftTest1[16].Magnitude > 0.5)
                    {
                        state = SyncState.GUARD_SYNCED;
                    }
                    else
                    {
                        if (bestPos > 576)
                        {
                            bestPos -= 576;
                        }
                        else
                        {
                            bestPos += 576;
                        }
                        Complex[] fftTest2 = GetFFTFromDoubleArray(buffer, bestPos, 512);
                        if (fftTest2[16].Magnitude > 0.5)
                        {
                            bufferOffset = bestPos;
                            state = SyncState.GUARD_SYNCED;
                        }
                        else
                        {
                            state = SyncState.DESYNC;
                            bufferPos = 0;
                        }
                    }
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
                    double thisError = Math.Abs(CalculatePhaseError(fft[12].Phase, fft[16].Phase));
                    if (thisError < lowestError)
                    {
                        lowestError = thisError;
                        newOffset = i;
                    }
                }
                if (Math.Abs(lowestError) < 0.05)
                {
                    bufferOffset += newOffset;
                    state = SyncState.SYMBOL_SYNCED;
                }
                else
                {
                    state = SyncState.DESYNC;
                    bufferPos = 0;
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
                //Bufferflip
                Array.Copy(buffer, 576, buffer2, 0, bufferPos - 576);
                bufferPos -= 576;
                double[] temp = buffer;
                buffer = buffer2;
                buffer2 = temp;

                //Debug
                Complex[] phase1 = GetFFTAmpPhase(fft);
                ZeroUnusedCarriers(fft);
                ZeroUnusedCarriers(phase1);
                FFTDebug.WriteHalfComplexArrayToFile(fft, "sync1.csv");
                FFTDebug.WriteHalfComplexArrayToFile(phase1, "sync1a.csv");
                Complex c = new Complex(-2, -2);
                double thisError = CalculatePhaseError(fft[12].Phase, fft[16].Phase) / 4;
                double offset = CalculatePhaseError(Math.PI / 2.0, fft[12].Phase) - thisError * 12;
                for (int i = 0; i < 512; i++)
                {
                    Complex rotate = Complex.FromPolarCoordinates(1, thisError * i + offset);
                    fft[i] = fft[i] * rotate;
                }
                Complex[] phase2 = GetFFTAmpPhase(fft);
                FFTDebug.WriteHalfComplexArrayToFile(fft, "sync2.csv");
                ZeroUnusedCarriers(phase2);
                FFTDebug.WriteHalfComplexArrayToFile(phase2, "sync2a.csv");
                decoder.ProcessFrame(fft);
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
                if (i < 32 && i != 12 && i != 16 && i != 20 && i != 24)
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
    }
}