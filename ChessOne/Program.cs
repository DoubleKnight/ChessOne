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
using ChessOneLib;

namespace ChessOne
{
    class Program
    {
        static void Main(string[] args)
        {
            ChessGame chessGame = new ChessGame();
            chessGame.MakeMove(4, 1, 4, 3, ChessPieceType.None, false);
            Console.ReadKey();
        }
    }
}
