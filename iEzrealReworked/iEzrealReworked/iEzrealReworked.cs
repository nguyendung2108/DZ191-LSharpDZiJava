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
        //The Spell Values for Q, W, E and R
        private readonly Dictionary<SpellSlot, Spell> _spells = new Dictionary<SpellSlot, Spell>
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q, 1150) },
            { SpellSlot.W, new Spell(SpellSlot.W, 1000) },
            { SpellSlot.E, new Spell(SpellSlot.E, 475) },
            { SpellSlot.R, new Spell(SpellSlot.R, 20000) }
        };

        //The common orbwalker
        private Orbwalking.Orbwalker _orbwalker;
        //The player
        private Obj_AI_Hero _player;

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

            damage += _player.GetSpellDamage(target, SpellSlot.R);

            if (count >= 7)
            {
                damage = damage * .3;
            }
            else if (count != 0)
            {
                damage = damage * (10 - count / 10);
            }

            return damage > target.Health + 10;
        }

        #endregion

        #region Events

        /// <summary>
        ///     The OnLoad function load your spells and other shit here before game starts.
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
            Game.OnUpdate += OnGameUpdate;
            Drawing.OnDraw += delegate { DrawHelper.DrawSpellsRanges(_spells); };
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

            if (MenuHelper.IsMenuEnabled("ksEnabled"))
            {
                OnKillsteal();
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

        #endregion

        #region ActiveModes

        /// <summary>
        ///     Performs the combo sequence
        /// </summary>
        private void OnCombo()
        {
            CastMysticShot(Mode.Combo);
            CastEssenceFlux(Mode.Combo);
            CastTrueshotBarrage();
        }

        /// <summary>
        ///     Performs the harass sequence
        /// </summary>
        private void OnHarass()
        {
            CastMysticShot(Mode.Harass);
            CastEssenceFlux(Mode.Harass);
        }

        private void OnKillsteal()
        {
            var target = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range, TargetSelector.DamageType.Physical);
            if (_player.GetSpellDamage(target, SpellSlot.Q) > target.Health + 10)
            {
                CastMysticShot(Mode.Killsteal);
            }
            if (_player.GetSpellDamage(target, SpellSlot.W) > target.Health + 10)
            {
                CastEssenceFlux(Mode.Killsteal);
            }
        }

        /// <summary>
        ///     Performs the farming sequence...
        /// </summary>
        private void OnFarm()
        {
            var allMinions = MinionManager.GetMinions(_player.ServerPosition, _spells[SpellSlot.Q].Range);
            Obj_AI_Base qMinion = allMinions.FirstOrDefault(min => min.IsValidTarget(_spells[SpellSlot.Q].Range));
            var minionHealth = HealthPrediction.GetHealthPrediction(
                qMinion,
                (int) (_spells[SpellSlot.Q].Delay + (_player.Distance(qMinion) / _spells[SpellSlot.Q].Speed) * 1000));
            switch (_orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.LaneClear:

                    if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Laneclear) && _spells[SpellSlot.Q].CanCast(qMinion))
                    {
                        if (!Orbwalking.InAutoAttackRange(qMinion) &&
                            _spells[SpellSlot.Q].GetDamage(qMinion) > minionHealth)
                        {
                            _spells[SpellSlot.Q].Cast(qMinion);
                        }
                        else
                        {
                            _spells[SpellSlot.Q].Cast(qMinion);
                        }
                    }
                    var ultMinions = _spells[SpellSlot.R].GetLineFarmLocation(allMinions);
                    if (_spells[SpellSlot.R].IsEnabledAndReady(Mode.Laneclear))
                    {
                        if (ultMinions.MinionsHit >= MenuHelper.GetSliderValue("com.iezreal.farm.r.lc.minhit"))
                        {
                            _spells[SpellSlot.R].Cast(ultMinions.Position);
                        }
                    }
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Lasthit) && _spells[SpellSlot.Q].CanCast(qMinion))
                    {
                        if (qMinion != null && qMinion.Health < _player.GetSpellDamage(qMinion, SpellSlot.R))
                        {
                            if (!Orbwalking.InAutoAttackRange(qMinion) &&
                                _spells[SpellSlot.Q].GetDamage(qMinion) > minionHealth)
                            {
                                _spells[SpellSlot.Q].Cast(qMinion);
                            }
                        }
                    }
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
            Menu = new Menu("iDzEzreal", "com.iezreal", true);

            var tsMenu = new Menu("Ezreal - Target Selector", "com.iezreal.ts");
            TargetSelector.AddToMenu(tsMenu);
            Menu.AddSubMenu(tsMenu);

            var orbMenu = new Menu("Ezreal - Orbwalker", "com.iezreal.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbMenu);
            Menu.AddSubMenu(orbMenu);

            var comboMenu = new Menu("Ezreal - Combo", "com.iezreal.combo");
            comboMenu.AddModeMenu(
                Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.R }, new[] { true, true, true });
            comboMenu.AddManaManager(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.R }, new[] { 35, 35, 10 });
            Menu.AddSubMenu(comboMenu);

            var harassMenu = new Menu("Ezreal - Harass", "com.iezreal.harass");
            harassMenu.AddModeMenu(Mode.Harass, new[] { SpellSlot.Q, SpellSlot.W }, new[] { true, false });
            harassMenu.AddManaManager(Mode.Harass, new[] { SpellSlot.Q, SpellSlot.W }, new[] { 35, 20 });
            Menu.AddSubMenu(harassMenu);

            //TODO skilloptions
            var skillOptions = new Menu("Ezreal - Skill Options", "com.iezreal.skilloptions");
            skillOptions.AddItem(new MenuItem("qRange", "Min Q Range").SetValue(new Slider(900, 0, 1150)));
            skillOptions.AddItem(new MenuItem("wRange", "Min W Range").SetValue(new Slider(800, 0, 1000)));
            skillOptions.AddItem(new MenuItem("rRange", "Min R Range").SetValue(new Slider(2000, 0, 20000)));
            skillOptions.AddItem(new MenuItem("rMin", "Min Hit for R").SetValue(new Slider(3, 0, 5)));
            Menu.AddSubMenu(skillOptions);

            var farmMenu = new Menu("Ezreal - Farm", "com.iezreal.farm");
            var laneclear = new Menu("Laneclear", "com.iezreal.farm.lc");
            {
                laneclear.AddModeMenu(Mode.Laneclear, new[] { SpellSlot.Q, SpellSlot.R }, new[] { true, false });
                laneclear.AddManaManager(Mode.Laneclear, new[] { SpellSlot.Q, SpellSlot.R }, new[] { 35, 35 });
                laneclear.AddItem(
                    new MenuItem("com.iezreal.farm.r.lc.minhit", "Min Minions hit for R").SetValue(
                        new Slider(10, 1, 20)));
            }
            //
            var lasthit = new Menu("Laneclear", "com.iezreal.farm.lh");
            {
                lasthit.AddModeMenu(Mode.Lasthit, new[] { SpellSlot.Q }, new[] { true });
                lasthit.AddManaManager(Mode.Lasthit, new[] { SpellSlot.Q }, new[] { 35 });
            }
            Menu.AddSubMenu(farmMenu);

            var killstealMenu = new Menu("Ezreal - Killsteal", "com.iezreal.ks");
            killstealMenu.AddItem(new MenuItem("ksEnabled", "Enable Killsteal").SetValue(true));
            killstealMenu.AddModeMenu(Mode.Killsteal, new[] { SpellSlot.Q, SpellSlot.W }, new[] { true, true });
            Menu.AddSubMenu(killstealMenu);

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
        /// <param name="mode">the mode the player is currently using</param>
        private void CastMysticShot(Mode mode)
        {
            var target = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range, TargetSelector.DamageType.Physical);
            if (target.IsValidTarget(_spells[SpellSlot.Q].Range) &&
                _player.Distance(target) <= MenuHelper.GetSliderValue("qRange"))
            {
                if (_spells[SpellSlot.Q].IsEnabledAndReady(mode) && _spells[SpellSlot.Q].CanCast(target))
                {
                    _spells[SpellSlot.Q].CastIfHitchanceEquals(
                        target, target.IsMoving ? HitChance.High : MenuHelper.GetHitchance());
                }
            }
        }

        /// <summary>
        ///     Casts Ezreal's Essence Flux
        /// </summary>
        /// <param name="mode">the mode the player is currently using</param>
        private void CastEssenceFlux(Mode mode)
        {
            var target = TargetSelector.GetTarget(_spells[SpellSlot.W].Range, TargetSelector.DamageType.Physical);
            if (target.IsValidTarget(_spells[SpellSlot.W].Range) &&
                _player.Distance(target) < MenuHelper.GetSliderValue("wRange"))
            {
                if (_spells[SpellSlot.W].IsEnabledAndReady(mode) && _spells[SpellSlot.W].CanCast(target))
                {
                    _spells[SpellSlot.W].CastIfHitchanceEquals(
                        target, target.IsMoving ? HitChance.High : MenuHelper.GetHitchance());
                }
            }
        }

        /// <summary>
        ///     Casts Ezreal's Trueshot Barrage takes into account minion and champion collision for damage reduction
        /// </summary>
        private void CastTrueshotBarrage()
        {
            var target = TargetSelector.GetTarget(20000, TargetSelector.DamageType.Physical);
            if (target.IsValidTarget(_spells[SpellSlot.R].Range))
            {
                if (_spells[SpellSlot.R].IsEnabledAndReady(Mode.Combo) &&
                    _player.Distance(target) <= MenuHelper.GetSliderValue("rRange"))
                {
                    if (CanExecuteTarget(target))
                    {
                        _spells[SpellSlot.R].CastIfHitchanceEquals(
                            target, target.IsMoving ? HitChance.High : MenuHelper.GetHitchance());
                    }
                    else
                    {
                        CastAoeUltimate();
                    }
                }
            }
        }

        /// <summary>
        ///     Casts the ultimate if x amount of enemies will be hit.
        /// </summary>
        private void CastAoeUltimate()
        {
            foreach (
                Obj_AI_Hero source in
                    from source in HeroManager.Enemies.Where(hero => hero.IsValidTarget(_spells[SpellSlot.R].Range))
                    let prediction = _spells[SpellSlot.R].GetPrediction(source, true)
                    where
                        _player.Distance(source) <= MenuHelper.GetSliderValue("rRange") &&
                        prediction.AoeTargetsHitCount >= MenuHelper.GetSliderValue("rMin")
                    select source)
            {
                _spells[SpellSlot.R].CastIfHitchanceEquals(source, MenuHelper.GetHitchance());
            }
        }

        #endregion
    }
}