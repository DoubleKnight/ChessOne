//#define LOGGING
//#define PROF

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Xml.Serialization;

namespace ChessOneLib
{
    public class ChessComputer : Player
    {
        public enum Level
        {
            Easy = 1,
            Medium = 2,
            Hard = 3,
        }

        public delegate void ConsideringMoveHandler(object sender, ConsideringMoveEventArgs e);

        public event ConsideringMoveHandler ConsideringMove;

        protected void RaiseConsideringMoveEvent(ChessMove move)
        {
            if (ConsideringMove != null)
                ConsideringMove(null, new ConsideringMoveEventArgs(move));
        }

        public delegate void DisplayHintHandler(object sender, DisplayHintEventArgs e);

        public event DisplayHintHandler DisplayHint;

        protected void RaiseDisplayHintEvent(ChessMove move)
        {
            if (DisplayHint != null)
                DisplayHint(null, new DisplayHintEventArgs(move));
        }

        private int depthToVisualize = 0;

        private int gameTicks = 0;

        public int GameTicks { get { return gameTicks; } }

        private static OpeningBook openingBook = new OpeningBook();

        public static OpeningBook OpeningBook { get { return openingBook; } }

        private const int INFINITY = 1000000;

#if !NETFX_CORE
        private Thread thinkThread = null;
        private Thread calculateHintThread = null;
#endif

        private bool stop = false;
        private object stopLock = new object();

        private bool aborted = false;
        private object abortedLock = new object();

#if PROF
        private Profiler prof = null;
#endif

        [XmlIgnore]
        public bool Stop
        {
            get
            {
                lock (stopLock)
                {
                    return stop;
                }
            }
            set
            {
                lock (stopLock)
                {
                    stop = value;
                }
            }
        }

        [XmlIgnore]
        public bool Aborted
        {
            get
            {
                lock (abortedLock)
                {
                    return aborted;
                }
            }
            set
            {
                lock (abortedLock)
                {
                    aborted = value;
                }
            }
        }

        public Level DifficultyLevel { get; set; }

        public int TimeLimit { get; set; }      // Limit of time to think, in milliseconds.
        private int DepthLimit { get; set; }

        public ChessComputer(ChessGame game, Level difficultyLevel)
            : base(game)
        {
            this.IsComputer = true;

            this.gameTicks = 0;

            this.DifficultyLevel = difficultyLevel;
            switch (difficultyLevel)
            {
                case Level.Easy:
                    DepthLimit = 2;
                    TimeLimit = 0;
                    break;
                case Level.Medium:
                    DepthLimit = 0;
                    TimeLimit = 3000;
                    break;
                case Level.Hard:
                    DepthLimit = 0;
                    TimeLimit = 12000;
                    break;
            }

#if PROF
            prof = new Profiler("ChessComputer");
#endif
        }

        public override void Think()
        {
#if NETFX_CORE
            System.Threading.Tasks.Task.Run(() => 
            { 
                ThinkAsync();
            });
#else
            thinkThread = new Thread(new ThreadStart(ThinkAsync));
            thinkThread.Start();
#endif
        }

        public void CalculateHint()
        {
#if NETFX_CORE
            System.Threading.Tasks.Task.Run(() => 
            { 
                CalculateHintAsync();
            });
#else
            calculateHintThread = new Thread(new ThreadStart(CalculateHintAsync));
            calculateHintThread.Start();
#endif
        }

        public override void StopThinking()
        {
            this.Aborted = true;
            this.Stop = true;
        }

        public override void GameOver()
        {
            System.Diagnostics.Debug.WriteLine("Game over. Game ticks: " + gameTicks);

#if PROF
            Debug.WriteLine(prof.ToString());
#endif
        }

        public void ThinkAsync()
        {
#if !NETFX_CORE
            if (Game.LastMove == null)
                Thread.Sleep(400);
#endif

            ChessMove move = GetNextMove();            
            if( !Aborted )
                Game.MakeMove(move, false);
        }

        public void CalculateHintAsync()
        {
            ChessMove move = GetNextMove();
            if (!Aborted)
                RaiseDisplayHintEvent(move);
        }

