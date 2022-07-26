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
            File.Delete("ProcessFrame1.csv");
            File.Delete("ProcessFrame2.csv");
            File.Delete("ProcessFrame3.csv");
            File.Delete("ProcessFrame4.csv");
            File.Delete("output.jpg");
            using (FileStream fs = new FileStream("output.jpg", FileMode.Create))
            {
                byte[] chunk = new byte[64];
                int readLeft = rawBytes.Length;
                Decoder decode = new Decoder(fs);
                Synchroniser sync = new Synchroniser(decode);
                //Desync the data for testing
                //sync.ReceiveData(new byte[8]);
                while (readLeft > 0)
                {
                    int thisCopy = readLeft;
                    if (readLeft > chunk.Length)
                    {
                        thisCopy = chunk.Length;
                    }
                    Array.Copy(rawBytes, rawBytes.Length - readLeft, chunk, 0, thisCopy);
                    sync.ReceiveData(chunk);
                    readLeft -= thisCopy;
                }
                sync.Complete();
            }
        }
    }
}
