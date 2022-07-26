using System.IO;
using System.Numerics;

namespace DarkFileTransfer.Common
{
    public class Decoder
    {
        int frameNumber = 0;
        Stream output;

        public bool Completed
        {
            private set;
            get;
        }

        public Decoder(Stream output)
        {
            this.output = output;
        }

        public void ProcessFrame(byte[] frameBytes)
        {
            Complex[] frameComplex = PcmConvert.ConvertPCMToComplex(frameBytes);
            Complex[] frameFFT = FFT.CalcFFT(frameComplex);

            if (frameNumber == 0)
            {
                File.Delete("ProcessFrame0.csv");
                File.Delete("ProcessFrame1.csv");
                File.Delete("ProcessFrame2.csv");
                File.Delete("ProcessFrame3.csv");
                FFTDebug.WriteComplexArrayToFile(frameFFT, "ProcessFrame0.csv");
            }
            if (frameNumber == 1)
            {
                FFTDebug.WriteComplexArrayToFile(frameFFT, "ProcessFrame1.csv");
            }
            if (frameNumber == 2)
            {
                FFTDebug.WriteComplexArrayToFile(frameFFT, "ProcessFrame2.csv");
            }
            if (frameNumber == 3)
            {
                FFTDebug.WriteComplexArrayToFile(frameFFT, "ProcessFrame3.csv");
            }

            for (int i = 32; i < (frameFFT.Length * 0.3); i += 8)
            {
                int value = 0;
                for (int j = 0; j < 4; j++)
                {
                    Complex c = frameFFT[i + j * 2];
                    value |= ReadConstellation(c) << j * 2;
                }
                output.WriteByte((byte)value);
            }

            frameNumber++;
        }

        public int ReadConstellation(Complex input)
        {
            int retVal = 0;
            if (input.Real > 0)
            {
                retVal += 1;
            }
            if (input.Imaginary > 0)
            {
                retVal += 2;
            }
            return retVal;
        }
    }
}