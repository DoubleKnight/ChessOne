using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessLib
{
    public class ConsideringMoveEventArgs : System.EventArgs
    {
        // Field
        private ChessMove move = null;

        // Constructor
        public ConsideringMoveEventArgs(ChessMove move)
        {
            this.move = move;
        }

        // Properties

        public ChessMove Move
        {
            get { return move; }
        }
    }
}
