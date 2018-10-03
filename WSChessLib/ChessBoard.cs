using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Runtime.Serialization;

/*
 * This file is common between XNA and Windows Phone projects!
 */

namespace ChessLib
{
    public enum ChessPositionIndex
    {
        None = -1,
        A1 = 0x00, B1 = 0x01, C1 = 0x02, D1 = 0x03, E1 = 0x04, F1 = 0x05, G1 = 0x06, H1 = 0x07,
        A2 = 0x08, B2 = 0x09, C2 = 0x0A, D2 = 0x0B, E2 = 0x0C, F2 = 0x0D, G2 = 0x0E, H2 = 0x0F,
        A3 = 0x10, B3 = 0x11, C3 = 0x12, D3 = 0x13, E3 = 0x14, F3 = 0x15, G3 = 0x16, H3 = 0x17,
        A4 = 0x18, B4 = 0x19, C4 = 0x1A, D4 = 0x1B, E4 = 0x1C, F4 = 0x1D, G4 = 0x1E, H4 = 0x1F,
        A5 = 0x20, B5 = 0x21, C5 = 0x22, D5 = 0x23, E5 = 0x24, F5 = 0x25, G5 = 0x26, H5 = 0x27,
        A6 = 0x28, B6 = 0x29, C6 = 0x2A, D6 = 0x2B, E6 = 0x2C, F6 = 0x2D, G6 = 0x2E, H6 = 0x2F,
        A7 = 0x30, B7 = 0x31, C7 = 0x32, D7 = 0x33, E7 = 0x34, F7 = 0x35, G7 = 0x36, H7 = 0x37,
        A8 = 0x38, B8 = 0x39, C8 = 0x3A, D8 = 0x3B, E8 = 0x3C, F8 = 0x3D, G8 = 0x3E, H8 = 0x3F,
    }

    [DataContract]
    public class ChessBoard
    {
        [XmlIgnore]
        public ChessGame Game { get; set; }

        [DataMember]
        internal ChessBoardColorState whiteBoardState = null;
        [DataMember]
        internal ChessBoardColorState blackBoardState = null;

        [DataMember]
        internal ulong enPassantTarget = 0;

        [DataMember]
        internal int count50MoveRule = 0;

        private List<ChessPiece> capturedPieces = null;

        // For save/restore.
        [XmlElement("wb")]
        public ChessBoardColorState WhiteBoardState { get { return whiteBoardState; } set { whiteBoardState = value; } }
        [XmlElement("bb")]
        public ChessBoardColorState BlackBoardState { get { return blackBoardState; } set { blackBoardState = value; } }
        [XmlElement("ep")]
        public ulong EnPassantTarget { get { return enPassantTarget; } set { enPassantTarget = value; } }
        [XmlElement("c50")]
        public int Count50MoveRule { get { return count50MoveRule; } set { count50MoveRule = value; } }

        [XmlElement("cp")] 
        public List<ChessPiece> CapturedPieces { get { return capturedPieces; } set { capturedPieces = value; } }

        /// <summary>
        /// Creates a new board.
        /// </summary>
        public ChessBoard(ChessGame game)
        {
            this.Game = game;
            InitBoardState();
            this.capturedPieces = new List<ChessPiece>();
        }

        public ChessBoard()
        {
            InitBoardState();
            this.capturedPieces = new List<ChessPiece>();
        }

        public void HookUp(ChessGame game)
        {
            this.Game = game;
        }

        public ChessBoard(ChessBoard boardToCopy)
            : this(boardToCopy, false)
        {
        }

        private void InitBoardState()
        {
            this.whiteBoardState = new ChessBoardColorState();
            this.blackBoardState = new ChessBoardColorState();
        }

        public ChessBoardColorState GetBoardState(ChessPieceColor color)
        {
            ChessBoardColorState boardState = null;
            if( color == ChessPieceColor.White )
                boardState = whiteBoardState;
            else
                boardState = blackBoardState;
            return boardState;
        }

