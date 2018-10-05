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
#if false
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
#endif

namespace ChessOneLib
{
    /// <summary>
    /// </summary>
    public class Profiler
    {
        private class TimerInfo
        {
            public long TotalTime { get; set; }
            public long StartTicks { get; set; }
        }

        private string name;
        private Dictionary<string, TimerInfo> timers = new Dictionary<string, TimerInfo>();

        public Profiler(string name)
        {
            this.name = name;
        }

        public void Start(string timerName)
        {
            TimerInfo timerInfo = null;
            if (timers.ContainsKey(timerName))
                timerInfo = timers[timerName];
            else
            {
                timerInfo = new TimerInfo();
                timers.Add(timerName, timerInfo);
            }

            timerInfo.StartTicks = DateTime.Now.Ticks;
            // Get ticks as late as possible in the method.
        }

        public void End(string timerName)
        {
            long ticks = DateTime.Now.Ticks;    // Get ticks as early as possible in the method.
            TimerInfo timerInfo = timers[timerName];
            timerInfo.TotalTime += ticks - timerInfo.StartTicks;
        }

        public override string ToString()
        {
            string res = "Profiler " + name + "\r\n";
            foreach (string timerName in timers.Keys)
            {
                TimerInfo timerInfo = timers[timerName];
                res += string.Format("{0}\t{1}\r\n", timerName, timerInfo.TotalTime);
            }
            return res;
        }
    }
}
