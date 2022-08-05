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

        public void ProcessFrame(Complex[] frameFFT)
        {
            FFTDebug.WriteComplexArrayToFile(frameFFT, $"frame/{frameNumber}.csv");
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