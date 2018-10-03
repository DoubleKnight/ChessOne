using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

/*
 * This file is common between XNA and Windows Phone projects!
 */

namespace ChessOneLib
{
    public enum ChessPieceType
    {
        None = 0x00,
        Pawn = 0x01,
        Knight = 0x02,
        Bishop = 0x03,
        Rook = 0x04,
        Queen = 0x05,
        King = 0x06,
        Mask = 0x0F
    }

    public enum ChessPieceColor
    {
        None = -1,
        White = 0,
        Black = 1
    }

    public class ChessMoveInfo
    {
        public ChessPieceColor MovedBy { get; set; }

        public ChessPositionIndex CapturedPiecePos { get; set; }
        public ChessPositionIndex SecondaryFromIndex { get; set; }
        public ChessPositionIndex SecondaryToIndex { get; set; }

        public ChessMoveInfo()
        {
            this.MovedBy = ChessPieceColor.None;
            this.CapturedPiecePos = ChessPositionIndex.None;
            this.SecondaryFromIndex = ChessPositionIndex.None;
            this.SecondaryToIndex = ChessPositionIndex.None;
        }
    }

    public class ChessMove
    {
        [XmlElement("f")] 
        public ChessPositionIndex FromIndex { get; set; }
        [XmlElement("t")]
        public ChessPositionIndex ToIndex { get; set; }

        [XmlElement("pt")]
        public ChessPieceType PieceType { get; set; }

        [XmlElement("ppt")]
        public ChessPieceType PromotionPieceType { get; set; }

        [XmlElement("c")]
        public bool IsCapture { get; set; }
        [XmlElement("ks")]
        public bool IsKingsideCastling { get; set; }
        [XmlElement("qs")]
        public bool IsQueensideCastling { get; set; }

        [XmlElement("b")]
        public ChessBoard ResultingBoard { get; set; }   // The chess board after the move is made.

        public ChessMove()
        {
        }

        public ChessMove(ChessPositionIndex fromIndex, ChessPositionIndex toIndex, ChessPieceType pieceType)
        {
            this.FromIndex = fromIndex;
            this.ToIndex = toIndex;
            this.PieceType = pieceType;
        }

        public ChessMove(ChessMove move)
        {
            this.FromIndex = move.FromIndex;
            this.ToIndex = move.ToIndex;
            this.PieceType = move.PieceType;
            this.IsCapture = move.IsCapture;
            this.PromotionPieceType = move.PromotionPieceType;
            this.IsKingsideCastling = move.IsKingsideCastling;
            this.IsQueensideCastling = move.IsQueensideCastling;
        }

        public bool Equals(ChessMove move)
        {
            return Equals(move.FromIndex, move.ToIndex);
        }

        public bool Equals(ChessPositionIndex fromIndex, ChessPositionIndex toIndex)
        {
            return (this.FromIndex == fromIndex && this.ToIndex == toIndex);
        }

        public override string ToString()
        {
            string res = string.Format("{0}{1}-{2}{3}",
                ColToString(ChessBoard.GetColumnFromIndex(FromIndex)),
                ChessBoard.GetRowFromIndex(FromIndex) + 1,
                ColToString(ChessBoard.GetColumnFromIndex(ToIndex)),
                ChessBoard.GetRowFromIndex(ToIndex) + 1);
            return res;
        }

        private string ColToString(int col)
        {
            string res = string.Empty;
            switch (col)
            {
                case 0:
                    res = "a";
                    break;
                case 1:
                    res = "b";
                    break;
                case 2:
                    res = "c";
                    break;
                case 3:
                    res = "d";
                    break;
                case 4:
                    res = "e";
                    break;
                case 5:
                    res = "f";
                    break;
                case 6:
                    res = "g";
                    break;
                case 7:
                    res = "h";
                    break;
            }
            return res;
        }
    }

    public enum ChessGameState
    {
        Active = 1,
        CheckMate = 2,
        StaleMate = 3,
        DrawByRepetition = 4,
        DrawByMaterial = 5,
        DrawBy50MoveRule = 6,
    }

    public interface ChessTraceCallback
    {
        void AddLog(string str);
    }

    public class ChessGame
    {
        private Random random = new Random((int)DateTime.Now.Ticks);
//        private Random random = new Random(2);  // Hardcoded for debugging.

        private ChessTraceCallback callback = null;
        public object callbackLock = new object();

        private List<ChessMove> chessMoves = new List<ChessMove>();

