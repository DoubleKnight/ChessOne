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
using System.Xml.Serialization;

namespace ChessOneLib
{
    public class Player
    {
        public ChessGame Game { get; set; }

        private string Name { get; set; }
        private string Email { get; set; }

        public bool IsComputer { get; set; }

        private int progress = 0;
        private object progressLock = new object();

        public Player(ChessGame game)
        {
            this.Game = game;
            IsComputer = false;
        }

        public virtual void Think()
        {
        }

        public virtual void StopThinking()
        {
        }

        public virtual void GameOver()
        {
        }

        /// <summary>
        /// A value between 0 and 100 that indicates how far the thinking is done.
        /// </summary>
        [XmlIgnore]
        public int Progress
        {
            get
            {
                lock (progressLock)
                {
                    return progress;
                }
            }
            set
            {
                lock (progressLock)
                {
                    progress = value;
                }
            }
        }
    }
}
