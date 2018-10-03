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