        public List<ChessMove> ChessMoves { get { return chessMoves; } set { chessMoves = value; } }

        public delegate void PieceMovedHandler(object sender, PieceMovedEventArgs e);

        public event PieceMovedHandler PieceMoved;

        protected void RaisePieceMovedEvent(ChessMove move, ChessMoveInfo moveInfo)
        {
            if (PieceMoved != null)
                PieceMoved(null, new PieceMovedEventArgs(move, moveInfo));
        }

        public ChessMove LastMove
        {
            get
            {
                ChessMove lastMove = null;
                if( chessMoves.Count > 0 )
                    lastMove = chessMoves[chessMoves.Count - 1];
                return lastMove;
            }
        }

        public ChessBoard Board { get; set; }

        public ChessPieceColor Turn { get; set; }

        public ChessGameState GameState { get; set; }

        public ChessTraceCallback Callback
        {
            get
            {
                lock (callbackLock)
                {
                    return callback;
                }
            }
            set
            {
                lock (callbackLock)
                {
                    callback = value;
                }
            }
        }

        public void AddLog(string text)
        {
            if (Callback != null)
                Callback.AddLog(text);
        }

        /// <summary>
        /// Creates the game.
        /// </summary>
        public ChessGame()
        {
            this.Board = new ChessBoard(this);
            this.Board.InitBoard();

            this.Turn = ChessPieceColor.White;

            GameState = ChessGameState.Active;
        }

        public void HookUp()
        {
            this.Board.Game = this;
        }
        
        internal static List<ChessMove> GetPossibleMoves(ChessBoard board, ChessPieceColor color)
        {
            List<ChessMove> moveList = new List<ChessMove>();
            AddPawnMoves(board, color, moveList, 0);
            AddKnightMoves(board, color, moveList, 0);
            AddBishopMoves(board, color, moveList, 0);
            AddRookMoves(board, color, moveList, 0);
            AddQueenMoves(board, color, moveList, 0);
            AddKingMoves(board, color, moveList, 0);

            // Check so the moves do not put own king in check.
            List<ChessMove> moves = new List<ChessMove>();
            foreach (ChessMove move in moveList)
            {
                ChessBoard resBoard = new ChessBoard(board);
                resBoard.ApplyMove(color, move, null);
                if (!IsInCheck(resBoard, color))
                    moves.Add(move);
            }

            return moves;
        }

        public static List<ChessMove> GetPossibleMovesFrom(ChessBoard board, ChessPositionIndex index)
        {
            List<ChessMove> moveList = new List<ChessMove>();
            List<ChessMove> moves = new List<ChessMove>();
            ChessPiece piece = new ChessPiece();
            if (board.GetPiece(index, ref piece))
            {
                switch (piece.PieceType)
                {
                    case ChessPieceType.Pawn:
                        AddOnePawnMoves(board, piece.PieceColor, (uint)index, moveList);
                        break;
                    case ChessPieceType.Knight:
                        AddOneKnightMoves(board, piece.PieceColor, (uint)index, moveList);
                        break;
                    case ChessPieceType.Bishop:
                        AddOneBishopMoves(board, piece.PieceColor, (uint)index, moveList);
                        break;
                    case ChessPieceType.Rook:
                        AddOneRookMoves(board, piece.PieceColor, (uint)index, moveList);
                        break;
                    case ChessPieceType.Queen:
                        AddOneQueenMoves(board, piece.PieceColor, (uint)index, moveList);
                        break;
                    case ChessPieceType.King:
                        AddKingMoves(board, piece.PieceColor, moveList, 0);
                        break;
                }

                // Check so the moves do not put own king in check.
                foreach (ChessMove move in moveList)
                {
                    ChessBoard resBoard = new ChessBoard(board);
                    resBoard.ApplyMove(piece.PieceColor, move, null);
                    if (!IsInCheck(resBoard, piece.PieceColor))
                        moves.Add(move);
                }
            }
            return moves;
        }

