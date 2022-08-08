using System;
using System.IO;
using System.Numerics;

using DarkFileTransfer.Common;

namespace DarkFileTransfer.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] fileBytes = File.ReadAllBytes("salmon.jpg");
            CarrierGenerator cg = new CarrierGenerator(fileBytes, 512);
            SampleGenerator sg = new SampleGenerator(cg);
            File.Delete("test.raw");
            using (FileStream fs = new FileStream("test.raw", FileMode.CreateNew))
            {
                while (!sg.Completed)
                {
                    byte[] chunk = sg.GetChunk();
                    fs.Write(chunk, 0, chunk.Length);
                }
            }
            byte[] rawBytes = File.ReadAllBytes("test.raw");
            byte[] wavBytes = PcmConvert.AddWAVHeader(rawBytes);
            File.Delete("test.wav");
            File.WriteAllBytes("test.wav", wavBytes);

            //Test output
            //File.Delete("output.jpg");

            MorletWavelet mw = new MorletWavelet(12, 512);

            //Test decoder
            byte[] rawWavBytes = File.ReadAllBytes("inphone.raw");

            using (FileStream fs = new FileStream("output.jpg", FileMode.Create))
            {
                byte[] chunk = new byte[64];
                int readLeft = rawWavBytes.Length;
                Decoder decode = new Decoder(fs);
                Synchroniser sync = new Synchroniser(decode);
                while (readLeft > 0)
                {
                    int thisCopy = readLeft > chunk.Length ? chunk.Length : readLeft;
                    Array.Copy(rawWavBytes, rawWavBytes.Length - readLeft, chunk, 0, thisCopy);
                    sync.ReceiveData(chunk);
                    readLeft -= thisCopy;
                }
            }
        }
    }
}
