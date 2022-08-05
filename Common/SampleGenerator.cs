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
            return PcmConvert.ConvertComplexToPCM(ifft);
        }
    }
}