        /// <summary>
        /// Makes a copy of a chess board.
        /// </summary>
        /// <param name="boardToCopy"></param>
        public ChessBoard(ChessBoard boardToCopy, bool copyExtra)
        {
            this.Game = boardToCopy.Game;

            this.whiteBoardState = new ChessBoardColorState(boardToCopy.whiteBoardState);
            this.blackBoardState = new ChessBoardColorState(boardToCopy.blackBoardState);

            this.enPassantTarget = boardToCopy.enPassantTarget;
            this.count50MoveRule = boardToCopy.count50MoveRule;

            this.capturedPieces = new List<ChessPiece>();
            if (copyExtra)
            {
                foreach (ChessPiece piece in boardToCopy.capturedPieces)
                    this.capturedPieces.Add(piece);
            }
        }

        public void ApplyMove(ChessPieceColor color, ChessMove move, ChessMoveInfo moveInfo)
        {
            // Make the move...
            ulong fromPos = Precomputed.IndexToBitBoard[(int)move.FromIndex];
            ulong toPos = Precomputed.IndexToBitBoard[(int)move.ToIndex];

            ulong capturePos = toPos;

            if (moveInfo != null)
                moveInfo.MovedBy = color;

            count50MoveRule++;

            ChessBoardColorState boardState = GetBoardState(color);
            boardState.Pieces &= ~fromPos;  // Move the piece.
            boardState.Pieces |= toPos;     // Move the piece.
            switch (move.PieceType)
            {
                case ChessPieceType.Pawn:
                    count50MoveRule = 0;

                    boardState.Pawns &= ~fromPos;
                    switch (move.PromotionPieceType)
                    {
                        case ChessPieceType.Knight:
                            boardState.Knights |= toPos;
                            break;
                        case ChessPieceType.Bishop:
                            boardState.Bishops |= toPos;
                            break;
                        case ChessPieceType.Rook:
                            boardState.Rooks |= toPos;
                            break;
                        case ChessPieceType.Queen:
                            boardState.Queens |= toPos;
                            break;
                        default:
                            boardState.Pawns |= toPos;
                            break;
                    }

                    if (color == ChessPieceColor.White)
                    {
                        if ((Precomputed.WhitePawnCaptureMoves[(int)move.FromIndex] & toPos & (enPassantTarget << 8)) != 0)
                        {
                            capturePos = enPassantTarget;
                            enPassantTarget = 0;
                        }
                        else if ((Precomputed.WhitePawnDoubleMoves[(int)move.FromIndex] & toPos) != 0)
                            enPassantTarget = toPos;
                        else
                            enPassantTarget = 0;
                    }
                    else
                    {
                        if ((Precomputed.BlackPawnCaptureMoves[(int)move.FromIndex] & toPos & (enPassantTarget >> 8)) != 0)
                        {
                            capturePos = enPassantTarget;
                            enPassantTarget = 0;
                        }
                        else if ((Precomputed.BlackPawnDoubleMoves[(int)move.FromIndex] & toPos) != 0)
                            enPassantTarget = toPos;
                        else
                            enPassantTarget = 0;
                    }

                    break;
                case ChessPieceType.Knight:
                    enPassantTarget = 0;
                    boardState.Knights &= ~fromPos;
                    boardState.Knights |= toPos;
                    break;
                case ChessPieceType.Bishop:
                    enPassantTarget = 0;
                    boardState.Bishops &= ~fromPos;
                    boardState.Bishops |= toPos;
                    break;
                case ChessPieceType.Rook:
                    enPassantTarget = 0;
                    boardState.Rooks &= ~fromPos;
                    boardState.Rooks |= toPos;

                    if( (fromPos & Precomputed.IndexToBitBoard[(int)ChessPositionIndex.A1]) != 0 )
                        whiteBoardState.QueensideCastlingPossible = false;
                    else if ((fromPos & Precomputed.IndexToBitBoard[(int)ChessPositionIndex.H1]) != 0)
                        whiteBoardState.KingsideCastlingPossible = false;
                    else if ((fromPos & Precomputed.IndexToBitBoard[(int)ChessPositionIndex.A8]) != 0)
                        blackBoardState.QueensideCastlingPossible = false;
                    else if ((fromPos & Precomputed.IndexToBitBoard[(int)ChessPositionIndex.H8]) != 0)
                        blackBoardState.KingsideCastlingPossible = false;

                    break;
                case ChessPieceType.Queen:
                    enPassantTarget = 0;
                    boardState.Queens &= ~fromPos;
                    boardState.Queens |= toPos;
                    break;
                case ChessPieceType.King:
                    enPassantTarget = 0;
                    boardState.King &= ~fromPos;
                    boardState.King |= toPos;

                    boardState.KingsideCastlingPossible = false;
                    boardState.QueensideCastlingPossible = false;

                    if (move.IsKingsideCastling)
                    {
                        if (color == ChessPieceColor.White)
                            MoveRookWhenCastling(color, ChessPositionIndex.H1, ChessPositionIndex.F1, moveInfo);
                        else
                            MoveRookWhenCastling(color, ChessPositionIndex.H8, ChessPositionIndex.F8, moveInfo);
                    }
                    else if (move.IsQueensideCastling)
                    {
                        if (color == ChessPieceColor.White)
                            MoveRookWhenCastling(color, ChessPositionIndex.A1, ChessPositionIndex.D1, moveInfo);
                        else
                            MoveRookWhenCastling(color, ChessPositionIndex.A8, ChessPositionIndex.D8, moveInfo);
                    }

                    break;
            }

            ChessPieceColor invertedColor = ChessGame.InvertColor(color);
            ChessBoardColorState invertedBoardState = GetBoardState(invertedColor);
            if ((invertedBoardState.Pieces & capturePos) != 0)   // Check if there is a capture.
            {
                invertedBoardState.Pieces &= ~capturePos;        // Remove the piece from all black pieces.
                invertedBoardState.Pawns &= ~capturePos;
                invertedBoardState.Knights &= ~capturePos;
                invertedBoardState.Bishops &= ~capturePos;
                invertedBoardState.Rooks &= ~capturePos;
                invertedBoardState.Queens &= ~capturePos;

                move.IsCapture = true;
                count50MoveRule = 0;
                if (moveInfo != null)
                {
                    moveInfo.CapturedPiecePos = (ChessPositionIndex)Util.fastBitScanForward(capturePos);
                }

                // PP 2012-12-24: when a rook is captured, castling is no longer possible.
                if ((capturePos & Precomputed.IndexToBitBoard[(int)ChessPositionIndex.A1]) != 0)
                    whiteBoardState.QueensideCastlingPossible = false;
                else if ((capturePos & Precomputed.IndexToBitBoard[(int)ChessPositionIndex.H1]) != 0)
                    whiteBoardState.KingsideCastlingPossible = false;
                else if ((capturePos & Precomputed.IndexToBitBoard[(int)ChessPositionIndex.A8]) != 0)
                    blackBoardState.QueensideCastlingPossible = false;
                else if ((capturePos & Precomputed.IndexToBitBoard[(int)ChessPositionIndex.H8]) != 0)
                    blackBoardState.KingsideCastlingPossible = false;
            }
        }

