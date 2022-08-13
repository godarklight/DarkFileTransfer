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
            int encodedByte = 0;
            int encodedBits = 0;
            byte[] encodedData = new byte[Constants.CARRIERS / 4];
            int encodedWritePos = 0;
            for (int i = 0; i < Constants.CARRIERS; i++)
            {
                encodedByte = encodedByte >> 2;
                Complex c = frameFFT[32 + i * Constants.CARRIER_SPACING];
                encodedByte |= (ReadConstellation(c) << 6);
                encodedBits += 2;
                if (encodedBits == 8)
                {
                    encodedData[encodedWritePos] = (byte)encodedByte;
                    encodedWritePos++;
                    encodedBits = 0;
                    encodedByte = 0;
                }
            }
            byte[] decoded = Convoluter.Decode(encodedData);
            output.Write(decoded, 0, decoded.Length);
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