        private static void AddOnePawnMoves(ChessBoard board, ChessPieceColor color, uint squareFrom, List<ChessMove> moveList)
        {
            ulong occBoard = board.whiteBoardState.Pieces | board.blackBoardState.Pieces;

            ulong targetBitboard = 0;

            if (color == ChessPieceColor.White)
            {
                targetBitboard = Precomputed.WhitePawnMoves[squareFrom] & ~occBoard;
                if (targetBitboard != 0)
                    targetBitboard |= Precomputed.WhitePawnDoubleMoves[squareFrom] & ~occBoard;
                targetBitboard |= Precomputed.WhitePawnCaptureMoves[squareFrom] & board.blackBoardState.Pieces;

                if ((Precomputed.WhitePawnEnPassantCaptureTargets[squareFrom] & board.enPassantTarget) != 0)
                    targetBitboard |= board.enPassantTarget << 8;
            }
            else
            {
                targetBitboard = Precomputed.BlackPawnMoves[squareFrom] & ~occBoard;
                if (targetBitboard != 0)
                    targetBitboard |= Precomputed.BlackPawnDoubleMoves[squareFrom] & ~occBoard;
                targetBitboard |= Precomputed.BlackPawnCaptureMoves[squareFrom] & board.whiteBoardState.Pieces;

                if ((Precomputed.BlackPawnEnPassantCaptureTargets[squareFrom] & board.enPassantTarget) != 0)
                    targetBitboard |= board.enPassantTarget >> 8;
            }

            if (moveList != null)
                GenerateMoves(squareFrom, targetBitboard, ChessPieceType.Pawn, moveList);
        }

        private static bool AddPawnMoves(ChessBoard board, ChessPieceColor color, List<ChessMove> moveList, ulong checkTarget)
        {
            bool res = false;
            ulong occBoard = board.whiteBoardState.Pieces | board.blackBoardState.Pieces;

            ulong remPawns = board.GetBoardState(color).Pawns;
            while (remPawns != 0)
            {
                uint squareFrom = Util.bitScanForward(remPawns);
                ulong targetBitboard = 0;

                if (color == ChessPieceColor.White)
                {
                    targetBitboard = Precomputed.WhitePawnMoves[squareFrom] & ~occBoard;
                    if (targetBitboard != 0)
                        targetBitboard |= Precomputed.WhitePawnDoubleMoves[squareFrom] & ~occBoard;
                    targetBitboard |= Precomputed.WhitePawnCaptureMoves[squareFrom] & board.blackBoardState.Pieces;

                    if ((Precomputed.WhitePawnEnPassantCaptureTargets[squareFrom] & board.enPassantTarget) != 0)
                        targetBitboard |= board.enPassantTarget << 8;
                }
                else
                {
                    targetBitboard = Precomputed.BlackPawnMoves[squareFrom] & ~occBoard;
                    if (targetBitboard != 0)
                        targetBitboard |= Precomputed.BlackPawnDoubleMoves[squareFrom] & ~occBoard;
                    targetBitboard |= Precomputed.BlackPawnCaptureMoves[squareFrom] & board.whiteBoardState.Pieces;

                    if ((Precomputed.BlackPawnEnPassantCaptureTargets[squareFrom] & board.enPassantTarget) != 0)
                        targetBitboard |= board.enPassantTarget >> 8;
                }

                if( moveList != null )
                    GenerateMoves(squareFrom, targetBitboard, ChessPieceType.Pawn, moveList);
                else
                {
                    if ((targetBitboard & checkTarget) != 0)
                    {
                        res = true;
                        break;
                    }
                }

                remPawns ^= Precomputed.IndexToBitBoard[squareFrom];  // Remove the pawn.
            }
            return res;
        }

        private static void GenerateMove(ChessPositionIndex squareFrom, ChessPositionIndex squareTo, ChessPieceType pieceType, ChessPieceType promotionPieceType, List<ChessMove> moveList)
        {
            ChessMove move = new ChessMove(squareFrom, squareTo, pieceType);
            move.PromotionPieceType = promotionPieceType;
            moveList.Add(move);
        }

