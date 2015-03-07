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
using System.Reflection;
using iDzLucian.Helpers;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace iDzLucian
{
    // ReSharper disable once InconsistentNaming
    internal class iDzLucian
    {
        private static Obj_AI_Hero _player;
        private static Spell _qExtended;
        public static Menu Menu;
        private static Orbwalking.Orbwalker _orbwalker;
        private static bool _shouldHavePassive;
        //Do not resharp _spells name, tyvm mkkk :3
        // ReSharper disable once InconsistentNaming
        private static readonly Dictionary<SpellSlot, Spell> _spells = new Dictionary<SpellSlot, Spell>
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q, 675f) },
            { SpellSlot.W, new Spell(SpellSlot.W, 1000f) },
            { SpellSlot.E, new Spell(SpellSlot.E, 425f) },
            { SpellSlot.R, new Spell(SpellSlot.R, 1400f) }
        };

        #region Events

        public static void OnLoad(EventArgs args)
        {
            _player = ObjectManager.Player;

            if (_player.ChampionName != "Lucian")
            {
                return;
            }

            LoadSpells();
            CreateMenu();
            Notifications.AddNotification(
                new Notification("iDZLucian v" + Assembly.GetExecutingAssembly().GetName().Version + " loaded!", 2500));
            Game.PrintChat("iDZLucian v" + Assembly.GetExecutingAssembly().GetName().Version + " loaded!");
            Game.OnUpdate += OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Orbwalking.AfterAttack += OrbwalkingAfterAttack;
        }

        private static void OrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
            {
                return;
            }
            //TODO Add E for Kiting and get away
            _shouldHavePassive = false;
            Obj_AI_Hero hero = target as Obj_AI_Hero;
            if (hero != null)
            {
                var targ = hero;
                if (_spells[SpellSlot.E].IsEnabledAndReady(Mode.Combo))
                {
                    var hypotheticalPosition = ObjectManager.Player.ServerPosition.Extend(
                        Game.CursorPos, _spells[SpellSlot.E].Range);
                    if (ObjectManager.Player.HealthPercentage() <= 30 &&
                        hero.HealthPercentage() >= ObjectManager.Player.HealthPercentage())
                    {
                        if (ObjectManager.Player.Position.Distance(ObjectManager.Player.ServerPosition) >= 35 &&
                            hero.Distance(ObjectManager.Player.ServerPosition) <
                            hero.Distance(ObjectManager.Player.Position) &&
                            PositionHelper.IsSafePosition(hypotheticalPosition))
                        {
                            _spells[SpellSlot.E].Cast(hypotheticalPosition);
                        }
                    }
                    if (PositionHelper.IsSafePosition(hypotheticalPosition) &&
                        hypotheticalPosition.Distance(targ.ServerPosition) <= Orbwalking.GetRealAutoAttackRange(null) &&
                        (!_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo) || !_spells[SpellSlot.Q].CanCast(targ)) &&
                        (!_spells[SpellSlot.W].IsEnabledAndReady(Mode.Combo) ||
                         !_spells[SpellSlot.W].CanCast(targ) &&
                         (hypotheticalPosition.Distance(targ.ServerPosition) > 400) && !(HasPassive())))
                    {
                        _spells[SpellSlot.E].Cast(hypotheticalPosition);
                    }
                }
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if (ObjectManager.Player.GetSpellSlot(args.SData.Name) != SpellSlot.R)
                {
                    _shouldHavePassive = true;
                    Utility.DelayAction.Add((int) Math.Floor(2000 - (Game.Ping / 2f)), () => _shouldHavePassive = false);
                }
                switch (args.SData.Name)
                {
                    case "LucianQ":
                        Utility.DelayAction.Add(
                            (int) (Math.Ceiling(Game.Ping / 2f) + 250 + 325), Orbwalking.ResetAutoAttackTimer);
                        break;
                    case "LucianW":
                        Utility.DelayAction.Add(
                            (int) (Math.Ceiling(Game.Ping / 2f) + 250 + 325), Orbwalking.ResetAutoAttackTimer);
                        break;
                }
                //Console.WriteLine(args.SData.Name);
            }
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }
            switch (_orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.LaneClear:
                    Farm();
                    break;
                case Orbwalking.OrbwalkingMode.LastHit:
                    Farm();
                    break;
            }
        }

        #endregion

        #region ActiveModes

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range, TargetSelector.DamageType.Physical);
            ExtendedQ(Mode.Combo);
            if (target.IsValidTarget(_spells[SpellSlot.Q].Range))
            {
                if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo))
                {
                    if (_spells[SpellSlot.Q].CanCast(target) && !(HasPassive()) && Orbwalking.InAutoAttackRange(target))
                    {
                        _spells[SpellSlot.Q].CastOnUnit(target);
                        _orbwalker.ForceTarget(target);
                    }
                }
                if (_spells[SpellSlot.W].IsEnabledAndReady(Mode.Combo) && !(_spells[SpellSlot.Q].CanCast(target) ||
                    _spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo)) &&
                    !(HasPassive()) && Orbwalking.InAutoAttackRange(target))
                {
                    _spells[SpellSlot.W].Cast(target);
                    _orbwalker.ForceTarget(target);
                }
            }
        }

        private static void Harass() // TODO needs testing, its basically just the same as combo imo
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(
                _spells[SpellSlot.Q].Range, TargetSelector.DamageType.Physical);

            ExtendedQ(Mode.Harass);

            if (target.IsValidTarget(_spells[SpellSlot.Q].Range))
            {
                if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Harass))
                {
                    if (_spells[SpellSlot.Q].CanCast(target) && !HasPassive() && Orbwalking.InAutoAttackRange(target))
                    {
                        _spells[SpellSlot.Q].CastOnUnit(target);
                        _orbwalker.ForceTarget(target);
                    }
                }
                if (_spells[SpellSlot.W].IsEnabledAndReady(Mode.Harass) && _spells[SpellSlot.W].CanCast(target) &&
                    !HasPassive() && !_spells[SpellSlot.Q].CanCast(target) &&
                    !_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Harass) && Orbwalking.InAutoAttackRange(target))
                {
                    _spells[SpellSlot.W].CastIfHitchanceEquals(target, MenuHelper.GetHitchance());
                    _orbwalker.ForceTarget(target);
                }
            }
        }

        private static void Farm()
        {
            var allMinions = MinionManager.GetMinions(_player.ServerPosition, _spells[SpellSlot.Q].Range);
            switch (_orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.LaneClear:
                    var minionFarmLocation = _spells[SpellSlot.Q].GetCircularFarmLocation(allMinions, 60);
                    if (minionFarmLocation.MinionsHit >= MenuHelper.GetSliderValue("com.idzlucian.farm.q.lc.minhit"))
                    {
                        var minionC =
                            allMinions.FindAll(m => m.Distance(minionFarmLocation.Position) <= 60)
                                .OrderBy(m => m.Distance(minionFarmLocation.Position));
                        if (!minionC.Any())
                        {
                            return;
                        }
                        var minion = minionC.First(m => m.IsValidTarget());
                        if (minion.IsValidTarget())
                        {
                            if (_spells[SpellSlot.Q].IsEnabledAndReady(Mode.Laneclear) && !HasPassive() &&
                                _spells[SpellSlot.Q].CanCast(minion) && Orbwalking.InAutoAttackRange(minion))
                            {
                                _spells[SpellSlot.Q].CastOnUnit(minion);
                            }
                        }
                    }
                    break;
            }
        }

        private static void Killsteal()
        {
            var target = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range, TargetSelector.DamageType.Physical);
            if (_spells[SpellSlot.Q].IsReady() && _spells[SpellSlot.Q].CanCast(target))
            {
                if (_spells[SpellSlot.Q].IsInRange(target) && _spells[SpellSlot.Q].GetDamage(target) > target.Health + 10)
                {
                    _spells[SpellSlot.Q].CastOnUnit(target);
                    //TODO extended Q finisher
                }
            }
        }

        #endregion

        #region Calculations and Checks

        private static bool HasPassive()
        {
            if (!MenuHelper.IsMenuEnabled("com.idzlucian.skilloptions.weave"))
            {
                return false;
            }

            return _shouldHavePassive || ObjectManager.Player.HasBuff("lucianpassivebuff");
        }

        private static double GetCullingDamage(Obj_AI_Hero target)
        {
            int level = _spells[SpellSlot.R].Level;
            return
                (float)
                    (_player.GetSpellDamage(target, SpellSlot.R) *
                     (level == 1
                         ? 7.5 + 7.5 * (_player.AttackSpeedMod - .6) / 1.4
                         : level == 2
                             ? 7.5 + 9 * (_player.AttackSpeedMod - .6) / 1.4
                             : level == 3 ? 7.5 + 10.5 * (_player.AttackSpeedMod - .6) : 0));
        }

        private static void ExtendedQ(Mode mode)
        {
            if (
                !MenuHelper.IsMenuEnabled(
                    "com.idzlucian." + MenuHelper.GetFullNameFromMode(mode).ToLowerInvariant() + ".useextendedq") ||
                ObjectManager.Player.ManaPercentage() <
                MenuHelper.GetSliderValue(
                    "com.idzlucian.manamanager.qmana" + MenuHelper.GetStringFromMode(mode).ToLowerInvariant()))
            {
                return;
            }
            var target = TargetSelector.GetTarget(_spells[SpellSlot.Q].Range, TargetSelector.DamageType.Physical);
            var targetExtended = TargetSelector.GetTarget(_qExtended.Range, TargetSelector.DamageType.Physical);
            if (!target.IsValidTarget(_spells[SpellSlot.Q].Range) && targetExtended.IsValidTarget(_qExtended.Range))
            {
                var targetPrediction = _qExtended.GetPrediction(targetExtended).CastPosition.To2D();
                var qCollision = _qExtended.GetCollision(
                    ObjectManager.Player.ServerPosition.To2D(), new List<Vector2> { targetPrediction });
                if (qCollision.Any())
                {
                    _spells[SpellSlot.Q].CastOnUnit(qCollision.First());
                }
            }
        }

        /// <summary>
        ///     Goes ham on a target using a minion for collision, and finishing the target with an extended Q
        /// </summary>
        private static void DashKillsteal()
        {
            //TODO test this, remains untesed due to my high ping. cmon dz embaress me
            var minions = MinionManager.GetMinions(_player.ServerPosition, _spells[SpellSlot.Q].Range);
            var extendedQTarget = TargetSelector.GetTarget(_qExtended.Range, TargetSelector.DamageType.Physical);

            if (extendedQTarget == null || !extendedQTarget.IsValidTarget(_qExtended.Range) ||
                !_spells[SpellSlot.Q].IsReady() || !_spells[SpellSlot.E].IsReady())
            {
                return;
            }

            foreach (var selectedMinion in
                minions.Where(minion => _spells[SpellSlot.Q].IsInRange(minion) && _qExtended.IsInRange(extendedQTarget))
                )
            {
                var bestPosition = _qExtended.GetPrediction(extendedQTarget, true).CastPosition.To2D();
                var collisionObjects = _qExtended.GetCollision(
                    selectedMinion.Position.To2D(), new List<Vector2> { bestPosition }); // FROM e endPositiono

                if (_spells[SpellSlot.E].IsInRange(bestPosition) && bestPosition != _player.Position.To2D())
                {
                    _spells[SpellSlot.E].Cast(bestPosition);
                }

                if (_spells[SpellSlot.Q].IsReady() && collisionObjects.Any())
                {
                    _spells[SpellSlot.Q].CastOnUnit(selectedMinion);
                }
            }
        }

        #endregion

        #region Menu and Spells

        private static void LoadSpells()
        {
            _spells[SpellSlot.Q].SetTargetted(0.25f, float.MaxValue);
            _qExtended = new Spell(SpellSlot.Q, 1100);
            _qExtended.SetSkillshot(0.25f, 5f, float.MaxValue, true, SkillshotType.SkillshotLine);
            _spells[SpellSlot.W].SetSkillshot(0.3f, 80, 1600, true, SkillshotType.SkillshotLine);
            _spells[SpellSlot.E].SetSkillshot(.25f, 1f, float.MaxValue, false, SkillshotType.SkillshotLine);
            _spells[SpellSlot.R].SetSkillshot(.1f, 110, 2800, true, SkillshotType.SkillshotLine);
        }

        private static void CreateMenu()
        {
            Menu = new Menu("iDzLucian", "com.idzlucian", true);

            var orbMenu = new Menu("Lucian - Orbwalker", "com.idzlucian.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbMenu);
            Menu.AddSubMenu(orbMenu);

            var tsMenu = new Menu("Lucian - Target Selector", "com.idzlucian.ts");
            TargetSelector.AddToMenu(tsMenu);
            Menu.AddSubMenu(tsMenu);

            var comboMenu = new Menu("Lucian - Combo", "com.idzlucian.combo");
            comboMenu.AddModeMenu(
                Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E }, new[] { true, true, false, false });
            comboMenu.AddManaManager(Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E }, new[] { 35, 35, 25 });

            var skillOptionsCombo = new Menu("Skill Options", "com.idzlucian.combo.skilloptions");
            {
                skillOptionsCombo.AddItem(
                    new MenuItem("com.idzlucian.skilloptions.weave", "Spell Weaving").SetValue(true));
                skillOptionsCombo.AddItem(
                    new MenuItem("com.idzlucian.combo.useextendedq", "Use Extended Q Combo").SetValue(true));
                skillOptionsCombo.AddItem(
                    new MenuItem("com.idzlucian.harass.useextendedq", "Use Extended Q Harass").SetValue(true));
            }
            comboMenu.AddSubMenu(skillOptionsCombo);

            Menu.AddSubMenu(comboMenu);

            var harassMenu = new Menu("Lucian - Harass", "com.idzlucian.harass");
            harassMenu.AddModeMenu(Mode.Harass, new[] { SpellSlot.Q, SpellSlot.W }, new[] { true, true });
            harassMenu.AddManaManager(Mode.Harass, new[] { SpellSlot.Q, SpellSlot.W }, new[] { 35, 35 });
            Menu.AddSubMenu(harassMenu);

            var farmMenu = new Menu("Lucian - Farm", "com.idzlucian.farm");
            farmMenu.AddModeMenu(Mode.Laneclear, new[] { SpellSlot.Q }, new[] { true });
            farmMenu.AddManaManager(Mode.Laneclear, new[] { SpellSlot.Q }, new[] { 35 });
            var farmOptions = new Menu("Farm Options", "com.idzlucian.farm.farm");
            {
                farmOptions.AddItem(
                    new MenuItem("com.idzlucian.farm.q.lc.minhit", "Min Minions for Q LC").SetValue(new Slider(2, 1, 6)));
            }
            farmMenu.AddSubMenu(farmOptions);
            Menu.AddSubMenu(farmMenu);

            var killstealMenu = new Menu("Lucian - Killsteal", "com.idzlucian.killsteal");
            {
                killstealMenu.AddItem(new MenuItem("com.idzlucian.ks.useQ", "Use Q Killsteal").SetValue(true));
                killstealMenu.AddItem(new MenuItem("com.idzlucian.ks.useW", "Use W Killsteal").SetValue(false));
            }

            var miscMenu = new Menu("Lucian - Misc", "com.idzlucian.misc");
            {
                miscMenu.AddHitChanceSelector();
                miscMenu.AddItem(new MenuItem("com.idzlucian.misc.debug", "Debug").SetValue(false));
            }
            Menu.AddSubMenu(miscMenu);

            Menu.AddToMainMenu();
        }

        #endregion
    }
}