        private ChessMove GetNextMove()
        {
            int startTicks = (int)DateTime.Now.Ticks;

            ChessMove move = null;

            Progress = 0;
            Aborted = false;
            Stop = false;

            // Try opening book first.
            move = openingBook.GetNextMove(Game.ChessMoves);
            if (move != null && Game.MakeMove(move, true))
            {
#if !NETFX_CORE
                Thread.Sleep(200);
#endif
            }
            else
            {
#if LOGGING
                Debug.WriteLine(string.Format("Calc start"));
#endif

#if NETFX_CORE
                MyTimer timer = null;
                if (TimeLimit > 0)
                {
                    timer = new MyTimer(OutOfTime, null, TimeLimit, System.Threading.Timeout.Infinite);
                    Debug.WriteLine("Timer " + TimeLimit);
                }
#else
                Timer timer = null;
                if (TimeLimit > 0)
                {
                    timer = new Timer(OutOfTime, null, TimeLimit, System.Threading.Timeout.Infinite);
                    Debug.WriteLine("Timer " + TimeLimit);
                }
#endif

                List<ChessMove> possibleMoves = ChessGame.GetPossibleMoves(Game.Board, Game.Turn);
                // Iterative deepening.
                int depth = 0;
                while (true)
                {
                    ChessMove bestMove = null;

                    Game.AddLog(string.Format("lvl {0} start depth {1}", (int)DifficultyLevel, depth));

                    int calcStartTicks = (int)DateTime.Now.Ticks;
                    CalculateMove(Game.Board, Game.Turn, possibleMoves, depth, true, -INFINITY, INFINITY, out bestMove, depth);

                    int calcTimeTicks = (int)DateTime.Now.Ticks - calcStartTicks;
                    Game.AddLog(string.Format("end depth {0} ticks {1} bestMove {2}", depth, calcTimeTicks, (bestMove == null ? 0 :1)));

                    Debug.WriteLine(string.Format("dTV: {0} time: {1}", depthToVisualize, calcTimeTicks / TimeSpan.TicksPerSecond));
                    if( depthToVisualize == 0 && ((float)calcTimeTicks / (float)TimeSpan.TicksPerSecond) > 0.3f )
                        depthToVisualize = depth;

                    if (bestMove != null)
                        move = bestMove;

                    if( bestMove == null || DepthLimit > 0 && depth >= DepthLimit )
                    {
                        if (timer != null)
                            timer.Dispose();
                        break;
                    }
                    else
                    {
                        // Move the move to the beginning of the list.
                        if (possibleMoves.Remove(move))
                            possibleMoves.Insert(0, move);
                    }
                    depth++;
                }

#if LOGGING
                Debug.WriteLine(string.Format("Calc end."));
#endif
            }

            gameTicks += (int)DateTime.Now.Ticks - startTicks;

            return move;
        }

        private void OutOfTime(object state)
        {
            Stop = true;
        }

        private int CompareMoves(ChessMove a, ChessMove b)
        {
            int res = 0;
            if (a.FromIndex > b.FromIndex)
                res = -1;
            else if (a.FromIndex < b.FromIndex)
                res = 1;
            return res;
        }

