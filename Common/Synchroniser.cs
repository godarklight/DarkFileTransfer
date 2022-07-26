using System;
using System.IO;

namespace DarkFileTransfer.Common
{
    public class Synchroniser
    {
        public byte[] buffer = new byte[4096];
        public byte[] buffer2 = new byte[4096];
        public byte[] process = new byte[1024];
        int bufferPos = 0;
        Decoder decoder;
        FileStream fstest = new FileStream("testfs.wav", FileMode.Create);

        public bool Completed
        {
            private set;
            get;
        }

        public Synchroniser(Decoder decoder)
        {
            Completed = false;
            this.decoder = decoder;
        }

        public void ReceiveData(byte[] inputData)
        {
            Buffer.BlockCopy(inputData, 0, buffer, bufferPos, inputData.Length);
            bufferPos += inputData.Length;
            if (bufferPos >= process.Length)
            {
                Buffer.BlockCopy(buffer, 0, process, 0, process.Length);
                decoder.ProcessFrame(process);
                fstest.Write(process, 0, process.Length);
                Buffer.BlockCopy(buffer, process.Length, buffer2, 0, buffer.Length - process.Length);
                byte[] temp = buffer;
                buffer = buffer2;
                buffer2 = temp;
                bufferPos -= process.Length;
            }

        }

        public void Complete()
        {
            fstest.Dispose();
        }
    }
}