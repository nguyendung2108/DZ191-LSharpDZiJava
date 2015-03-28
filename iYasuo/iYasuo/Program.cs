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

#region imports

using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

#endregion

namespace iYasuo
{
    internal enum Spells
    {
        Q,
        Q2,
        W,
        E,
        R
    }

    internal static class Program
    {
        //The menu instance
        private static Menu _menu;
        //The common orbwalker
        private static Orbwalking.Orbwalker _orbwalker;
        //The player
        private static Obj_AI_Hero _player;
        //The Spell Values for Q, W, E and R
        // ReSharper disable once InconsistentNaming
        private static readonly Dictionary<Spells, Spell> _spells = new Dictionary<Spells, Spell>
        {
            { Spells.Q, new Spell(SpellSlot.Q, 475f) },
            { Spells.Q2, new Spell(SpellSlot.Q, 900f) },
            { Spells.W, new Spell(SpellSlot.W, 400f) },
            { Spells.E, new Spell(SpellSlot.E, 475f) },
            { Spells.R, new Spell(SpellSlot.R, 1200f) }
        };

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        #region ActiveModes

        /// <summary>
        ///     Do the combo sequence
        /// </summary>
        private static void OnCombo()
        {
            //TODO :S
        }

        private static void OnFarm()
        {
            List<Obj_AI_Base> minions = MinionManager.GetMinions(
                _spells[Spells.Q2].Range, MinionTypes.All, MinionTeam.NotAlly);
            Obj_AI_Base qMinion = minions.FirstOrDefault(min => min.IsValidTarget(_spells[Spells.Q].Range));

            if (qMinion != null && qMinion.IsValidTarget(_spells[Spells.Q].Range) && _spells[Spells.Q].IsReady() &&
                !_player.HasEmpoweredSpell() && _menu.Item("useQF").GetValue<bool>())
            {
                _spells[Spells.Q].Cast(qMinion);
            }

            Obj_AI_Minion minion =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(x => x.IsValidTarget(_spells[Spells.E].Range))
                    .FirstOrDefault(x => _player.GetSpellDamage(x, SpellSlot.E) > x.Health);
            if (minion != null &&
                !V3E(_player.ServerPosition, minion.ServerPosition, _spells[Spells.E].Range).UnderTurret(true) &&
                _menu.Item("useEF").GetValue<bool>())
            {
                _spells[Spells.E].CastOnUnit(minion);
            }
        }

        /// <summary>
        ///     Do the flee sequence
        /// </summary>
        private static void OnFlee()
        {
            Obj_AI_Base dashTarget =
                ObjectManager.Get<Obj_AI_Base>()
                    .Where(
                        min =>
                            min.Distance(Game.CursorPos) < 400 && _player.Distance(min) <= 475f && _player.CanDash(min) &&
                            !V3E(_player.ServerPosition, min.ServerPosition, _spells[Spells.E].Range).UnderTurret(true))
                    .OrderBy(min => min.Distance(Game.CursorPos))
                    .FirstOrDefault();

            _player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if (_spells[Spells.E].IsReady() && _spells[Spells.E].IsInRange(dashTarget))
            {
                _spells[Spells.E].CastOnUnit(dashTarget);
            }
        }

        #endregion

        #region events

        private static void OnGameLoad(EventArgs args)
        {
            //Initialize our player
            _player = ObjectManager.Player;

            //If the champions name is not Yasuo then don't load the assembly
            if (_player.ChampionName != "Yasuo")
            {
                return;
            }
            //Load the spell values
            LoadSpells();
            //Set the menu and create the sub menus etc etc
            CreateMenu();

            //Event Subscribers
            Game.OnUpdate += OnGameUpdate;
        }

        /// <summary>
        ///     Performs the update task
        /// </summary>
        /// <param name="args">The event arguments.</param>
        private static void OnGameUpdate(EventArgs args)
        {
            if (_player.IsDead)
            {
                return;
            }

            if (_menu.Item("fleeKey").GetValue<KeyBind>().Active)
            {
                OnFlee();
            }

            switch (_orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    OnCombo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    //harass and last hit :S
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    //TODO laneclear
                    OnFarm();
                    break;
            }
        }

        #endregion

        #region Spell Casting

        private static void CastQ()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(_spells[Spells.Q2].Range, TargetSelector.DamageType.Physical);

            if (!target.IsValidTarget(_spells[Spells.Q2].Range))
            {
                return;
            }

            if (_menu.Item("useQC").GetValue<bool>() && _spells[Spells.Q].IsReady() &&
                _spells[Spells.Q].IsInRange(target))
            {
                _spells[Spells.Q].CastIfHitchanceEquals(target, HitChance.High);
            }

