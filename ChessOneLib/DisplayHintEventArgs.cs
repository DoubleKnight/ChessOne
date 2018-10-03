using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessOneLib
{
    public class DisplayHintEventArgs : System.EventArgs
    {
        // Field
        private ChessMove move = null;

        // Constructor
        public DisplayHintEventArgs(ChessMove move)
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
