#region License
// ChessOne
// Copyright (C) 2010-2018 Double Knight AB
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessOneLib
{
    class Util
    {
        ///////////////////////////////////////////debrujin//////////////////////////////////////
        /// <summary>         
        /// array for the bitscan forward routine         
        /// </summary>         
        private static uint[] debruijn64Array = new uint[64]{
            63,  0, 58,  1, 59, 47, 53,  2,
            60, 39, 48, 27, 54, 33, 42,  3,
            61, 51, 37, 40, 49, 18, 28, 20,
            55, 30, 34, 11, 43, 14, 22,  4,
            62, 57, 46, 52, 38, 26, 32, 41,
            50, 36, 17, 19, 29, 10, 13, 21,
            56, 45, 25, 31, 35, 16,  9, 12,
            44, 24, 15,  8, 23,  7,  6,  5         
        };         
        
        /// <summary>         
        /// const used for the debrujing algorithm         
        /// </summary>         
        const long debruijn64 = (long)(0x07EDD5E59A4E28C2);

        /// <summary>
        /// 
        /// get the index of the LSB of bitboard
        /// 
        /// </summary>
        /// 
        /// <param name="bitboard">bitboard</param>
        /// 
        /// <returns>index</returns>
        static public uint bitScanForward(ulong bitboard)
        {            
            return debruijn64Array[(ulong)(getBitboardLSB(bitboard) * debruijn64) >> 58];
        }          
        
        /// <summary>         
        /// get the index of the only one 1 inf bitbard
        /// /// </summary>         
        /// <param name="bitboard">bitboard</param>
        /// /// <returns>index</returns>
        static public uint fastBitScanForward(ulong bitboard)
        {             
            return debruijn64Array[(ulong)(bitboard * debruijn64) >> 58];
        }

        /// <summary>
        /// 
        /// get a bitboard with only the LSB active
        /// 
        /// </summary>
        /// 
        /// <param name="bitboard">bitboard</param>
        /// /// <returns>bitboard</returns>
        static public ulong getBitboardLSB(ulong bitboard)
        {             
            return ((ulong)((long)bitboard & -(long)bitboard));
        }

        /// <summary>
        /// count the "ones" in a bitboard
        /// </summary>
        /// <param name="bitboard">bitboard</param>
        /// <returns>numbro of bit=1</returns>
        /// 
        static public int getBitCountFromBitboard(ulong bitboard)
        {
            int i = 0;
            if (bitboard != 0)
            {
                do
                {
                    i++;
                } while ((bitboard &= bitboard - 1) != 0); // reset LS1B 
            }
            return i;
        }

        /// <summary>
        /// Get a description of the bitboard for debugging purposes.
        /// </summary>
        /// <param name="bitboard"></param>
        /// <returns></returns>
        static public string BitBoardToString(ulong bitboard)
        {
            string res = string.Empty;
            for (int row = 7; row >= 0; row--)
            {
                for (int col = 0; col <= 7; col++)
                {
                    int index = 8 * row + col;
                    if ((((ulong)1 << index) & bitboard) != 0)
                        res += "1";
                    else
                        res += "0";
                }
                res += "\r\n";
            }
            return res;
        }
    }
}