            if (_menu.Item("useQC2").GetValue<bool>() && _player.HasEmpoweredSpell() && _spells[Spells.Q2].IsReady() &&
                _spells[Spells.Q2].IsInRange(target))
            {
                _spells[Spells.Q2].CastIfHitchanceEquals(target, HitChance.High);
            }
        }

        // ReSharper disable once InconsistentNaming
        private static void CastEQ()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(_spells[Spells.E].Range, TargetSelector.DamageType.Physical);

            if (!target.IsValidTarget(_spells[Spells.E].Range))
            {
                return;
            }

            if (_menu.Item("useQC").GetValue<bool>() && _menu.Item("useEC").GetValue<bool>() &&
                _spells[Spells.Q].IsReady() && _spells[Spells.E].IsReady() &&
                _player.Distance(target) <= _spells[Spells.E].Range)
            {
                _spells[Spells.E].CastOnUnit(target);
                Utility.DelayAction.Add(target.GetDistanceCastTime(_spells[Spells.E]), () => _spells[Spells.Q].Cast());
            }
        }

        private static void CastE()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(_spells[Spells.Q].Range, TargetSelector.DamageType.Physical);
            Obj_AI_Base bestMinion = GetBestMinion(target);

            if (_menu.Item("useEGap").GetValue<bool>() && _player.Distance(target) > _spells[Spells.Q].Range)
            {
                if (_spells[Spells.E].IsReady() && bestMinion != null &&
                    bestMinion.IsValidTarget(_spells[Spells.E].Range) && _spells[Spells.E].IsInRange(bestMinion))
                {
                    _spells[Spells.E].CastOnUnit(bestMinion);
                }
            }
            else
            {
                if (_menu.Item("useEC").GetValue<bool>() && _spells[Spells.E].IsReady() &&
                    _spells[Spells.E].IsInRange(target) && target.IsValidTarget(_spells[Spells.E].Range))
                {
                    _spells[Spells.E].CastOnUnit(target);
                }
            }
        }

        private static void CastR()
        {
            IEnumerable<Obj_AI_Hero> knockedUpEnemies =
                HeroManager.Enemies.Where(hero => hero.IsValidTarget(_spells[Spells.R].Range) && hero.IsAirborne());

            if (knockedUpEnemies.Count() >= _menu.Item("rCount").GetValue<Slider>().Value)
            {
                if (_spells[Spells.R].IsReady() && ShouldCastR())
                {
                    _spells[Spells.R].Cast();
                }
            }

            if (_menu.Item("delayUltimate").GetValue<bool>() && _spells[Spells.R].IsReady())
            {
                //TODO :S
            }
        }

        #endregion

        #region calculations and shit

        private static Obj_AI_Base GetBestMinion(Obj_AI_Hero target)
        {
            return
                MinionManager.GetMinions(
                    _player.ServerPosition, _spells[Spells.E].Range, MinionTypes.All, MinionTeam.NotAlly)
                    .OrderByDescending(minion => minion.Distance(_player))
                    .FirstOrDefault(
                        minion =>
                            minion.IsValidTarget(_spells[Spells.E].Range) &&
                            _player.Distance(minion) <= _menu.Item("eGapRange").GetValue<Slider>().Value &&
                            minion.Distance(target.ServerPosition) < _player.Distance(target.ServerPosition));
        }

        private static int GetDistanceCastTime(this Obj_AI_Base target, Spell spell)
        {
            return (int) (((_player.Distance(target) / spell.Speed) + spell.Delay) + Game.Ping / 2f);
        }

        private static bool ShouldCastR()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(_spells[Spells.R].Range, TargetSelector.DamageType.Physical);
            Vector3 extendedPosition = _player.ServerPosition.Extend(target.ServerPosition, _spells[Spells.R].Range);

            return extendedPosition.CountEnemiesInRange(_spells[Spells.Q2].Range) <= 2 &&
                   !extendedPosition.UnderTurret(true); // TODO get combo damage.
        }

        private static float AirborneTimeLeft(Obj_AI_Hero target)
        {
            BuffInstance firstOrDefault =
                target.Buffs.FirstOrDefault(
                    buff => buff.Type.Equals(BuffType.Knockback) || buff.Type.Equals(BuffType.Knockup));
            if (firstOrDefault != null)
            {
                return firstOrDefault.EndTime - Game.Time;
            }
            return 0;
        }

        private static Vector3 V3E(Vector3 from, Vector3 to, float distance)
        {
            return from + Vector3.Normalize(to - from) * distance;
        }

        private static bool IsAirborne(this Obj_AI_Hero source)
        {
            return source.HasBuffOfType(BuffType.Knockup) || source.HasBuffOfType(BuffType.Knockback);
        }

        private static bool CanDash(this Obj_AI_Hero source, Obj_AI_Base target)
        {
            return source.Distance(target.ServerPosition) <= _spells[Spells.E].Range &&
                   !target.HasBuff("YasuoDashWrapper");
        }

        private static bool HasEmpoweredSpell(this Obj_AI_Hero source)
        {
            return source.HasBuff("YasuoQ3W");
        }

        #endregion

        #region menu and spells

        /// <summary>
        ///     Sets the spells skillshot values if needed
        /// </summary>
        private static void LoadSpells()
        {
            _spells[Spells.Q].SetSkillshot(0.36f, 350f, 20000f, false, SkillshotType.SkillshotLine);
            _spells[Spells.Q2].SetSkillshot(0.36f, 120, 1200f, false, SkillshotType.SkillshotLine);
        }

        /// <summary>
        ///     Creats the menu for the specified champion
        /// </summary>
        private static void CreateMenu()
        {
            _menu = new Menu("iYasuo", "com.iyasuo", true);

            Menu tsMenu = new Menu("iYasuo - Target Selector", "com.iyasuo.ts");
            TargetSelector.AddToMenu(tsMenu);
            _menu.AddSubMenu(tsMenu);

            Menu orbMenu = new Menu("iYasuo - Orbwalker", "com.iyasuo.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbMenu);
            _menu.AddSubMenu(orbMenu);

            Menu comboMenu = new Menu("iYasuo - Combo", "com.iyasuo.combo");
            {
                //Q Menu
                Menu qMenu = new Menu("Steel Tempest (Q)", "steelTempest");
                {
                    qMenu.AddItem(new MenuItem("useQC", "Enabled").SetValue(true));
                    qMenu.AddItem(new MenuItem("useQC2", "Use Whirlwind").SetValue(true));
                    comboMenu.AddSubMenu(qMenu);
                }
                //W Menu
                Menu wMenu = new Menu("Windwall (W)", "windwall");
                {
                    wMenu.AddItem(new MenuItem("useWC", "Enabled").SetValue(true));
                    //TODO only wall dangerous etc etc
                    //TODO spell customizability
                    comboMenu.AddSubMenu(wMenu);
                }
                //E Menu
                Menu eMenu = new Menu("Sweeping Blade (E)", "sweepingBlade");
                {
                    eMenu.AddItem(new MenuItem("useEC", "Enabled").SetValue(true));
                    eMenu.AddItem(new MenuItem("useEGap", "Gapclose With E").SetValue(true));
                    eMenu.AddItem(new MenuItem("eGapRange", "Gapclosing Range").SetValue(new Slider(1200, 0, 2000)));
                    //OTHER CUSTOMIZABLE STOOF?
                    comboMenu.AddSubMenu(eMenu);
                }
                //R Menu
                Menu rMenu = new Menu("Last Breath (R)", "lastBreath");
                {
                    rMenu.AddItem(new MenuItem("useRC", "Enabled").SetValue(true));
                    rMenu.AddItem(new MenuItem("delayUltimate", "Delay Ultimate for landing").SetValue(true));
                    rMenu.AddItem(new MenuItem("rCount", "Auto Ult on X enemies").SetValue(new Slider(3, 0, 5)));
                    comboMenu.AddSubMenu(rMenu);
                }
                _menu.AddSubMenu(comboMenu);
            }

            Menu harassMenu = new Menu("iYasuo - Harass", "com.iyasuo.harass");
            {
                harassMenu.AddItem(new MenuItem("useQH", "Use Q in harass").SetValue(false));
                _menu.AddSubMenu(harassMenu);
            }

            Menu farmMenu = new Menu("iYasuo - Farm", "com.iyasuo.farm");
            {
                farmMenu.AddItem(new MenuItem("useQF", "Use Q to Farm").SetValue(true));
                farmMenu.AddItem(new MenuItem("useEF", "Use E to Farm").SetValue(true));
                _menu.AddSubMenu(farmMenu);
            }

            Menu fleeMenu = new Menu("iYasuo - Flee", "com.iyasuo.flee");
            {
                fleeMenu.AddItem(
                    new MenuItem("fleeKey", "Fleeing Key").SetValue(
                        new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
                fleeMenu.AddItem(new MenuItem("stackQ", "Stack Q while fleeing").SetValue(true));
                _menu.AddSubMenu(fleeMenu);
            }

            _menu.AddToMainMenu();
        }

        #endregion
    }
}