        private static void GenerateMoves(uint squareFrom, ulong targetBitboard, ChessPieceType pieceType, List<ChessMove> moveList)
        {
            if (pieceType == ChessPieceType.Pawn && ((targetBitboard & Precomputed.Rows[7]) != 0 || (targetBitboard & Precomputed.Rows[0]) != 0) )
            {
                while (targetBitboard != 0)
                {
                    uint squareTo = Util.bitScanForward(targetBitboard);

                    ChessMove knightMove = new ChessMove((ChessPositionIndex)squareFrom, (ChessPositionIndex)squareTo, ChessPieceType.Pawn);
                    knightMove.PromotionPieceType = ChessPieceType.Knight;
                    moveList.Add(knightMove);

                    GenerateMove((ChessPositionIndex)squareFrom, (ChessPositionIndex)squareTo, ChessPieceType.Pawn, ChessPieceType.Knight, moveList);
                    GenerateMove((ChessPositionIndex)squareFrom, (ChessPositionIndex)squareTo, ChessPieceType.Pawn, ChessPieceType.Bishop, moveList);
                    GenerateMove((ChessPositionIndex)squareFrom, (ChessPositionIndex)squareTo, ChessPieceType.Pawn, ChessPieceType.Rook, moveList);
                    GenerateMove((ChessPositionIndex)squareFrom, (ChessPositionIndex)squareTo, ChessPieceType.Pawn, ChessPieceType.Queen, moveList);

                    targetBitboard ^= Precomputed.IndexToBitBoard[squareTo];  // Remove the possible square.
                }
            }
            else
            {
                while (targetBitboard != 0)
                {
                    uint squareTo = Util.bitScanForward(targetBitboard);

                    moveList.Add(new ChessMove((ChessPositionIndex)squareFrom, (ChessPositionIndex)squareTo, pieceType));

                    targetBitboard ^= Precomputed.IndexToBitBoard[squareTo];  // Remove the possible square.
                }
            }
        }

        private static void AddOneKnightMoves(ChessBoard board, ChessPieceColor color, uint squareFrom, List<ChessMove> moveList)
        {
            ulong eligSquares = ~board.GetBoardState(color).Pieces;   // Can't move where we have own pieces.

            ulong target = Precomputed.KnightMoves[squareFrom] & eligSquares;

            if (moveList != null)
                GenerateMoves(squareFrom, target, ChessPieceType.Knight, moveList);
        }

        private static bool AddKnightMoves(ChessBoard board, ChessPieceColor color, List<ChessMove> moveList, ulong checkTarget)
        {
            bool res = false;
            ulong eligSquares = ~board.GetBoardState(color).Pieces;   // Can't move where we have own pieces.

            ulong remKnights = board.GetBoardState(color).Knights;
            while (remKnights != 0)
            {
                uint squareFrom = Util.bitScanForward(remKnights);
                ulong target = Precomputed.KnightMoves[squareFrom] & eligSquares;

                if (moveList != null)
                    GenerateMoves(squareFrom, target, ChessPieceType.Knight, moveList);
                else
                {
                    if ((target & checkTarget) != 0)
                    {
                        res = true;
                        break;
                    }
                }

                remKnights ^= Precomputed.IndexToBitBoard[squareFrom];  // Remove the knight.
            }
            return res;
        }

        private static ulong GetRookMoves(uint squareFrom, ulong occBoard, ulong eligSquares)
        {
            ulong res = 0;

            ulong rightMoves = Precomputed.RightMoves[squareFrom] & occBoard;
            rightMoves = (rightMoves << 1) | (rightMoves << 2) | (rightMoves << 3) | (rightMoves << 4) | (rightMoves << 5) | (rightMoves << 6);
            rightMoves &= Precomputed.RightMoves[squareFrom];   // Remove spurious shifted bits.
            rightMoves ^= Precomputed.RightMoves[squareFrom];   // Get the squares we _can_ move to.
            rightMoves &= eligSquares;

            ulong leftMoves = Precomputed.LeftMoves[squareFrom] & occBoard;
            leftMoves = (leftMoves >> 1) | (leftMoves >> 2) | (leftMoves >> 3) | (leftMoves >> 4) | (leftMoves >> 5) | (leftMoves >> 6);
            leftMoves &= Precomputed.LeftMoves[squareFrom];   // Remove spurious shifted bits.
            leftMoves ^= Precomputed.LeftMoves[squareFrom];   // Get the squares we _can_ move to.
            leftMoves &= eligSquares;

            ulong upMoves = Precomputed.UpMoves[squareFrom] & occBoard;
            upMoves = (upMoves << 8) | (upMoves << 16) | (upMoves << 24) | (upMoves << 32) | (upMoves << 40) | (upMoves << 48);
            upMoves &= Precomputed.UpMoves[squareFrom];   // Remove spurious shifted bits.
            upMoves ^= Precomputed.UpMoves[squareFrom];   // Get the squares we _can_ move to.
            upMoves &= eligSquares;

            ulong downMoves = Precomputed.DownMoves[squareFrom] & occBoard;
            downMoves = (downMoves >> 8) | (downMoves >> 16) | (downMoves >> 24) | (downMoves >> 32) | (downMoves >> 40) | (downMoves >> 48);
            downMoves &= Precomputed.DownMoves[squareFrom];   // Remove spurious shifted bits.
            downMoves ^= Precomputed.DownMoves[squareFrom];   // Get the squares we _can_ move to.
            downMoves &= eligSquares;

            res = rightMoves | leftMoves | upMoves | downMoves;
            return res;
        }

