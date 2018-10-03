using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessLib
{
    public class PieceMovedEventArgs : System.EventArgs
    {
        // Field
        private ChessMove move = null;
        private ChessMoveInfo moveInfo = null;

        // Constructor
        public PieceMovedEventArgs(ChessMove move, ChessMoveInfo moveInfo)
        {
            this.move = move;
            this.moveInfo = moveInfo;
        }

        // Properties

        public ChessMove Move
        {
            get { return move; }
        }

        public ChessMoveInfo MoveInfo
        {
            get { return moveInfo; }
        }
    }
}
