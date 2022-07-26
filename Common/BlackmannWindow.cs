using System;

namespace DarkFileTransfer.Common
{
    public class WindowGenerator
    {
        public double[] GenerateBlackmannWindow(int length)
        {
            double[] retVal = new double[length];
            double nminus1 = length - 1;
            for (int i = 0; i < length; i++)
            {
                retVal[i] = 0.42 - (0.5 * Math.Cos((2.0 * Math.PI * i) / nminus1)) + (0.08 * Math.Cos((4 * Math.PI * i) / nminus1));
            }
            return retVal;
        }
    }
}