        private static ulong GetBishopMoves(uint squareFrom, ulong occBoard, ulong eligSquares)
        {
            ulong res = 0;

            ulong angle45Moves = Precomputed.Angle45Moves[squareFrom] & occBoard;
            angle45Moves = (angle45Moves << 9) | (angle45Moves << 18) | (angle45Moves << 27) | (angle45Moves << 36) | (angle45Moves << 45) | (angle45Moves << 54);
            angle45Moves &= Precomputed.Angle45Moves[squareFrom];   // Remove spurious shifted bits.
            angle45Moves ^= Precomputed.Angle45Moves[squareFrom];   // Get the squares we _can_ move to.
            angle45Moves &= eligSquares;

            ulong angle135Moves = Precomputed.Angle135Moves[squareFrom] & occBoard;
            angle135Moves = (angle135Moves << 7) | (angle135Moves << 14) | (angle135Moves << 21) | (angle135Moves << 28) | (angle135Moves << 35) | (angle135Moves << 42);
            angle135Moves &= Precomputed.Angle135Moves[squareFrom];   // Remove spurious shifted bits.
            angle135Moves ^= Precomputed.Angle135Moves[squareFrom];   // Get the squares we _can_ move to.
            angle135Moves &= eligSquares;

            ulong angle225Moves = Precomputed.Angle225Moves[squareFrom] & occBoard;
            angle225Moves = (angle225Moves >> 9) | (angle225Moves >> 18) | (angle225Moves >> 27) | (angle225Moves >> 36) | (angle225Moves >> 45) | (angle225Moves >> 54);
            angle225Moves &= Precomputed.Angle225Moves[squareFrom];   // Remove spurious shifted bits.
            angle225Moves ^= Precomputed.Angle225Moves[squareFrom];   // Get the squares we _can_ move to.
            angle225Moves &= eligSquares;

            ulong angle315Moves = Precomputed.Angle315Moves[squareFrom] & occBoard;
            angle315Moves = (angle315Moves >> 7) | (angle315Moves >> 14) | (angle315Moves >> 21) | (angle315Moves >> 28) | (angle315Moves >> 35) | (angle315Moves >> 42);
            angle315Moves &= Precomputed.Angle315Moves[squareFrom];   // Remove spurious shifted bits.
            angle315Moves ^= Precomputed.Angle315Moves[squareFrom];   // Get the squares we _can_ move to.
            angle315Moves &= eligSquares;

            res = angle45Moves | angle135Moves | angle225Moves | angle315Moves;
            return res;
        }

        private static void AddOneBishopMoves(ChessBoard board, ChessPieceColor color, uint squareFrom, List<ChessMove> moveList)
        {
            ulong eligSquares = ~board.GetBoardState(color).Pieces;   // Can't move where we have own pieces.

            ulong occBoard = board.whiteBoardState.Pieces | board.blackBoardState.Pieces;

            ulong target = GetBishopMoves(squareFrom, occBoard, eligSquares);

            if (moveList != null)
                GenerateMoves(squareFrom, target, ChessPieceType.Bishop, moveList);
        }

        private static bool AddBishopMoves(ChessBoard board, ChessPieceColor color, List<ChessMove> moveList, ulong checkTarget)
        {
            bool res = false;
            ulong eligSquares = ~board.GetBoardState(color).Pieces;   // Can't move where we have own pieces.

            ulong occBoard = board.whiteBoardState.Pieces | board.blackBoardState.Pieces;

            ulong remBishops = board.GetBoardState(color).Bishops;
            while (remBishops != 0)
            {
                uint squareFrom = Util.bitScanForward(remBishops);
                ulong target = GetBishopMoves(squareFrom, occBoard, eligSquares);

                if (moveList != null)
                    GenerateMoves(squareFrom, target, ChessPieceType.Bishop, moveList);
                else
                {
                    if ((target & checkTarget) != 0)
                    {
                        res = true;
                        break;
                    }
                }

                remBishops ^= Precomputed.IndexToBitBoard[squareFrom];  // Remove the bishop.
            }
            return res;
        }

