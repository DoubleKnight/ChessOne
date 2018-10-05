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
using System.Xml;
using System.IO;
using System.Xml.Linq;

namespace ChessOneLib
{
    class OpeningNode
    {
        private OpeningNode parent = null;

        private List<ChessMove> moves = new List<ChessMove>();
        private List<OpeningNode> nodes = new List<OpeningNode>();

        public List<ChessMove> Moves { get { return moves; } }

        public OpeningNode Parent { get { return parent; } }

        public OpeningNode(OpeningNode parent)
        {
            this.parent = parent;
        }

        public void AddMove(ChessMove move, OpeningNode node)
        {
            moves.Add(move);
            nodes.Add(node);
        }

        public OpeningNode FindMove(ChessMove move)
        {
            OpeningNode res = null;
            for (int i = 0; i < moves.Count; i++)
            {
                if (moves[i].FromIndex == move.FromIndex
                    && moves[i].ToIndex == move.ToIndex
                    && moves[i].PromotionPieceType == move.PromotionPieceType)
                {
                    res = nodes[i];
                    break;
                }
            }
            return res;
        }
    }

    public class OpeningBook
    {
        private OpeningNode topNode = new OpeningNode(null);

#if DEBUG
        private Random random = new Random(2);  // Hardcoded for debugging.
#else
        private Random random = new Random((int)DateTime.Now.Ticks);
#endif

        public OpeningBook()
        {
        }

        public void Load(XmlReader xmlReader)
        {
            xmlReader.ReadStartElement("moves");
            AddMove(topNode, xmlReader);
            xmlReader.ReadEndElement();
        }

        private void AddMove(OpeningNode parentNode, XmlReader reader)
        {
            while (reader.IsStartElement("move"))
            {
                string moveString = reader.GetAttribute("id");
                reader.ReadStartElement("move");

                ChessMove move = MoveFromString(moveString);
                OpeningNode node = new OpeningNode(parentNode);

                parentNode.AddMove(move, node);

                AddMove(node, reader);

                reader.ReadEndElement();
            }
        }

        private ChessMove MoveFromString(string moveString)
        {
            int fromX = (int)moveString[0] - (int)'a';
            int fromY = int.Parse(moveString[1].ToString()) - 1;
            int toX = (int)moveString[2] - (int)'a';
            int toY = int.Parse(moveString[3].ToString()) - 1;

            // TODO: PieceType.
            return new ChessMove((ChessPositionIndex)fromX + 8 * fromY, (ChessPositionIndex)toX + 8 * toY, ChessPieceType.Pawn);
        }

        public ChessMove GetNextMove(List<ChessMove> gameMoves)
        {
            OpeningNode node = topNode;
            ChessMove res = null;

            for (int i = 0; i < gameMoves.Count && node != null; i++)
            {
                ChessMove gameMove = gameMoves[i];
                node = node.FindMove(gameMove);
                if (node == null)
                    break;
            }

            if (node != null && node.Moves.Count > 0)
            {
                int index = random.Next(node.Moves.Count);
                res = node.Moves[index];
            }

            return res;
        }
    }
}
