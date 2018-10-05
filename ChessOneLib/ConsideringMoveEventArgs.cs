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