        private static void AddOneRookMoves(ChessBoard board, ChessPieceColor color, uint squareFrom, List<ChessMove> moveList)
        {
            ulong eligSquares = ~board.GetBoardState(color).Pieces;   // Can't move where we have own pieces.

            ulong occBoard = board.whiteBoardState.Pieces | board.blackBoardState.Pieces;

            ulong target = GetRookMoves(squareFrom, occBoard, eligSquares);

            if (moveList != null)
                GenerateMoves(squareFrom, target, ChessPieceType.Rook, moveList);
        }

        private static bool AddRookMoves(ChessBoard board, ChessPieceColor color, List<ChessMove> moveList, ulong checkTarget)
        {
            bool res = false;
            ulong eligSquares = ~board.GetBoardState(color).Pieces;   // Can't move where we have own pieces.

            ulong occBoard = board.whiteBoardState.Pieces | board.blackBoardState.Pieces;

            ulong remRooks = board.GetBoardState(color).Rooks;
            while (remRooks != 0)
            {
                uint squareFrom = Util.bitScanForward(remRooks);
                ulong target = GetRookMoves(squareFrom, occBoard, eligSquares);

                if (moveList != null)
                    GenerateMoves(squareFrom, target, ChessPieceType.Rook, moveList);
                else
                {
                    if ((target & checkTarget) != 0)
                    {
                        res = true;
                        break;
                    }
                }

                remRooks ^= Precomputed.IndexToBitBoard[squareFrom];  // Remove the rook.
            }

            return res;
        }

        private static void AddOneQueenMoves(ChessBoard board, ChessPieceColor color, uint squareFrom, List<ChessMove> moveList)
        {
            ulong eligSquares = ~board.GetBoardState(color).Pieces;   // Can't move where we have own pieces.

            ulong occBoard = board.whiteBoardState.Pieces | board.blackBoardState.Pieces;

            ulong target = GetRookMoves(squareFrom, occBoard, eligSquares) | GetBishopMoves(squareFrom, occBoard, eligSquares);

            if (moveList != null)
                GenerateMoves(squareFrom, target, ChessPieceType.Queen, moveList);
        }

        private static bool AddQueenMoves(ChessBoard board, ChessPieceColor color, List<ChessMove> moveList, ulong checkTarget)
        {
            bool res = false;
            ulong eligSquares = ~board.GetBoardState(color).Pieces;   // Can't move where we have own pieces.

            ulong occBoard = board.whiteBoardState.Pieces | board.blackBoardState.Pieces;

            ulong remQueens = board.GetBoardState(color).Queens;
            while (remQueens != 0)
            {
                uint squareFrom = Util.bitScanForward(remQueens);
                ulong target = GetRookMoves(squareFrom, occBoard, eligSquares) | GetBishopMoves(squareFrom, occBoard, eligSquares);

                if( moveList != null )
                    GenerateMoves(squareFrom, target, ChessPieceType.Queen, moveList);
                else
                {
                    if ((target & checkTarget) != 0)
                    {
                        res = true;
                        break;
                    }
                }

                remQueens ^= Precomputed.IndexToBitBoard[squareFrom];  // Remove the queen.
            }
            return res;
        }

