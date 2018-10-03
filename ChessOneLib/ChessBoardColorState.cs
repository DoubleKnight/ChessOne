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
