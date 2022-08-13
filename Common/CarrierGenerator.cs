using System;
using System.IO;
using System.Numerics;

namespace DarkFileTransfer.Common
{
    public class CarrierGenerator
    {
        byte[] morseID = new byte[] { 2, 0, 1, 0, 1, 1, 1, 0, 2 };
        int morseIDPos = 0;
        int morseFramePos = 0;
        int morseValue = 0;
        int timingID = 0;
        int timingPos = 0;
        int timingValue = 0;
        Stream inputData;
        public bool Completed
        {
            get
            {
                return inputData.Position == inputData.Length;
            }
        }

        public CarrierGenerator(Stream inputData, int chunkSize)
        {
            this.inputData = inputData;
        }

        public Complex[] GetCarriers()
        {
            Complex[] retVal = new Complex[Constants.FFT_SIZE];

            //Pilot Tones
            retVal[8] = new Complex(2, 0);
            retVal[12] = new Complex(2, 0);
            retVal[16] = new Complex(2, 0);

            //Timing Channel
            if (timingPos == 0)
            {
                timingValue = timingID;
                timingID++;
                timingPos = 32;
                retVal[20] = new Complex(4, 0);
            }
            retVal[24] = new Complex((timingValue & 1) * 2.0, 0);
            timingValue = timingValue >> 1;
            timingPos--;

            //Morse ID channel
            morseFramePos--;
            if (morseFramePos == -1)
            {
                if (morseIDPos == morseID.Length)
                {
                    morseIDPos = 0;
                    morseFramePos = 16;
                    morseValue = 0;
                }
                else
                {
                    morseValue = morseID[morseIDPos];
                    morseFramePos = 1;
                    if (morseValue == 0)
                    {
                        morseFramePos = 2;
                    }
                    if (morseValue == 2)
                    {
                        morseValue = 1;
                        morseFramePos = 3;
                    }
                    morseIDPos++;
                }
            }
            if (morseFramePos == 0)
            {
                morseValue = 0;
            }
            retVal[28] = new Complex(morseValue * 4.0, 0.0);

            //Data channels
            //30% of an 8Khz channel is 2.7Khz
            int totalBytes = Constants.CARRIERS / 4;
            byte[] unencoded = new byte[totalBytes / 2];
            int inputBytesRead = inputData.Read(unencoded, 0, unencoded.Length);
            byte[] encoded = Convoluter.Encode(unencoded);
            int encodedBitsLeft = 0;
            int encodedByte = 0;
            int encodedReadPos = 0;
            for (int i = 0; i < Constants.CARRIERS; i++)
            {
                if (encodedBitsLeft == 0)
                {
                    encodedByte = encoded[encodedReadPos];
                    encodedBitsLeft = 8;
                    encodedReadPos++;
                }

                double val1 = (encodedByte & 1) == 0 ? -1.414 : 1.414;
                encodedByte = encodedByte >> 1;
                double val2 = (encodedByte & 1) == 0 ? -1.414 : 1.414;
                encodedByte = encodedByte >> 1;
                encodedBitsLeft -= 2;
                retVal[32 + i * Constants.CARRIER_SPACING] = new Complex(val1, val2);
            }

            //Convert to a real valued signal
            for (int i = 1; i < (retVal.Length / 2); i++)
            {
                retVal[retVal.Length - i] = Complex.Conjugate(retVal[i]);
            }

            return retVal;
        }
    }
}