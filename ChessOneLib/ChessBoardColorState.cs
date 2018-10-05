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
    public class ChessBoardColorState
    {
        public UInt64 Pieces { get; set; }

        public UInt64 Pawns { get; set; }
        public UInt64 Knights { get; set; }
        public UInt64 Bishops { get; set; }
        public UInt64 Rooks { get; set; }
        public UInt64 Queens { get; set; }
        public UInt64 King { get; set; }

        public bool KingsideCastlingPossible { get; set; }
        public bool QueensideCastlingPossible { get; set; }

        public ChessBoardColorState()
        {
            KingsideCastlingPossible = true;
            QueensideCastlingPossible = true;
        }

        public ChessBoardColorState(ChessBoardColorState stateToCopy)
        {
            this.Pieces = stateToCopy.Pieces;

            this.Pawns = stateToCopy.Pawns;
            this.Knights = stateToCopy.Knights;
            this.Bishops = stateToCopy.Bishops;
            this.Rooks = stateToCopy.Rooks;
            this.Queens = stateToCopy.Queens;
            this.King = stateToCopy.King;

            this.KingsideCastlingPossible = stateToCopy.KingsideCastlingPossible;
            this.QueensideCastlingPossible = stateToCopy.QueensideCastlingPossible;
        }
    }
}