        private void MoveRookWhenCastling(ChessPieceColor color, ChessPositionIndex fromIndex, ChessPositionIndex toIndex, ChessMoveInfo moveInfo)
        {
            ulong rookFromPos = Precomputed.IndexToBitBoard[(int)fromIndex];
            ulong rookToPos = Precomputed.IndexToBitBoard[(int)toIndex];

            ChessBoardColorState boardState = GetBoardState(color);

            boardState.Pieces &= ~rookFromPos;
            boardState.Pieces |= rookToPos;

            boardState.Rooks &= ~rookFromPos;
            boardState.Rooks |= rookToPos;

            if (moveInfo != null)
            {
                moveInfo.SecondaryFromIndex = fromIndex;
                moveInfo.SecondaryToIndex = toIndex;
            }
        }

        public void InitBoard()
        {
            Clear();

            // First row.
            SetPiece(ChessPositionIndex.A1, ChessPieceType.Rook, ChessPieceColor.White);
            SetPiece(ChessPositionIndex.B1, ChessPieceType.Knight, ChessPieceColor.White);
            SetPiece(ChessPositionIndex.C1, ChessPieceType.Bishop, ChessPieceColor.White);
            SetPiece(ChessPositionIndex.D1, ChessPieceType.Queen, ChessPieceColor.White);
            SetPiece(ChessPositionIndex.E1, ChessPieceType.King, ChessPieceColor.White);
            SetPiece(ChessPositionIndex.F1, ChessPieceType.Bishop, ChessPieceColor.White);
            SetPiece(ChessPositionIndex.G1, ChessPieceType.Knight, ChessPieceColor.White);
            SetPiece(ChessPositionIndex.H1, ChessPieceType.Rook, ChessPieceColor.White);

            // Second row.
            SetPiece(ChessPositionIndex.A2, ChessPieceType.Pawn, ChessPieceColor.White);
            SetPiece(ChessPositionIndex.B2, ChessPieceType.Pawn, ChessPieceColor.White);
            SetPiece(ChessPositionIndex.C2, ChessPieceType.Pawn, ChessPieceColor.White);
            SetPiece(ChessPositionIndex.D2, ChessPieceType.Pawn, ChessPieceColor.White);
            SetPiece(ChessPositionIndex.E2, ChessPieceType.Pawn, ChessPieceColor.White);
            SetPiece(ChessPositionIndex.F2, ChessPieceType.Pawn, ChessPieceColor.White);
            SetPiece(ChessPositionIndex.G2, ChessPieceType.Pawn, ChessPieceColor.White);
            SetPiece(ChessPositionIndex.H2, ChessPieceType.Pawn, ChessPieceColor.White);

            // 7th row.
            SetPiece(ChessPositionIndex.A7, ChessPieceType.Pawn, ChessPieceColor.Black);
            SetPiece(ChessPositionIndex.B7, ChessPieceType.Pawn, ChessPieceColor.Black);
            SetPiece(ChessPositionIndex.C7, ChessPieceType.Pawn, ChessPieceColor.Black);
            SetPiece(ChessPositionIndex.D7, ChessPieceType.Pawn, ChessPieceColor.Black);
            SetPiece(ChessPositionIndex.E7, ChessPieceType.Pawn, ChessPieceColor.Black);
            SetPiece(ChessPositionIndex.F7, ChessPieceType.Pawn, ChessPieceColor.Black);
            SetPiece(ChessPositionIndex.G7, ChessPieceType.Pawn, ChessPieceColor.Black);
            SetPiece(ChessPositionIndex.H7, ChessPieceType.Pawn, ChessPieceColor.Black);

            // 8th row.
            SetPiece(ChessPositionIndex.A8, ChessPieceType.Rook, ChessPieceColor.Black);
            SetPiece(ChessPositionIndex.B8, ChessPieceType.Knight, ChessPieceColor.Black);
            SetPiece(ChessPositionIndex.C8, ChessPieceType.Bishop, ChessPieceColor.Black);
            SetPiece(ChessPositionIndex.D8, ChessPieceType.Queen, ChessPieceColor.Black);
            SetPiece(ChessPositionIndex.E8, ChessPieceType.King, ChessPieceColor.Black);
            SetPiece(ChessPositionIndex.F8, ChessPieceType.Bishop, ChessPieceColor.Black);
            SetPiece(ChessPositionIndex.G8, ChessPieceType.Knight, ChessPieceColor.Black);
            SetPiece(ChessPositionIndex.H8, ChessPieceType.Rook, ChessPieceColor.Black);
        }