        /// <summary>
        /// Recursive method to find the best move in a given position. Min-max algorithm alternates
        /// between black and white.
        /// </summary>
        /// <param name="board"></param>
        /// <param name="turn"></param>
        /// <param name="possibleMoves"></param>
        /// <param name="level">Number of half moves to look forward.</param>
        /// <param name="topLevel">True if this is the outermost call.</param>
        /// <param name="alpha"></param>
        /// <param name="beta"></param>
        /// <param name="bestMove"></param>
        /// <returns></returns>
        private int CalculateMove(ChessBoard board, ChessPieceColor turn, List<ChessMove> possibleMoves, int level, bool topLevel,
            int alpha, int beta, out ChessMove bestMove, int initialDepth)
        {
            int best_score = -INFINITY;
            bestMove = null;

            if (!Stop)
            {
                int i = 0;
                foreach (ChessMove possibleMove in possibleMoves)
                {
                    if (topLevel && ((depthToVisualize != 0 && initialDepth >= depthToVisualize) || (depthToVisualize == 0 && initialDepth > 2)))
                    {
                        RaiseConsideringMoveEvent(possibleMove);
                    }

                    i++;

                    ChessBoard resBoard = new ChessBoard(board);
                    resBoard.ApplyMove(turn, possibleMove, null); 

                    int move_score = 0;
                    if (level == 0)
                    {
#if PROF
                        prof.Start("StaticEval");
#endif
                        move_score = StaticEvaluation(resBoard, turn);
#if PROF
                        prof.End("StaticEval");
#endif
                    }
                    else
                    {
                        ChessMove m = null;
                        ChessPieceColor newTurn = ChessGame.InvertColor(turn);
                        List<ChessMove> moves = ChessGame.GetPossibleMoves(resBoard, newTurn);
                        bool abort = false;

                        if (moves.Count == 0)
                        {
                            if (!ChessGame.IsInCheck(board, newTurn))
                            {
                                // Stale mate, good depending on piece value.
                                int whiteValue = 0;
                                int blackValue = 0;
                                board.GetPiecesValue(out whiteValue, out blackValue);

                                int delta = (turn == ChessPieceColor.White ? whiteValue - blackValue : blackValue - whiteValue);
                                if (delta > 0)
                                    move_score = -10000;
                                else
                                    move_score = 10000;
                            }
                            else
                            {
                                int checkMateScore = 50000 + level * 100;   // Prioritize early check mate.

                                move_score = checkMateScore;  // Check mate.
                            }
                        }
                        else
                        {
                            move_score = -CalculateMove(resBoard, newTurn, moves, level - 1, false, -beta, -alpha, out m, initialDepth);
                            if (m == null)  // Abort
                                abort = true;
                        }

                        if (abort)  // Abort
                        {
                            bestMove = null;
                            best_score = -INFINITY;
                            break;
                        }
                    }

                    if (topLevel)
                    {
                        // Decrease score for repetition moves.
                        if (Game.ChessMoves.Count >= 3)
                        {
                            ChessMove myLastMove = Game.ChessMoves[Game.ChessMoves.Count - 2];
                            ChessMove oppLastMove = Game.ChessMoves[Game.ChessMoves.Count - 1];
                            ChessMove oppSecLastMove = Game.ChessMoves[Game.ChessMoves.Count - 3];

                            if (myLastMove.FromIndex == possibleMove.ToIndex
                                && myLastMove.ToIndex == possibleMove.FromIndex
                                && !myLastMove.IsCapture
                                && oppLastMove.FromIndex == oppSecLastMove.ToIndex
                                && oppLastMove.ToIndex == oppSecLastMove.FromIndex
                                && !oppLastMove.IsCapture && !oppSecLastMove.IsCapture) // TODO: check for IsCapture in possibleMove
                                move_score -= 30;
                        }
                    }

#if LOGGING
                    Debug.WriteLine(string.Format("{0}Level {1} {2} {3} alpha {4} beta {5}", GetIndent(level), level, possibleMove.ToString(), move_score, alpha, beta));
#endif
                    if (move_score > best_score)
                    {
                        best_score = move_score;
                        bestMove = possibleMove;
                    }

                    if (best_score > alpha)
                        alpha = best_score;

                    if (alpha >= beta)
                        return alpha;   // This means the move is as good as or worse than a previous move.
                }
            }

            return best_score;
        }

        private string GetIndent(int level)
        {
            string res = "";
            for (int i = 3 - level; i > 0; i--)
                res += "\t";
            return res;
        }

        private int StaticEvaluation(ChessBoard boardCopy, ChessPieceColor turn)
        {
            int whiteValue = 0;
            int blackValue = 0;
            bool endGame = false;

#if PROF
prof.Start("StaticEval1");
#endif
            boardCopy.GetPiecesValue(out whiteValue, out blackValue);
#if PROF
prof.End("StaticEval1");
#endif
            if (whiteValue < 30
                || blackValue < 30)
                endGame = true;

            whiteValue *= 100;
            blackValue *= 100;

            if (!endGame)
            {
                // White king check
#if PROF
                prof.Start("StaticEval2");
#endif

                if ((boardCopy.whiteBoardState.King &
                    (Precomputed.IndexToBitBoard[(int)ChessPositionIndex.G1]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.H1]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.A1]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.B1]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.C1])) != 0)
                    whiteValue += 40;
                else if ((boardCopy.whiteBoardState.King &
                    ~(Precomputed.IndexToBitBoard[(int)ChessPositionIndex.E1])) != 0)
                    whiteValue -= 40;

                if ((boardCopy.whiteBoardState.King &
                    ~(Precomputed.Rows[0])) != 0)
                {
                    whiteValue -= 20;   // King is on second or more row.

                    if ((boardCopy.whiteBoardState.King &
                        ~(Precomputed.Rows[1])) != 0)
                    {
                        whiteValue -= 200;   // King is on third or more row.
                    }
                }

                // Black king check
                if ((boardCopy.blackBoardState.King &
                    (Precomputed.IndexToBitBoard[(int)ChessPositionIndex.G8]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.H8]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.A8]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.B8]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.C8])) != 0)
                    blackValue += 40;
                else if ((boardCopy.blackBoardState.King &
                    ~(Precomputed.IndexToBitBoard[(int)ChessPositionIndex.E8])) != 0)
                    blackValue -= 40;

                if ((boardCopy.blackBoardState.King &
                    ~(Precomputed.Rows[7])) != 0)
                {
                    blackValue -= 20;   // King is on second or more row.

                    if ((boardCopy.blackBoardState.King &
                        ~(Precomputed.Rows[6])) != 0)
                    {
                        blackValue -= 200;   // King is on third or more row.
                    }
                }

