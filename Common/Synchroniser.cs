using System;
using System.Numerics;
using System.IO;

namespace DarkFileTransfer.Common
{
    public class Synchroniser
    {
        private double freqOffset = 0.0;
        private SyncState state = SyncState.DESYNC;
        public byte[] buffer = new byte[65536];
        public byte[] buffer2 = new byte[65536];
        int bufferPos = 0;
        long totalPos = 0;
        Decoder decoder;

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
            Buffer.BlockCopy(inputData, 0, buffer, bufferPos, inputData.Length);
            bufferPos += inputData.Length;
            totalPos += inputData.Length;
            if (state == SyncState.DESYNC && bufferPos >= 1024)
            {
                Complex[] samples = PcmConvert.ConvertPCMToComplex(buffer, 1024);
                double alcValue = ALC.GetALCMultiplier(samples);
                if (alcValue > 30)
                {
                    bufferPos = 0;
                    return;
                }
                ALC.ApplyALC(samples, alcValue);
                Complex[] fftSync = FFT.CalcFFT(samples);
                if (fftSync[12].Magnitude > 1)
                {
                    state = SyncState.FREQ_SYNCED;
                }
                else
                {
                    bufferPos = 0;
                }
            }
            if (state == SyncState.FREQ_SYNCED && bufferPos >= 8192)
            {
                double highest = 0;
                int offset = 0;
                Complex[] samples = PcmConvert.ConvertPCMToComplex(buffer, 8192);
                //ALC.ApplyALC(samples, ALC.GetALCMultiplier(samples));
                Complex[] fftIn = new Complex[512];
                for (int i = 0; i < 2048; i += 16)
                {
                    Array.Copy(samples, i, fftIn, 0, 512);
                    Complex[] fft = FFT.CalcFFT(fftIn);
                    if (highest < fft[16].Magnitude)
                    {
                        highest = fft[16].Magnitude;
                        offset = i;
                    }
                }

                double smallestError = double.PositiveInfinity;
                int smallestJ = 0;
                Complex[] fftSave = null;
                for (int j = -64; j < 64; j++)
                {
                    if (offset + j < 0)
                    {
                        continue;
                    }
                    Array.Copy(samples, offset + j, fftIn, 0, 512);
                    Complex[] fixFFT = FFT.CalcFFT(fftIn);
                    FFTDebug.WriteComplexArrayToFile(fixFFT, $"const/{offset + j}.csv");
                    double error = (fixFFT[12] - fixFFT[16]).Magnitude;
                    if (error < smallestError)
                    {
                        smallestJ = j;
                        smallestError = error;
                        fftSave = fixFFT;
                    }
                }
                int newBufferStart = offset + smallestJ;
                Console.WriteLine($"Found start at {totalPos - bufferPos + newBufferStart}");
                state = SyncState.TIME_SYNCED;
                Array.Copy(buffer, newBufferStart * 2, buffer2, 0, buffer2.Length - (newBufferStart * 2));
                byte[] temp2 = buffer;
                buffer = buffer2;
                buffer2 = temp2;
                bufferPos -= newBufferStart;
            }
            if (state == SyncState.TIME_SYNCED)
            {
                if (bufferPos >= 1024)
                {
                    Complex[] fftIn = PcmConvert.ConvertPCMToComplex(buffer, 1024);
                    Complex[] fft = FFT.CalcFFT(fftIn);
                    decoder.ProcessFrame(fft);
                    Buffer.BlockCopy(buffer, 1024, buffer2, 0, buffer.Length - 1024);
                    byte[] temp = buffer;
                    buffer = buffer2;
                    buffer2 = temp;
                    bufferPos -= 1024;
                }
            }
        }
    }
}