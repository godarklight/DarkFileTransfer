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
            bool testEncoder = true;
            bool testDecoder = true;
            bool testConvolute = true;
            bool testFile = true;

            if (testEncoder)
            {
                //Test encoder
                Stream inputFile = new FileStream("salmon.jpg", FileMode.Open);
                CarrierGenerator cg = new CarrierGenerator(inputFile, Constants.FFT_SIZE);
                SampleGenerator sg = new SampleGenerator(cg);
                Stream outStream = new MemoryStream();
                while (!sg.Completed)
                {
                    byte[] chunk = sg.GetChunk();
                    outStream.Write(chunk, 0, chunk.Length);
                }
                outStream.Seek(0, SeekOrigin.Begin);
                Stream wavBytes = PcmConvert.AddWAVHeader(outStream);
                Stream outputFile = new FileStream("test.wav", FileMode.Create);
                wavBytes.CopyTo(outputFile);
                inputFile.Dispose();
                wavBytes.Dispose();
                outputFile.Dispose();
            }

            if (testDecoder)
            {
                //Test decoder
                Stream wavBytes = new FileStream("test.wav", FileMode.Open);
                Stream saveOut = new FileStream("output.jpg", FileMode.Create);
                wavBytes.Seek(44, SeekOrigin.Begin);
                Decoder decode = new Decoder(saveOut);
                Synchroniser sync = new Synchroniser(decode);
                byte[] audioChunk = new byte[64];
                while (wavBytes.Position != wavBytes.Length)
                {
                    wavBytes.Read(audioChunk, 0, audioChunk.Length);
                    sync.ReceiveData(audioChunk);
                }
                wavBytes.Dispose();
                saveOut.Dispose();
            }

            if (testConvolute)
            {
                Stream inputFile = new FileStream("salmon.jpg", FileMode.Open);
                byte[] convolute = new byte[16];
                int readPos = 0;
                bool convoluteOK = true;
                while (inputFile.Position < inputFile.Length)
                {
                    inputFile.Read(convolute, 0, convolute.Length);
                    byte[] encode = Convoluter.Encode(convolute);
                    byte[] decode = Convoluter.Decode(encode);
                    for (int i = 0; i < decode.Length; i++)
                    {
                        if (convolute[i] != decode[i])
                        {
                            Console.WriteLine($"Error at {readPos}");
                            convoluteOK = false;
                        }
                        readPos++;
                    }
                }
                Console.WriteLine($"ConvoluteOK? {convoluteOK}");
                inputFile.Dispose();
            }

            if (testFile)
            {
                //Test byte compare
                Stream inputFile = new FileStream("salmon.jpg", FileMode.Open);
                Stream outputFile = new FileStream("output.jpg", FileMode.Open);
                while (inputFile.Position != inputFile.Length)
                {
                    if (inputFile.ReadByte() != outputFile.ReadByte())
                    {
                        Console.WriteLine($"Error at {inputFile.Position}");
                        break;
                    }
                }
                inputFile.Dispose();
                outputFile.Dispose();
            }
        }
    }
}