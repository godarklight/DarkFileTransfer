using System;
using System.Numerics;

namespace DarkFileTransfer.Common
{
    public static class PcmConvert
    {
        public static double[] ConvertPCMToDouble(byte[] inputData)
        {
            double[] retVal = new double[inputData.Length / 2];
            for (int i = 0; i < inputData.Length / 2; i++)
            {
                short value = (short)(inputData[i * 2 + 1] << 8 | inputData[i * 2]);
                retVal[i] = value / (double)short.MaxValue;
            }
            return retVal;
        }

        public static Complex[] ConvertPCMToComplex(byte[] inputData)
        {
            double[] temp = ConvertPCMToDouble(inputData);
            Complex[] retVal = new Complex[temp.Length];
            for (int i = 0; i < retVal.Length; i++)
            {
                retVal[i] = new Complex(temp[i], 0);
            }
            return retVal;
        }

        public static byte[] ConvertDoubleToPCM(double[] inputData)
        {
            byte[] retVal = new byte[2 * inputData.Length];
            for (int i = 0; i < inputData.Length; i++)
            {
                double input = Math.Clamp(inputData[i], -1.0, 1.0);
                short value = (short)(input * short.MaxValue);
                //Little endian
                retVal[i * 2] = (byte)(value & 255);
                retVal[i * 2 + 1] = (byte)(value >> 8);
            }
            return retVal;
        }

        public static byte[] ConvertComplexToPCM(Complex[] inputData)
        {
            double[] inputDataDouble = new double[inputData.Length];
            for (int i = 0; i < inputData.Length; i++)
            {
                inputDataDouble[i] = inputData[i].Real;
            }
            return ConvertDoubleToPCM(inputDataDouble);
        }

        public static byte[] AddWAVHeader(byte[] inputData)
        {
            //https://onestepcode.com/read-wav-header/
            byte[] retVal = new byte[inputData.Length + 44];
            //ChunkID
            retVal[0] = (byte)'R';
            retVal[1] = (byte)'I';
            retVal[2] = (byte)'F';
            retVal[3] = (byte)'F';
            //ChunkSize (from byte 8 to the rest of the file)
            GetIntBytes(inputData.Length + 36, true).CopyTo(retVal, 4);
            //Format
            retVal[8] = (byte)'W';
            retVal[9] = (byte)'A';
            retVal[10] = (byte)'V';
            retVal[11] = (byte)'E';
            //Subchunk1 ID
            retVal[12] = (byte)'f';
            retVal[13] = (byte)'m';
            retVal[14] = (byte)'t';
            retVal[15] = (byte)' ';
            //Subchunk1 Size
            GetIntBytes(16, true).CopyTo(retVal, 16);
            //Audio Format 1=PCM
            GetShortBytes(1, true).CopyTo(retVal, 20);
            //Channels
            GetShortBytes(1, true).CopyTo(retVal, 22);
            //Sample rate
            GetIntBytes(8000, true).CopyTo(retVal, 24);
            //Byte rate
            GetIntBytes(16000, true).CopyTo(retVal, 28);
            //Block align
            GetShortBytes(2, true).CopyTo(retVal, 32);
            //Bits per sample
            GetShortBytes(16, true).CopyTo(retVal, 34);
            //Format
            retVal[36] = (byte)'d';
            retVal[37] = (byte)'a';
            retVal[38] = (byte)'t';
            retVal[39] = (byte)'a';
            //Data size
            GetIntBytes(inputData.Length, true).CopyTo(retVal, 40);
            //Data
            Buffer.BlockCopy(inputData, 0, retVal, 44, inputData.Length);
            return retVal;
        }

        public static byte[] GetIntBytes(int input, bool littleEndian)
        {
            byte[] u4 = BitConverter.GetBytes(input);
            if (BitConverter.IsLittleEndian != littleEndian)
            {
                Array.Reverse(u4);
            }
            return u4;
        }

        public static byte[] GetShortBytes(short input, bool littleEndian)
        {
            byte[] u2 = BitConverter.GetBytes(input);
            if (BitConverter.IsLittleEndian != littleEndian)
            {
                Array.Reverse(u2);
            }
            return u2;
        }
    }
}