        public void SetPiece(ChessPositionIndex index, ChessPieceType pieceType, ChessPieceColor pieceColor)
        {
            UInt64 pos = (UInt64)1 << (int)index;

            if (pieceColor == ChessPieceColor.None)
                SetEmpty(index);
            else
            {
                ChessBoardColorState boardState = GetBoardState(pieceColor);

                boardState.Pieces |= pos;
                switch (pieceType)
                {
                    case ChessPieceType.Pawn:
                        boardState.Pawns |= pos;
                        break;
                    case ChessPieceType.Knight:
                        boardState.Knights |= pos;
                        break;
                    case ChessPieceType.Bishop:
                        boardState.Bishops |= pos;
                        break;
                    case ChessPieceType.Rook:
                        boardState.Rooks |= pos;
                        break;
                    case ChessPieceType.Queen:
                        boardState.Queens |= pos;
                        break;
                    case ChessPieceType.King:
                        boardState.King |= pos;
                        break;
                }
            }
        }

        public void SetPiece(int x, int y, ChessPiece piece)
        {
            SetPiece((ChessPositionIndex)x + 8 * y, piece.PieceType, piece.PieceColor);
        }

        public void Clear()
        {
            whiteBoardState.Pieces = 0;
            whiteBoardState.Pawns = 0;
            whiteBoardState.Knights = 0;
            whiteBoardState.Bishops = 0;
            whiteBoardState.Rooks = 0;
            whiteBoardState.Queens = 0;
            whiteBoardState.King = 0;

            blackBoardState.Pieces = 0;
            blackBoardState.Pawns = 0;
            blackBoardState.Knights = 0;
            blackBoardState.Bishops = 0;
            blackBoardState.Rooks = 0;
            blackBoardState.Queens = 0;
            blackBoardState.King = 0;
        }

