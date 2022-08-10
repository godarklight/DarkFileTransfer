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
        int readPos = 0;
        byte[] fileBytes;
        int chunkSize = 512;
        public bool Completed
        {
            private set;
            get;
        }

        public CarrierGenerator(byte[] fileBytes, int chunkSize)
        {
            Completed = false;
            this.chunkSize = chunkSize;
            this.fileBytes = fileBytes;
        }

        public Complex[] GetCarriers()
        {
            Complex[] retVal = new Complex[chunkSize];

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
            for (int i = 0; i < 16; i++)
            {
                if (readPos == fileBytes.Length)
                {
                    Completed = true;
                    break;
                }

                int num = fileBytes[readPos];

                for (int j = 0; j < 4; j++)
                {
                    double val1 = (num & 1) == 0 ? -1.414 : 1.414;
                    num = num >> 1;
                    double val2 = (num & 1) == 0 ? -1.414 : 1.414;
                    num = num >> 1;
                    retVal[32 + 8 * i + j * 2] = new Complex(val1, val2);
                }

                readPos++;
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