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
using LeagueSharp;
using LeagueSharp.Common;

namespace iDzLucian
{
    internal class Program
    {
        private static Obj_AI_Hero _player;
        private static Spell _q, _qExtended, _w, _e, _r;
        private static Menu _menu;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad(EventArgs args)
        {
            _player = ObjectManager.Player;

            if (_player.ChampionName != "Lucian")
            {
                return;
            }

            LoadSpells();
            CreateMenu();

            Game.OnGameUpdate += OnGameUpdate;
        }

        private static void LoadSpells()
        {
            _q = new Spell(SpellSlot.Q, 675);
            _q.SetTargetted(0.25f, float.MaxValue);

            _qExtended = new Spell(SpellSlot.Q, 1100);
            _qExtended.SetSkillshot(0.25f, 5f, float.MaxValue, true, SkillshotType.SkillshotLine);

            _w = new Spell(SpellSlot.W, 1000);
            _w.SetSkillshot(0.3f, 80, 1600, true, SkillshotType.SkillshotLine);

            _e = new Spell(SpellSlot.E, 425);
            _e.SetSkillshot(.25f, 1f, float.MaxValue, false, SkillshotType.SkillshotLine);

            _r = new Spell(SpellSlot.R, 1400);
            _r.SetSkillshot(.1f, 110, 2800, true, SkillshotType.SkillshotLine);
        }

        private static void CreateMenu()
        {
            _menu = new Menu("iDzLucian", "iDzLucian", true);

            var comboMenu = new Menu("Combo Options", "com.iDzLucian.combo");
            {
                //TODO
            }

            _menu.AddToMainMenu();
        }
        private static void OnGameUpdate(EventArgs args) {}
    }
}