        internal void SetEmpty(ChessPositionIndex index)
        {
            UInt64 pos = ~((UInt64)1 << (int)index);

            whiteBoardState.Pieces &= pos;
            whiteBoardState.Pawns &= pos;
            whiteBoardState.Knights &= pos;
            whiteBoardState.Bishops &= pos;
            whiteBoardState.Rooks &= pos;
            whiteBoardState.Queens &= pos;
            whiteBoardState.King &= pos;

            blackBoardState.Pieces &= pos;
            blackBoardState.Pawns &= pos;
            blackBoardState.Knights &= pos;
            blackBoardState.Bishops &= pos;
            blackBoardState.Rooks &= pos;
            blackBoardState.Queens &= pos;
            blackBoardState.King &= pos;
        }

        public void SetEmpty(int x, int y)
        {
            SetEmpty((ChessPositionIndex)x + 8 * y);
        }

        public static int GetRowFromIndex(ChessPositionIndex index)
        {
            return (int)index / 8;
        }

        public static int GetColumnFromIndex(ChessPositionIndex index)
        {
            return (int)index % 8;
        }

        public int GetRow(ChessPositionIndex index)
        {
            return (int)index / 8;
        }

        public int GetColumn(ChessPositionIndex index)
        {
            return (int)index % 8;
        }

        public bool GetPiece(ChessPositionIndex index, ref ChessPiece piece)
        {
            ChessPieceType pieceType;
            ChessPieceColor pieceColor;
            bool res = GetPiece(index, out pieceType, out pieceColor);
            if (res)
            {
                piece.PieceType = pieceType;
                piece.PieceColor = pieceColor;
            }
            else
            {
                piece.PieceType = ChessPieceType.None;
                piece.PieceColor = ChessPieceColor.None;
            }
            return res;
        }

        public bool GetPiece(int x, int y, ref ChessPiece piece)
        {
            return GetPiece((ChessPositionIndex)x + 8 * y, ref piece);
        }