        private static bool AddKingMoves(ChessBoard board, ChessPieceColor color, List<ChessMove> moveList, ulong checkTarget)
        {
            bool res = false;
            ulong eligSquares = ~board.GetBoardState(color).Pieces;   // Can't move where we have own pieces.

            uint squareFrom = Util.fastBitScanForward(board.GetBoardState(color).King);
            ulong target = Precomputed.KingMoves[squareFrom] & eligSquares;

            if (moveList != null)
            {
                // Castling
                ulong occBoard = board.whiteBoardState.Pieces | board.blackBoardState.Pieces;
                if (color == ChessPieceColor.White)
                {
                    if (!IsInCheck(board, ChessPieceColor.Black, Precomputed.IndexToBitBoard[(int)ChessPositionIndex.E1]))
                    {
                        if (board.GetBoardState(color).KingsideCastlingPossible
                            && (occBoard & Precomputed.WhiteKingsideCastlingSquares) == 0
                            && !IsInCheck(board, ChessPieceColor.Black, Precomputed.IndexToBitBoard[(int)ChessPositionIndex.F1]))
                        {
                            ChessMove move = new ChessMove((ChessPositionIndex)squareFrom, ChessPositionIndex.G1, ChessPieceType.King);
                            move.IsKingsideCastling = true;
                            moveList.Add(move);
                        }
                        if (board.GetBoardState(color).QueensideCastlingPossible
                            && (occBoard & Precomputed.WhiteQueensideCastlingSquares) == 0
                            && !IsInCheck(board, ChessPieceColor.Black, Precomputed.IndexToBitBoard[(int)ChessPositionIndex.D1]))
                        {
                            ChessMove move = new ChessMove((ChessPositionIndex)squareFrom, ChessPositionIndex.C1, ChessPieceType.King);
                            move.IsQueensideCastling = true;
                            moveList.Add(move);
                        }
                    }
                }
                else
                {
                    if (!IsInCheck(board, ChessPieceColor.White, Precomputed.IndexToBitBoard[(int)ChessPositionIndex.E8]))
                    {
                        if (board.GetBoardState(color).KingsideCastlingPossible
                            && (occBoard & Precomputed.BlackKingsideCastlingSquares) == 0
                            && !IsInCheck(board, ChessPieceColor.White, Precomputed.IndexToBitBoard[(int)ChessPositionIndex.F8]))
                        {
                            ChessMove move = new ChessMove((ChessPositionIndex)squareFrom, ChessPositionIndex.G8, ChessPieceType.King);
                            move.IsKingsideCastling = true;
                            moveList.Add(move);
                        }
                        if (board.GetBoardState(color).QueensideCastlingPossible
                            && (occBoard & Precomputed.BlackQueensideCastlingSquares) == 0
                            && !IsInCheck(board, ChessPieceColor.White, Precomputed.IndexToBitBoard[(int)ChessPositionIndex.D8]))
                        {
                            ChessMove move = new ChessMove((ChessPositionIndex)squareFrom, ChessPositionIndex.C8, ChessPieceType.King);
                            move.IsQueensideCastling = true;
                            moveList.Add(move);
                        }
                    }
                }

                GenerateMoves(squareFrom, target, ChessPieceType.King, moveList);
            }
            else
            {
                if ((target & checkTarget) != 0)
                    res = true;
            }

            return res;
        }

        public bool MakeMove(int fromX, int fromY, int toX, int toY, ChessPieceType promotionPieceType, bool validateOnly)
        {
            if (fromX < 0 || fromX > 7 || fromY < 0 || fromY > 7
                || toX < 0 || toX > 7 || toY < 0 || toY > 7)
                throw new Exception("Invalid index");

            ChessPositionIndex fromIndex = (ChessPositionIndex)fromX + 8 * fromY;
            ChessPositionIndex toIndex = (ChessPositionIndex)toX + 8 * toY;

            ChessPiece piece = new ChessPiece();
            if( !Board.GetPiece(fromIndex, ref piece) )
                throw new Exception("Piece not found");

            ChessMove move = new ChessMove(fromIndex, toIndex, piece.PieceType);
            move.PromotionPieceType = promotionPieceType;

            return MakeMove(move, validateOnly);
        }

        public bool MakeMove(ChessMove move, bool validateOnly)
        {
            bool res = false;
            ChessPiece piece = new ChessPiece();

            if (move.FromIndex < ChessPositionIndex.A1 || move.FromIndex > ChessPositionIndex.H8
                || move.ToIndex < ChessPositionIndex.A1 || move.ToIndex > ChessPositionIndex.H8
                || move.FromIndex == move.ToIndex )
                throw new Exception("Invalid index");

            if( Board.GetPiece(move.FromIndex, ref piece) && piece.PieceColor == Turn)
            {
                move.PieceType = piece.PieceType;

                if( !Board.IsColor(move.ToIndex, piece.PieceColor ) )
                {
                    List<ChessMove> possibleMoves = GetPossibleMoves(Board, Turn);
                    bool validMove = false;
                    foreach (ChessMove possibleMove in possibleMoves)
                    {
                        if (possibleMove.FromIndex == move.FromIndex
                            && possibleMove.ToIndex == move.ToIndex)
                        {
                            move.IsKingsideCastling = possibleMove.IsKingsideCastling;
                            move.IsQueensideCastling = possibleMove.IsQueensideCastling;
                            validMove = true;
                            break;
                        }
                    }

                    if( validMove )
                    {
                        // Make a copy so we can "undo" the moves if in check after a move.
                        ChessBoard boardCopy = new ChessBoard(Board, true);

                        ChessMoveInfo moveInfo = new ChessMoveInfo();

                        boardCopy.ApplyMove(Turn, move, moveInfo);

                        if (!IsInCheck(boardCopy, Turn))
                        {
                            if (validateOnly)
                                res = true;
                            else
                            {
                                if (move.IsCapture)
                                {
                                    ChessPiece capturedPiece = new ChessPiece();
                                    if (Board.GetPiece(moveInfo.CapturedPiecePos, ref capturedPiece))
                                        boardCopy.CapturedPieces.Add(capturedPiece);
                                }

                                Board = boardCopy;

                                ChessMove moveCopy = new ChessMove(move);
                                moveCopy.ResultingBoard = Board;
                                chessMoves.Add(moveCopy);

                                Turn = InvertColor(Turn);

                                GameState = LookForCheckMateOrDraw(Board, Turn, chessMoves);
                                if (GameState != ChessGameState.Active)
                                    Turn = ChessPieceColor.None;

                                RaisePieceMovedEvent(moveCopy, moveInfo);

                                res = true;
                            }
                        }
                    }
                }
            }

            return res;
        }

