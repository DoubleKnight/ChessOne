using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ChessLib
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
