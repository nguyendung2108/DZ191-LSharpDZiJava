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

using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace AntiFUCKINGRengar
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class Program
    {
        private static Menu _menu;
        private static Obj_AI_Hero _player;
        private static Spell _gapcloseSpell;
        private static Obj_AI_Hero _rengarObj;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnLoad;
        }

        private static void OnLoad(EventArgs args)
        {
            _player = ObjectManager.Player;
            if (!IsCompatibleChampion())
                return;

            _menu = new Menu("Anti Fucking Rengar", "antirengo", true);
            _menu.AddItem(new MenuItem("enabled", "ENABLE RENGO BUTT FUCKING").SetValue(true));
            _menu.AddToMainMenu();

            _gapcloseSpell = GetSpell();

            Game.PrintChat("Cause Sometimes RENGO IS 2 STRONK.");

            GameObject.OnCreate += OnCreateObject;
        }

        private static void OnCreateObject(GameObject sender, EventArgs args)
        {
            if (sender.Name == "Rengar_LeapSound.troy" && sender.IsEnemy)
            {
                foreach (Obj_AI_Hero enemy in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(hero => hero.IsValidTarget(1500) && hero.ChampionName == "Rengar"))
                {
                    _rengarObj = enemy;
                }
            }
            if (_rengarObj != null && _player.Distance(_rengarObj, true) < 1000 * 1000 &&
                _menu.Item("enabled").GetValue<bool>())
            {
                DoButtFuck();
            }
        }

        private static bool IsCompatibleChampion()
        {
            return _player.ChampionName == "Vayne" || _player.ChampionName == "Tristana" ||
                   _player.ChampionName == "Draven";
        }

        private static Spell GetSpell()
        {
            switch (_player.ChampionName)
            {
                case "Vayne":
                    return new Spell(SpellSlot.E, 550);
                case "Tristana":
                    return new Spell(SpellSlot.R, 550);
                case "Draven":
                    return new Spell(SpellSlot.E, 1100);
            }
            return null;
        }

        private static void DoButtFuck()
        {
            if (_rengarObj.ChampionName == "Rengar")
            {
                if (_rengarObj.IsValidTarget(_gapcloseSpell.Range) && _gapcloseSpell.IsReady() &&
                    _rengarObj.Distance(_player) <= _gapcloseSpell.Range)
                {
                    _gapcloseSpell.Cast(_rengarObj);
                    Game.PrintChat("Butt Fucked Rengar");
                }
            }
        }
    }
}