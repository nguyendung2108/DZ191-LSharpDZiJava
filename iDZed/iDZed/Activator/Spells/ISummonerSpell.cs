using System;
using iDZed.Activator.Spells;
using LeagueSharp.Common;

namespace iDzed.Activator.Spells
{
    interface ISummonerSpell
    {
        void OnLoad();
        String GetDisplayName();
        void AddToMenu(Menu menu);
        bool RunCondition();
        void Execute();
        SummonerSpell GetSummonerSpell();
        String GetName();
    }
}
