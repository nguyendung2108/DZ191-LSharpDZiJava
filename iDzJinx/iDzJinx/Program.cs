using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp.Common;

namespace iDzJinx
{
    static class Program
    {
        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Jinx.OnGameLoad;
        }
    }
}
