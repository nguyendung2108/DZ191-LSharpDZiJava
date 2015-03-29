using System;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;

namespace iYasuo
{
    class ComboManager
    {
        public delegate void OnAction();
        /// <summary>
        /// 
        /// WIP
        /// Gets the spell to cast based on the action
        /// 
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static OnAction getAction(ComboAction mode)
        {
            switch (mode)
            {
                case ComboAction.E:
                    return CastE;
            }
            return null;
        }

        public static void CastE()
        {
            //TODO Cast E Lmao
        }
    }

    internal class Combo
    {
        public static List<ComboAction> _combo { get; set; }
        public static  int currentActionIndex { get; set; }

        public static int nextActionIndex
        {
            get
            {
                return ((currentActionIndex + 1)<=_combo.Count)?(currentActionIndex+1):(_combo.Count);
            }
        }
        public static int previoudActionIndex { 
            get
            {
                return (currentActionIndex - 1) >= 0 ? (currentActionIndex - 1) : 0;
            } 
        }
    }
    enum ComboAction
    {
        E,Q,EQ,R
    }
}