        public bool IsPiece(ChessPositionIndex index, ChessPieceType pieceType, ChessPieceColor pieceColor)
        {
            bool res = false;
            ChessPiece piece = new ChessPiece();
            if (GetPiece(index, ref piece))
            {
                if (piece.PieceColor == pieceColor && piece.PieceType == pieceType)
                    res = true;
            }
            return res;
        }

        public bool GetPiece(ChessPositionIndex index, out ChessPieceType pieceType, out ChessPieceColor pieceColor)
        {
            bool res = false;
            UInt64 pos = (UInt64)1 << (int)index;

            pieceType = ChessPieceType.None;
            pieceColor = ChessPieceColor.None;

            for (ChessPieceColor color = ChessPieceColor.White; color <= ChessPieceColor.Black; color++)
            {
                ChessBoardColorState boardState = GetBoardState(color);

                if ((boardState.Pieces & pos) != 0)
                {
                    pieceColor = color;
                    if ((boardState.Pawns & pos) != 0)
                        pieceType = ChessPieceType.Pawn;
                    else if ((boardState.Knights & pos) != 0)
                        pieceType = ChessPieceType.Knight;
                    else if ((boardState.Bishops & pos) != 0)
                        pieceType = ChessPieceType.Bishop;
                    else if ((boardState.Rooks & pos) != 0)
                        pieceType = ChessPieceType.Rook;
                    else if ((boardState.Queens & pos) != 0)
                        pieceType = ChessPieceType.Queen;
                    else if ((boardState.King & pos) != 0)
                        pieceType = ChessPieceType.King;

                    res = true;
                    break;
                }
            }

            return res;
        }

        public bool IsEmpty(ChessPositionIndex index)
        {
            UInt64 pos = (UInt64)1 << (int)index;

            return ((whiteBoardState.Pieces & pos) == 0 && (blackBoardState.Pieces & pos) == 0);
        }

        public bool IsColor(ChessPositionIndex index, ChessPieceColor color)
        {
            UInt64 pos = (UInt64)1 << (int)index;

            return ((GetBoardState(color).Pieces & pos) != 0);
        }

        public bool IsTypeAndColor(ChessPositionIndex index, ChessPieceType type, ChessPieceColor color)
        {
            bool res = false;
            ChessPiece piece = new ChessPiece();
            if (GetPiece(index, ref piece))
            {
                res = (piece.PieceColor == color && piece.PieceType == type);
            }
            return res;
        }

        public void CapturePiece(ChessPositionIndex index)
        {
            ChessPiece piece = new ChessPiece();
            if (GetPiece(index, ref piece))
            {
                capturedPieces.Add(piece);
            }
            SetEmpty(index);
        }

        internal void GetKing(ChessPieceColor chessPieceColor, out ChessPositionIndex kingIndex)
        {
            kingIndex = ChessPositionIndex.A1;
            Byte piece = (Byte)((Byte)ChessPieceType.King | (Byte)chessPieceColor);
            UInt64 pos = 1;
            for (ChessPositionIndex index = ChessPositionIndex.A1; index <= ChessPositionIndex.H8; index ++, pos <<= 1 )
            {
                if ((GetBoardState(chessPieceColor).King & pos) != 0 )
                {
                    kingIndex = index;
                    break;
                }
            }
        }

