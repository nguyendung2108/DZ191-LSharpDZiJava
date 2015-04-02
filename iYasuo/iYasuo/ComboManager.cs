// This file is part of LeagueSharp.Common.
// 
// LeagueSharp.Common is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// LeagueSharp.Common is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with LeagueSharp.Common.  If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Generic;

namespace iYasuo
{
    internal class ComboManager
    {
        public delegate void OnAction();

        /// <summary>
        ///     WIP
        ///     Gets the spell to cast based on the action
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static OnAction GetAction(ComboAction mode)
        {
            switch (mode)
            {
                case ComboAction.Q:
                    return CastQ;
                case ComboAction.E:
                    return CastE;
            }
            return null;
        }

        public static void CastQ() {}

        public static void CastE()
        {
            //TODO Cast E Lmao
        }
    }

    internal class Combo
    {
        public static List<ComboAction> _combo { get; set; }
        public static int CurrentActionIndex { get; set; }

        public static int NextActionIndex
        {
            get { return ((CurrentActionIndex + 1) <= _combo.Count) ? (CurrentActionIndex + 1) : (_combo.Count); }
        }

        public static int PrevioudActionIndex
        {
            get { return (CurrentActionIndex - 1) >= 0 ? (CurrentActionIndex - 1) : 0; }
        }
    }

    internal enum ComboAction
    {
        E,
        Q,
        EQ,
        R
    }
}