using System;
using System.IO;
using System.Numerics;

namespace DarkFileTransfer.Common
{
    public static class PcmConvert
    {
        public static double[] ConvertPCMToDouble(byte[] inputData, int length)
        {
            double[] retVal = new double[length / 2];
            for (int i = 0; i < length / 2; i++)
            {
                short value = (short)(inputData[i * 2 + 1] << 8 | inputData[i * 2]);
                retVal[i] = value / (double)short.MaxValue;
            }
            return retVal;
        }

        public static double[] ConvertPCMToDouble(byte[] inputData)
        {
            return ConvertPCMToDouble(inputData, inputData.Length);
        }

        public static Complex[] ConvertPCMToComplex(byte[] inputData, int length)
        {
            double[] temp = ConvertPCMToDouble(inputData, length);
            Complex[] retVal = new Complex[temp.Length];
            for (int i = 0; i < retVal.Length; i++)
            {
                retVal[i] = new Complex(temp[i], 0);
            }
            return retVal;
        }

        public static Complex[] ConvertPCMToComplex(byte[] inputData)
        {
            return ConvertPCMToComplex(inputData, inputData.Length);
        }

        public static byte[] ConvertDoubleToPCM(double[] inputData, int length)
        {
            byte[] retVal = new byte[2 * length];
            for (int i = 0; i < length; i++)
            {
                double input = Math.Clamp(inputData[i], -1.0, 1.0);
                short value = (short)(input * short.MaxValue);
                //Little endian
                retVal[i * 2] = (byte)(value & 255);
                retVal[i * 2 + 1] = (byte)(value >> 8);
            }
            return retVal;
        }

        public static byte[] ConvertDoubleToPCM(double[] inputData)
        {
            return ConvertDoubleToPCM(inputData, inputData.Length);
        }

        public static byte[] ConvertComplexToPCM(Complex[] inputData, int length)
        {
            double[] inputDataDouble = new double[length];
            for (int i = 0; i < length; i++)
            {
                inputDataDouble[i] = inputData[i].Real;
            }
            return ConvertDoubleToPCM(inputDataDouble);
        }

        public static byte[] ConvertComplexToPCM(Complex[] inputData)
        {
            return ConvertComplexToPCM(inputData, inputData.Length);
        }

        public static Stream AddWAVHeader(Stream inputData)
        {
            MemoryStream ms = new MemoryStream();
            //https://onestepcode.com/read-wav-header/
            //byte[] retVal = new byte[inputData.Length + 44];
            //ChunkID
            ms.WriteByte((byte)'R');
            ms.WriteByte((byte)'I');
            ms.WriteByte((byte)'F');
            ms.WriteByte((byte)'F');
            //ChunkSize (from byte 8 to the rest of the file)
            ms.Write(GetIntBytes((int)(inputData.Length) + 36, true), 0, 4);
            //Format
            ms.WriteByte((byte)'W');
            ms.WriteByte((byte)'A');
            ms.WriteByte((byte)'V');
            ms.WriteByte((byte)'E');
            //Subchunk1 ID
            ms.WriteByte((byte)'f');
            ms.WriteByte((byte)'m');
            ms.WriteByte((byte)'t');
            ms.WriteByte((byte)' ');
            //Subchunk1 Size
            ms.Write(GetIntBytes(16, true), 0, 4);
            //Audio Format 1=PCM
            ms.Write(GetShortBytes(1, true), 0, 2);
            //Channels
            ms.Write(GetShortBytes(1, true), 0, 2);
            //Sample rate
            ms.Write(GetIntBytes(8000, true), 0, 4);
            //Byte rate
            ms.Write(GetIntBytes(16000, true), 0, 4);
            //Block align
            ms.Write(GetShortBytes(2, true), 0, 2);
            //Bits per sample
            ms.Write(GetShortBytes(16, true), 0, 2);
            //Format
            ms.WriteByte((byte)'d');
            ms.WriteByte((byte)'a');
            ms.WriteByte((byte)'t');
            ms.WriteByte((byte)'a');
            //Data size
            ms.Write(GetIntBytes((int)(inputData.Length), true), 0, 4);
            //Data
            inputData.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
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