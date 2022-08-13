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
        Wavelet waveletSync = new SineWavelet(12, Constants.FFT_SIZE);
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

            if (state == SyncState.DESYNC && bufferPos >= Constants.FFT_SIZE)
            {
                bufferOffset = bufferPos - Constants.FFT_SIZE;
                Complex val = waveletSync.Convolute(buffer, bufferOffset);
                if (val.Magnitude > 0.1)
                {
                    state = SyncState.FREQ_SYNCED;
                }
                else if (bufferPos > 16384)
                {
                    Array.Copy(buffer, Constants.FFT_SIZE, buffer2, 0, buffer2.Length - Constants.FFT_SIZE);
                    double[] temp = buffer;
                    buffer = buffer2;
                    buffer2 = temp;
                    bufferPos -= Constants.FFT_SIZE;
                    bufferOffset -= Constants.FFT_SIZE;
                }
            }

            if (state == SyncState.FREQ_SYNCED && bufferPos - bufferOffset > Constants.FFT_SIZE * 4)
            {
                int bestPos = 0;
                double lowestError = double.PositiveInfinity;
                for (int i = 0; i < Constants.FFT_SIZE; i++)
                {
                    double error = 0;
                    //Try to match half of the guard interval
                    for (int j = 0; j < Constants.GUARD_SIZE / 2; j++)
                    {
                        error += Math.Abs(buffer[i + j + bufferOffset] - buffer[i + j + bufferOffset + Constants.FFT_SIZE]);
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
                    Complex[] fftTest = GetFFTFromDoubleArray(buffer, bufferOffset, Constants.FFT_SIZE);

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

            if (state == SyncState.GUARD_SYNCED && bufferPos - bufferOffset > Constants.FFT_SIZE * 16)
            {
                int newOffset = 0;
                double lowestError = double.PositiveInfinity;
                Complex[] fftin = new Complex[Constants.FFT_SIZE];
                for (int i = 0; i <= 64; i++)
                {
                    Complex[] fft = GetFFTFromDoubleArray(buffer, bufferOffset + i, Constants.FFT_SIZE);
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
                    while (bufferOffset > (Constants.FFT_SIZE + Constants.GUARD_SIZE))
                    {
                        Complex[] fftback = GetFFTFromDoubleArray(buffer, bufferOffset - (Constants.FFT_SIZE + Constants.GUARD_SIZE), Constants.FFT_SIZE);
                        double pilotError = GetPilotError(fftback);
                        if (pilotError < 0.05)
                        {
                            bufferOffset -= (Constants.FFT_SIZE + Constants.GUARD_SIZE);
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

            while (state == SyncState.SYMBOL_SYNCED && (bufferPos - bufferOffset) > Constants.FFT_SIZE)
            {
                Complex[] fft = GetFFTFromDoubleArray(buffer, bufferOffset, Constants.FFT_SIZE);
                if (fft[8].Magnitude < 0.1 || fft[12].Magnitude < 0.1 || fft[16].Magnitude < 0.1)
                {
                    state = SyncState.DESYNC;
                    break;
                }

                //Bufferflip
                Array.Copy(buffer, Constants.FFT_SIZE + Constants.GUARD_SIZE, buffer2, 0, bufferPos - Constants.FFT_SIZE + Constants.GUARD_SIZE);
                bufferPos -= Constants.FFT_SIZE + Constants.GUARD_SIZE;
                double[] temp = buffer;
                buffer = buffer2;
                buffer2 = temp;

                //Fix the rotation of the carriers
                double thisError = CalculatePhaseError(fft[8].Phase, fft[16].Phase) / 8.0;
                double offset = CalculatePhaseError(0, fft[8].Phase) - thisError * 8;
                for (int i = 0; i < Constants.FFT_SIZE; i++)
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