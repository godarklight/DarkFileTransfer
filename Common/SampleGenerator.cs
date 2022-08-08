using System;
using System.IO;
using System.Numerics;

namespace DarkFileTransfer.Common
{
    public class SampleGenerator
    {
        CarrierGenerator cg;
        public bool Completed
        {
            get
            {
                return cg.Completed;
            }
        }

        public SampleGenerator(CarrierGenerator cg)
        {
            this.cg = cg;
        }

        public byte[] GetChunk()
        {
            Complex[] carriers = cg.GetCarriers();
            Complex[] ifft = FFT.CalcIFFT(carriers);
            int length8 = ifft.Length / 8;
            Complex[] ifftCyclic = new Complex[ifft.Length + length8];
            Array.Copy(ifft, 0, ifftCyclic, length8, ifft.Length);
            Array.Copy(ifft, ifft.Length - length8, ifftCyclic, 0, length8);
            return PcmConvert.ConvertComplexToPCM(ifftCyclic);
        }
    }
}