        public void Undo(int numberOfMoves)
        {
            for (int i = 0; i < numberOfMoves && chessMoves.Count > 0; i++)
            {
                chessMoves.RemoveAt(chessMoves.Count - 1);
                Turn = InvertColor(Turn); 
            }

            if (chessMoves.Count == 0)
            {
                this.Board = new ChessBoard(this);
                this.Board.InitBoard();
            }
            else
                this.Board = LastMove.ResultingBoard;
        }

        public static ChessPieceColor InvertColor(ChessPieceColor color)
        {
            ChessPieceColor c = ChessPieceColor.None;
            if (color == ChessPieceColor.White)
                c = ChessPieceColor.Black;
            else
                c = ChessPieceColor.White;
            return c;
        }

        public static ChessGameState LookForCheckMateOrDraw(ChessBoard board, ChessPieceColor turn, List<ChessMove> chessMoves)
        {
            ChessGameState newGameState = ChessGameState.Active;

            List<ChessMove> possibleMoves = GetPossibleMoves(board, turn);
            if (possibleMoves.Count == 0)
            {
                if (IsInCheck(board, turn))
                    newGameState = ChessGameState.CheckMate;
                else
                    newGameState = ChessGameState.StaleMate;
            }

            if (newGameState == ChessGameState.Active)
            {
                if (chessMoves != null && chessMoves.Count > 8)
                {
                    int index = chessMoves.Count - 1;
                    if (CompareMoves(index - 7, index - 3, chessMoves)
                        && CompareMoves(index - 6, index - 2, chessMoves)
                        && CompareMoves(index - 5, index - 1, chessMoves)
                        && CompareMoves(index - 4, index, chessMoves))
                        newGameState = ChessGameState.DrawByRepetition;
                }
            }

            if (newGameState == ChessGameState.Active)
            {
                if (board.count50MoveRule > 99)
                    newGameState = ChessGameState.DrawBy50MoveRule;
            }

            if (newGameState == ChessGameState.Active)
            {
                if (board.CheckDrawByMaterial())
                    newGameState = ChessGameState.DrawByMaterial;
            }

            return newGameState;
        }

        private static bool CompareMoves(int index1, int index2, List<ChessMove> chessMoves)
        {
            bool res = false;
            ChessMove move1 = chessMoves[index1];
            ChessMove move2 = chessMoves[index2];
            if (move1.Equals(move2) && !move1.IsCapture && !move2.IsCapture)
                res = true;
            return res;
        }

        internal static bool IsInCheck(ChessBoard board, ChessPieceColor colorToCheck, ulong checkTarget)
        {
            bool res = false;

            if (AddPawnMoves(board, colorToCheck, null, checkTarget))
                res = true;
            else if (AddKnightMoves(board, colorToCheck, null, checkTarget))
                res = true;
            else if (AddBishopMoves(board, colorToCheck, null, checkTarget))
                res = true;
            else if (AddRookMoves(board, colorToCheck, null, checkTarget))
                res = true;
            else if (AddQueenMoves(board, colorToCheck, null, checkTarget))
                res = true;
            else if (AddKingMoves(board, colorToCheck, null, checkTarget))
                res = true;

            return res;
        }

        internal static bool IsInCheck(ChessBoard board, ChessPieceColor kingColor)
        {
            ChessPieceColor colorToCheck = InvertColor(kingColor);
            ulong checkTarget = board.GetBoardState(kingColor).King;

            return IsInCheck(board, colorToCheck, checkTarget);
        }
    }
}
