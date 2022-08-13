using System;
using System.IO;
using System.Numerics;

namespace DarkFileTransfer.Common
{
    //https://en.wikipedia.org/wiki/Convolutional_code#/media/File:Conv_code_177_133.png
    public static class Convoluter
    {
        private static int[] nibble;

        public static byte[] Encode(byte[] input)
        {
            if (nibble == null)
            {
                GenerateTables();
            }
            byte[] output = new byte[input.Length * 2];
            int state = 0;
            int writePos = 0;
            int inputByte = 0;
            for (int readPos = 0; readPos < input.Length; readPos++)
            {
                inputByte = input[readPos];
                int outputByte = 0;
                for (int i = 0; i < 8; i++)
                {
                    state = state >> 1;
                    state += (inputByte & 1) << 6;
                    inputByte = inputByte >> 1;
                    outputByte = outputByte >> 2;
                    outputByte |= nibble[state] << 6;

                    if (i % 4 == 3)
                    {
                        output[writePos] = (byte)outputByte;
                        writePos++;
                        outputByte = 0;
                    }
                }
            }
            return output;
        }

        public static byte[] Decode(byte[] input)
        {
            if (nibble == null)
            {
                GenerateTables();
            }
            int inputState = 0;
            int inputPos = 0;
            int inputBitsLeft = 0;
            byte[] output = new byte[input.Length / 2];
            int steps = 8 * output.Length;
            int[,] trellis = new int[steps + 1, 64];
            int[,] trellisCost = new int[steps + 1, 64];
            //Unlink all paths
            for (int i = 0; i <= steps; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    trellis[i, j] = -1;
                    trellisCost[i, j] = int.MaxValue;
                }
            }

            //We know the first state is 0
            trellis[0, 0] = 0;
            trellisCost[0, 0] = 0;

            for (int step = 0; step < steps; step++)
            {
                //Step input
                if (inputBitsLeft == 0)
                {
                    if (inputPos == input.Length)
                    {
                        break;
                    }
                    inputState = input[inputPos];
                    inputPos++;
                    inputBitsLeft = 6;
                }
                else
                {
                    inputState = inputState >> 2;
                    inputBitsLeft -= 2;
                }

                //Buld trellis
                int processState = inputState & 0b11;
                for (int possibleState = 0; possibleState < 64; possibleState++)
                {
                    if (trellis[step, possibleState] != -1)
                    {
                        int next0State = possibleState >> 1;
                        int next0Value = nibble[possibleState];
                        int next0Error = GetDistance(processState, next0Value);
                        int next0ErrorCumulative = trellisCost[step, possibleState] + next0Error;
                        int next1State = (possibleState | 64) >> 1;
                        int next1Value = nibble[possibleState | 64];
                        int next1Error = GetDistance(processState, next1Value);
                        int next1ErrorCumulative = trellisCost[step, possibleState] + next1Error;

                        if (trellis[step + 1, next0State] == -1 || trellisCost[step + 1, next0State] > next0ErrorCumulative)
                        {
                            trellis[step + 1, next0State] = possibleState;
                            trellisCost[step + 1, next0State] = next0ErrorCumulative;
                        }
                        if (trellis[step + 1, next1State] == -1 || trellisCost[step + 1, next1State] > next1ErrorCumulative)
                        {
                            trellis[step + 1, next1State] = possibleState;
                            trellisCost[step + 1, next1State] = next1ErrorCumulative;
                        }
                    }
                }
            }

            //Find best path
            int lowestID = 0;
            int lowest = int.MaxValue;
            for (int possibleState = 0; possibleState < 64; possibleState++)
            {
                int possibleCost = trellisCost[steps, possibleState];
                if (possibleCost < lowest)
                {
                    lowestID = possibleState;
                    lowest = possibleCost;
                }
            }

            //Trackback
            int pathPos = steps - 1;
            int[] path = new int[steps];
            int lastID = lowestID;
            while (pathPos >= 0)
            {
                path[pathPos] = lastID;
                lastID = trellis[pathPos + 1, lastID];
                pathPos--;
            }

            //Convert the states to output bytes
            for (int i = 0; i < path.Length / 8; i++)
            {
                int outputByte = (path[i * 8 + 0] & 0b00100000) >> 5;
                outputByte |= (path[i * 8 + 1] & 0b00100000) >> 4;
                outputByte |= (path[i * 8 + 2] & 0b00100000) >> 3;
                outputByte |= (path[i * 8 + 3] & 0b00100000) >> 2;
                outputByte |= (path[i * 8 + 4] & 0b00100000) >> 1;
                outputByte |= (path[i * 8 + 5] & 0b00100000);
                outputByte |= (path[i * 8 + 6] & 0b00100000) << 1;
                outputByte |= (path[i * 8 + 7] & 0b00100000) << 2;
                output[i] = (byte)outputByte;
            }
            return output;
        }

        private static void GenerateTables()
        {
            nibble = new int[128];
            for (int i = 0; i < 128; i++)
            {
                nibble[i] = GetNibble(i);
            }
        }

        private static int GetNibble(int state)
        {
            int state1 = ((state >> 6) & 1) ^ ((state >> 5) & 1) ^ ((state >> 4) & 1) ^ ((state >> 3) & 1) ^ ((state) & 1);
            int state2 = ((state >> 6) & 1) ^ ((state >> 4) & 1) ^ ((state >> 3) & 1) ^ ((state >> 1) & 1) ^ ((state) & 1);
            return state2 << 1 | state1;
        }

        private  static int GetDistance(int a, int b)
        {
            int bit0 = (a & 1) ^ (b & 1);
            int bit1 = ((a >> 1) & 1) ^ ((b >> 1) & 1);
            return bit0 + bit1;
        }
    }
}