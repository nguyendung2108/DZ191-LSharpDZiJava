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
using iDzKatarina.helpers;
using LeagueSharp;
using LeagueSharp.Common;

namespace iDzKatarina
{
    internal static class Katarina
    {
        private static Obj_AI_Hero _player;
        private static Orbwalking.Orbwalker _orbwalker;
        public static Menu Menu;

        public static void OnGameLoaded(EventArgs args)
        {
            _player = ObjectManager.Player;
            if (_player.ChampionName != "Katarina")
            {
                return;
            }

            LoadSpells();
            CreateMenu();

            Game.OnUpdate += OnGameUpdate;
        }

        private static void LoadSpells() {}

        private static void CreateMenu()
        {
            Menu = new Menu("iDzKatarina", "com.idzkatarina", true);

            Menu tsMenu = new Menu("Katarina - Target Selector", "com.idzkatarina.ts");
            TargetSelector.AddToMenu(tsMenu);
            Menu.AddSubMenu(tsMenu);

            Menu orbMenu = new Menu("Katarina - Orbwalker", "com.idzkatarina.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbMenu);
            Menu.AddSubMenu(orbMenu);

            Menu comboMenu = new Menu("Katarina - Combo", "com.idzkatarina.combo");
            comboMenu.AddModeMenu(
                Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R },
                new[] { true, true, true, true });
            Menu.AddSubMenu(comboMenu);

            Menu harassMenu = new Menu("Katarina - Harass", "com.idzkatarina.harass");
            harassMenu.AddModeMenu(
                Mode.Harass, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E }, new[] { true, false, false });
            Menu.AddSubMenu(harassMenu);

            Menu skillOptions = new Menu("Katarina - Skill Options", "com.idzkatarina.skilloptions");
            skillOptions.AddItem(new MenuItem("procQ", "Always try to proc Q").SetValue(false));
            Menu.AddSubMenu(skillOptions);

            Menu.AddToMainMenu();
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (_player.IsDead)
            {
                return;
            }

            switch (_orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:

                    break;
            }
        }
    }
}