#if PROF
                prof.End("StaticEval2");
#endif

#if PROF
                prof.Start("StaticEval3");
#endif
                // White queen check
                if ((boardCopy.whiteBoardState.Queens &
                    ~(Precomputed.IndexToBitBoard[(int)ChessPositionIndex.D1])) != 0)
                    whiteValue -= 60;

                // Black queen check
                if ((boardCopy.blackBoardState.Queens &
                    ~(Precomputed.IndexToBitBoard[(int)ChessPositionIndex.D8])) != 0)
                    blackValue -= 60;
#if PROF
                prof.End("StaticEval3");
#endif


                // White knight check
#if PROF
                prof.Start("StaticEval4");
#endif
                whiteValue += 40 * Util.getBitCountFromBitboard((boardCopy.whiteBoardState.Knights & (Precomputed.IndexToBitBoard[(int)ChessPositionIndex.C3]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.D3]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.E3]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.F3]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.C4]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.D4]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.E4]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.F4])));
                whiteValue -= 40 * Util.getBitCountFromBitboard((boardCopy.whiteBoardState.Knights & Precomputed.Rows[0]));

                // Black knight check
                blackValue += 40 * Util.getBitCountFromBitboard((boardCopy.blackBoardState.Knights & (Precomputed.IndexToBitBoard[(int)ChessPositionIndex.C6]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.D6]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.E6]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.F6]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.C5]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.D5]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.E5]
                    | Precomputed.IndexToBitBoard[(int)ChessPositionIndex.F5])));
                blackValue -= 40 * Util.getBitCountFromBitboard((boardCopy.blackBoardState.Knights & Precomputed.Rows[7]));
#if PROF
                prof.End("StaticEval4");
#endif

                // White bishop check
#if PROF
                prof.Start("StaticEval5");
#endif
                whiteValue += 40 * Util.getBitCountFromBitboard((boardCopy.whiteBoardState.Bishops & Precomputed.Rows[3]));
                whiteValue += 30 * Util.getBitCountFromBitboard((boardCopy.whiteBoardState.Bishops & ~Precomputed.Rows[0] & ~Precomputed.Rows[3]));
                whiteValue -= 40 * Util.getBitCountFromBitboard((boardCopy.whiteBoardState.Bishops & Precomputed.Rows[0]));

                // Black bishop check
                blackValue += 40 * Util.getBitCountFromBitboard((boardCopy.blackBoardState.Bishops & Precomputed.Rows[4]));
                blackValue += 30 * Util.getBitCountFromBitboard((boardCopy.blackBoardState.Bishops & ~Precomputed.Rows[7] & ~Precomputed.Rows[4]));
                blackValue -= 40 * Util.getBitCountFromBitboard((boardCopy.blackBoardState.Bishops & Precomputed.Rows[7]));
#if PROF
                prof.End("StaticEval5");
#endif

                // White rook check
#if PROF
                prof.Start("StaticEval6");
#endif
                whiteValue += 40 * Util.getBitCountFromBitboard((boardCopy.whiteBoardState.Rooks & Precomputed.Rows[0]));
                whiteValue -= 40 * Util.getBitCountFromBitboard((boardCopy.whiteBoardState.Rooks & ~Precomputed.Rows[0]));

                // Black rook check
                blackValue += 40 * Util.getBitCountFromBitboard((boardCopy.blackBoardState.Rooks & Precomputed.Rows[7]));
                blackValue -= 40 * Util.getBitCountFromBitboard((boardCopy.blackBoardState.Rooks & ~Precomputed.Rows[7]));
#if PROF
                prof.End("StaticEval6");
#endif
            }

            int delta = (turn == ChessPieceColor.White ? whiteValue - blackValue : blackValue - whiteValue);
            return delta;
        }
    }
}
