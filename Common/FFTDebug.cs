
using System;
using System.IO;
using System.Numerics;

namespace DarkFileTransfer.Common
{
    public static class FFTDebug
    {
        public static void WriteComplexArrayToFile(Complex[] inputData, string outputFile)
        {
            File.Delete(outputFile);
            using (StreamWriter sw = new StreamWriter(outputFile))
            {
                for (int i = 0; i < inputData.Length; i++)
                {
                    sw.WriteLine($"{i},{inputData[i].Real},{inputData[i].Imaginary}");
                }
                sw.Flush();
            }
        }

        public static void WriteHalfComplexArrayToFile(Complex[] inputData, string outputFile)
        {
            File.Delete(outputFile);
            using (StreamWriter sw = new StreamWriter(outputFile))
            {
                for (int i = 0; i < inputData.Length / 2; i++)
                {
                    sw.WriteLine($"{i},{inputData[i].Real},{inputData[i].Imaginary}");
                }
                sw.Flush();
            }
        }
    }
}