using System;
using System.Numerics;

namespace DarkFileTransfer.Common
{
    public static class ALC
    {
        public static double GetALCMultiplier(double[] input)
        {
            double alcMultiplier = 100.0;
            for (int i = 0; i < input.Length; i++)
            {
                double newAlc = Math.Abs(0.99 / input[i]);
                if (newAlc < alcMultiplier)
                {
                    alcMultiplier = newAlc;
                }
            }
            return alcMultiplier;
        }

        public static double GetALCMultiplier(Complex[] input)
        {
            double alcMultiplier = 100.0;
            for (int i = 0; i < input.Length; i++)
            {
                double newAlc = Math.Abs(0.99 / input[i].Magnitude);
                if (newAlc < alcMultiplier)
                {
                    alcMultiplier = newAlc;
                }
            }
            return alcMultiplier;
        }

        public static void ApplyALC(double[] input, double alc)
        {
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = input[i] * alc;
            }
        }

        public static void ApplyALC(Complex[] input, double alc)
        {
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = input[i] * alc;
            }
        }
    }
}