        public void GetPiecesValue(out int whiteValue, out int blackValue)
        {
            whiteValue = 0;
            blackValue = 0;

            whiteValue = Util.getBitCountFromBitboard(whiteBoardState.Pawns) * GetPieceValue(ChessPieceType.Pawn)
                + Util.getBitCountFromBitboard(whiteBoardState.Knights) * GetPieceValue(ChessPieceType.Knight)
                + Util.getBitCountFromBitboard(whiteBoardState.Bishops) * GetPieceValue(ChessPieceType.Bishop)
                + Util.getBitCountFromBitboard(whiteBoardState.Rooks) * GetPieceValue(ChessPieceType.Rook)
                + Util.getBitCountFromBitboard(whiteBoardState.Queens) * GetPieceValue(ChessPieceType.Queen);

            blackValue = Util.getBitCountFromBitboard(blackBoardState.Pawns) * GetPieceValue(ChessPieceType.Pawn)
                + Util.getBitCountFromBitboard(blackBoardState.Knights) * GetPieceValue(ChessPieceType.Knight)
                + Util.getBitCountFromBitboard(blackBoardState.Bishops) * GetPieceValue(ChessPieceType.Bishop)
                + Util.getBitCountFromBitboard(blackBoardState.Rooks) * GetPieceValue(ChessPieceType.Rook)
                + Util.getBitCountFromBitboard(blackBoardState.Queens) * GetPieceValue(ChessPieceType.Queen);
        }

        public int GetPieceValue(ChessPieceType pieceType)
        {
            int value = 0;
            switch (pieceType)
            {
                case ChessPieceType.Pawn:
                    value = 1;
                    break;
                case ChessPieceType.Knight:
                    value = 3;
                    break;
                case ChessPieceType.Bishop:
                    value = 3;
                    break;
                case ChessPieceType.Rook:
                    value = 5;
                    break;
                case ChessPieceType.Queen:
                    value = 9;
                    break;
                case ChessPieceType.King:   // Invaluable!
                    value = 0;
                    break;
            }
            return value;
        }

        internal bool CheckDrawByMaterial()
        {
            bool isDraw = false;

            if (whiteBoardState.Pawns == 0
                && whiteBoardState.Rooks == 0
                && whiteBoardState.Queens == 0
                && blackBoardState.Pawns == 0
                && blackBoardState.Rooks == 0
                && blackBoardState.Queens == 0)
            {
                // Here we know that only kings with bishops or knights are left.

                int whiteKnightCount = Util.getBitCountFromBitboard(whiteBoardState.Knights);
                int blackKnightCount = Util.getBitCountFromBitboard(blackBoardState.Knights);
                int whiteBishopCount = Util.getBitCountFromBitboard(whiteBoardState.Bishops);
                int blackBishopCount = Util.getBitCountFromBitboard(blackBoardState.Bishops);

                if (whiteBoardState.Knights == 0 && blackBoardState.Knights == 0 && whiteBoardState.Bishops == 0 && blackBoardState.Bishops == 0)
                    isDraw = true;  // King vs king.
                else if (whiteKnightCount == 1 && blackBoardState.Knights == 0 && whiteBoardState.Bishops == 0 && blackBoardState.Bishops == 0)
                    isDraw = true;  // White king and white knight vs black king.
                else if (whiteBoardState.Knights == 0 && blackKnightCount == 1 && whiteBoardState.Bishops == 0 && blackBoardState.Bishops == 0)
                    isDraw = true;  // Black king and black knight vs white king.    
                else if (whiteBoardState.Knights == 0 && blackBoardState.Knights == 0 && whiteBishopCount == 1 && blackBoardState.Bishops == 0)
                    isDraw = true;  // White king and white bishop vs black king.
                else if (whiteBoardState.Knights == 0 && blackBoardState.Knights == 0 && whiteBoardState.Bishops == 0 && blackBishopCount == 1)
                    isDraw = true;  // Black king and black bishop vs white king.    
                else if( whiteBoardState.Knights == 0 && blackBoardState.Knights == 0 )
                {
                    // Only bishops left, special check.
                    if (((whiteBoardState.Bishops | blackBoardState.Bishops) & Precomputed.EvenSquares) == 0)
                        isDraw = true;  // There are only bihops on odd squares left.
                    else if (((whiteBoardState.Bishops | blackBoardState.Bishops) & Precomputed.OddSquares) == 0)
                        isDraw = true;  // There are only bishops on even squares left.
                }
            }

            return isDraw;
        }
    }
}
