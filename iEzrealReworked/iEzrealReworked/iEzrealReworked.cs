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
using System.Collections.Generic;
using System.Linq;
using iEzrealReworked.helpers;
using LeagueSharp;
using LeagueSharp.Common;

namespace iEzrealReworked
{
    // ReSharper disable once InconsistentNaming
    internal class iEzrealReworked
    {
        //The menu instance
        public static Menu Menu;
        //The common orbwalker
        private Orbwalking.Orbwalker _orbwalker;
        //The player
        private Obj_AI_Hero _player;
        //The Spell Values for Q, W, E and R
        private readonly Dictionary<SpellSlot, Spell> _spells = new Dictionary<SpellSlot, Spell>
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q, 1150) },
            { SpellSlot.W, new Spell(SpellSlot.W, 1000) },
            { SpellSlot.E, new Spell(SpellSlot.E, 475) },
            { SpellSlot.R, new Spell(SpellSlot.R, 20000) }
        };

        #region calculations

        /// <summary>
        ///     Gets the real trueshot barrage damage taking into account minions and champions inline with Ultimate width
        /// </summary>
        /// <param name="target"></param>
        /// <returns>if the player can kill the target with ult...</returns>
        private bool CanExecuteTarget(Obj_AI_Hero target)
        {
            double damage = 0;

            var prediction = _spells[SpellSlot.R].GetPrediction(target);
            var count = prediction.CollisionObjects.Count;

            if (count >= 7)
            {
                damage = _player.GetSpellDamage(target, SpellSlot.R) * .3;
            }
            else if (count != 0)
            {
                damage = _player.GetSpellDamage(target, SpellSlot.R) * (10 - count / 10);
            }

            return damage > target.Health + target.HPRegenRate * 3 + 25;
        }

        #endregion

        #region Events

        /// <summary>
        ///     The onload function load your spells and other shit here before game starts.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        public void OnLoad(EventArgs args)
        {
            //Initialize our player
            _player = ObjectManager.Player;

            //If the champions name is not ezreal then don't load the assembly
            if (_player.ChampionName != "Ezreal")
            {
                return;
            }

            //Load the spell values
            LoadSpells();
            //Set the menu and create the sub menus etc etc
            CreateMenu();

            //Event Subscribers
            Game.OnGameUpdate += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
        }

        /// <summary>
        ///     Performs the update task
        /// </summary>
        /// <param name="args">The event arguments.</param>
        private void OnGameUpdate(EventArgs args)
        {
            if (_player.IsDead)
            {
                return;
            }

            switch (_orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    OnCombo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    OnHarass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    OnFarm();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    OnFarm();
                    break;
            }
        }

        /// <summary>
        ///     Draw the spell ranges and whatever other shit you feel like drawing.
        /// </summary>
        /// <param name="args"></param>
        private void OnDraw(EventArgs args)
        {
            foreach (KeyValuePair<SpellSlot, Spell> spell in
                _spells.Where(
                    spell =>
                        MenuHelper.IsMenuEnabled(
                            "com.iezreal.drawing.draw" + MenuHelper.GetStringFromSpellSlot(spell.Key))))
            {
                Render.Circle.DrawCircle(
                    _player.Position, spell.Value.Range,
                    MenuHelper.GetCicleColour("com.iezreal.drawing.draw" + MenuHelper.GetStringFromSpellSlot(spell.Key)));
            }
        }

        #endregion

        #region ActiveModes

        /// <summary>
        ///     Performs the combo sequence
        /// </summary>
        private void OnCombo()
        {
            var target = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range, TargetSelector.DamageType.Physical);
            CastMysticShot(target, Mode.Combo);
            CastEssenceFlux(target, Mode.Combo);
            CastTrueshotBarrage(target);
        }

        /// <summary>
        ///     Performs the harass sequence
        /// </summary>
        private void OnHarass()
        {
            var target = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range, TargetSelector.DamageType.Physical);
            CastMysticShot(target, Mode.Harass);
            CastEssenceFlux(target, Mode.Harass);
        }

        private void OnFarm()
        {
            var allMinions = MinionManager.GetMinions(_player.ServerPosition, _spells[SpellSlot.Q].Range);
            switch (_orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Obj_AI_Base qMinion = allMinions.FirstOrDefault(min => min.IsValidTarget(_spells[SpellSlot.Q].Range));
                    if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Laneclear) && _spells[SpellSlot.Q].CanCast(qMinion))
                    {
                        _spells[SpellSlot.Q].Cast(qMinion);
                    }
                    //TODO r laneclear with amt of minions...

                    break;
                case Orbwalking.OrbwalkingMode.LastHit:

                    break;
            }
        }

        #endregion

        #region Menu and spells

        /// <summary>
        ///     Creats the menu for the specified champion using Asuna's MenuHelper Class
        /// </summary>
        private void CreateMenu()
        {
            Menu = new Menu("iEzreal Reworked", "com.iezreal", true);

            var orbMenu = new Menu("Ezreal - Orbwalker", "com.iezreal.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbMenu);
            Menu.AddSubMenu(orbMenu);

            var tsMenu = new Menu("Ezreal - Target Selector", "com.iezreal.ts");
            TargetSelector.AddToMenu(tsMenu);
            Menu.AddSubMenu(tsMenu);

            var comboMenu = new Menu("Ezreal - Combo", "com.iezreal.combo");
            comboMenu.AddModeMenu(
                Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.R }, new[] { true, true, true });
            comboMenu.AddManaManager(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.R }, new[] { 35, 35, 10 });
            Menu.AddSubMenu(comboMenu);

            var harassMenu = new Menu("Ezreal - Harass", "com.iezreal.harass");
            harassMenu.AddModeMenu(Mode.Harass, new[] { SpellSlot.Q, SpellSlot.W }, new[] { true, false });
            harassMenu.AddManaManager(Mode.Harass, new[] { SpellSlot.Q, SpellSlot.W }, new[] { 35, 20 });
            Menu.AddSubMenu(harassMenu);

            var farmMenu = new Menu("Ezreal - Farm", "com.iezreal.farm");
            farmMenu.AddModeMenu(Mode.Laneclear, new[] { SpellSlot.Q, SpellSlot.R }, new[] { true, false });
            farmMenu.AddManaManager(Mode.Laneclear, new[] { SpellSlot.Q, SpellSlot.R }, new[] { 35, 35 });
            farmMenu.AddSlider("", 0, 0, 0);
            Menu.AddSubMenu(farmMenu);

            var miscMenu = new Menu("Ezreal - Misc", "com.iezreal.misc");
            miscMenu.AddHitChanceSelector();
            miscMenu.AddItem(new MenuItem("com.iezreal.misc.debug", "Debug").SetValue(false));
            Menu.AddSubMenu(miscMenu);

            var drawMenu = new Menu("Ezreal - Draw", "com.iezreal.drawing");
            drawMenu.AddDrawMenu(_spells, System.Drawing.Color.DarkRed);
            Menu.AddSubMenu(drawMenu);

            Menu.AddToMainMenu();
        }

        /// <summary>
        ///     Sets the spells skillshot values if needed
        /// </summary>
        private void LoadSpells()
        {
            _spells[SpellSlot.Q].SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);
            _spells[SpellSlot.W].SetSkillshot(0.25f, 80f, 2000f, false, SkillshotType.SkillshotLine);
            _spells[SpellSlot.R].SetSkillshot(1f, 160f, 2000f, false, SkillshotType.SkillshotLine);
        }

        #endregion

        #region spell casting

        /// <summary>
        ///     Casts Ezreal's Mystic Shot
        /// </summary>
        /// <param name="target">the target to cast the spell at</param>
        /// <param name="mode">the mode the player is currently using</param>
        private void CastMysticShot(Obj_AI_Base target, Mode mode)
        {
            if (target.IsValidTarget(_spells[SpellSlot.Q].Range))
            {
                if (_spells[SpellSlot.Q].IsEnabledAndReady(mode) && _spells[SpellSlot.Q].CanCast(target))
                {
                    _spells[SpellSlot.Q].CastIfHitchanceEquals(target, MenuHelper.GetHitchance());
                }
            }
        }

        /// <summary>
        ///     Casts Ezreal's Essence Flux
        /// </summary>
        /// <param name="target">the target to cast the spell at</param>
        /// <param name="mode">the mode the player is currently using</param>
        private void CastEssenceFlux(Obj_AI_Base target, Mode mode)
        {
            if (target.IsValidTarget(_spells[SpellSlot.W].Range))
            {
                if (_spells[SpellSlot.W].IsEnabledAndReady(mode) && _spells[SpellSlot.W].CanCast(target))
                {
                    _spells[SpellSlot.W].CastIfHitchanceEquals(target, MenuHelper.GetHitchance());
                }
            }
        }

        /// <summary>
        ///     Casts Ezreal's Trueshot Barrage takes into account minion and champion collision for damage reduction
        /// </summary>
        /// <param name="target">the target to cast the spell at</param>
        private void CastTrueshotBarrage(Obj_AI_Hero target)
        {
            if (target.IsValidTarget(_spells[SpellSlot.R].Range))
            {
                if (_spells[SpellSlot.R].IsEnabledAndReady(Mode.Combo) && _spells[SpellSlot.R].CanCast(target))
                {
                    if (CanExecuteTarget(target))
                    {
                        _spells[SpellSlot.R].CastIfHitchanceEquals(target, MenuHelper.GetHitchance());
                    }
                    else
                    {
                        Game.PrintChat(string.Format("{0} is not killable..", target.ChampionName));
                    }
                }
            }
        }

        #endregion
    }
}