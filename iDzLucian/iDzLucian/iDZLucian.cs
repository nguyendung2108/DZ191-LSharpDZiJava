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
    internal class iDzLucian
    {
        private static Obj_AI_Hero _player;
        private static Spell _qExtended;
        public static Menu Menu;
        private static Orbwalking.Orbwalker _orbwalker;
        private static bool _shouldHavePassive;

        private static readonly Dictionary<SpellSlot, Spell> Spells = new Dictionary<SpellSlot, Spell>
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q, 675f) },
            { SpellSlot.W, new Spell(SpellSlot.W, 1000f) },
            { SpellSlot.E, new Spell(SpellSlot.E, 425f) },
            { SpellSlot.R, new Spell(SpellSlot.R, 1400f) }
        };

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
            Game.OnGameUpdate += OnGameUpdate;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            Orbwalking.AfterAttack += OrbwalkingAfterAttack;
        }

        private static void OrbwalkingAfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe)
                return;
            _shouldHavePassive = false;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            //TODO Get Correct spell names
            //Reset the AutoAttack timer after a Q, so we can attack immediately after.
            //Logic for Spell Weaving would be:
            //W AA Q AA (E AA)? // wouldn't it be Q aa, W aa, E, aa?
            if (sender.IsMe)
            {
                switch (args.SData.Name)
                {
                    case "LucianQ":
                        Utility.DelayAction.Add(
                            (int) (Math.Ceiling(Game.Ping / 2f) + 250 + 325), Orbwalking.ResetAutoAttackTimer);
                        break;
                    case "LucianW":
                        Utility.DelayAction.Add(
                            (int) (Math.Ceiling(Game.Ping / 2f) + 250 + 375), Orbwalking.ResetAutoAttackTimer);
                        break;
                    case "LucianE":
                        break;
                }
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

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Spells[SpellSlot.Q].Range, TargetSelector.DamageType.Physical);

            if (target.IsValidTarget(Spells[SpellSlot.Q].Range))
            {
                if (Spells[SpellSlot.Q].IsEnabledAndReady(Mode.Combo))
                {
                    if (Spells[SpellSlot.Q].CanCast(target) && !(HasPassive() && Orbwalking.InAutoAttackRange(target)))
                    {
                        Spells[SpellSlot.Q].Cast(target);
                        _orbwalker.ForceTarget(target);
                        _shouldHavePassive = true;
                    }
                    else
                    {
                        ExtendedQ();
                    }
                }
                if (Spells[SpellSlot.W].IsEnabledAndReady(Mode.Combo) && !Spells[SpellSlot.Q].CanCast(target) &&
                    !(HasPassive() && Orbwalking.InAutoAttackRange(target)))
                {
                    if (Spells[SpellSlot.W].GetPrediction(target).Hitchance >= HitChance.High)
                    {
                        Spells[SpellSlot.W].Cast(target);
                        _orbwalker.ForceTarget(target);
                        _shouldHavePassive = true;
                    }
                }
            }
        }

        private static void ExtendedQ()
        {
            //Untested lmao
            var target = TargetSelector.GetTarget(Spells[SpellSlot.Q].Range, TargetSelector.DamageType.Physical);
            var targetExtended = TargetSelector.GetTarget(_qExtended.Range, TargetSelector.DamageType.Physical);
            if (!target.IsValidTarget() && targetExtended.IsValidTarget())
            {
                var targetPrediction = _qExtended.GetPrediction(targetExtended).UnitPosition.To2D();
                var qCollision = _qExtended.GetCollision(
                    ObjectManager.Player.ServerPosition.To2D(), new List<Vector2> { targetPrediction });
                if (qCollision.Any())
                {
                    Spells[SpellSlot.Q].Cast(qCollision.First());
                    _shouldHavePassive = true;
                }
            }
        }

        private static void Harass()
        {
            
        }

        private static void Farm()
        {
            
        }

        private static bool HasPassive()
        {
            return _shouldHavePassive || ObjectManager.Player.HasBuff("lucianpassivebuff");
        }

        #region Menu and Spells

        private static void LoadSpells()
        {
            Spells[SpellSlot.Q].SetTargetted(0.25f, float.MaxValue);
            _qExtended = new Spell(SpellSlot.Q, 1100);
            _qExtended.SetSkillshot(0.25f, 5f, float.MaxValue, true, SkillshotType.SkillshotLine);
            Spells[SpellSlot.W].SetSkillshot(0.3f, 80, 1600, true, SkillshotType.SkillshotLine);
            Spells[SpellSlot.E].SetSkillshot(.25f, 1f, float.MaxValue, false, SkillshotType.SkillshotLine);
            Spells[SpellSlot.R].SetSkillshot(.1f, 110, 2800, true, SkillshotType.SkillshotLine);
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
                Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R },
                new[] { true, true, false, false });
            comboMenu.AddManaManager(
                Mode.Combo, new[] { SpellSlot.Q, SpellSlot.W, SpellSlot.E, SpellSlot.R }, new[] { 35, 35, 25, 10 });
            Menu.AddSubMenu(comboMenu);

            var harassMenu = new Menu("Lucian - Harass", "com.idzlucian.harass");
            harassMenu.AddModeMenu(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W }, new[] { true, true });
            harassMenu.AddManaManager(Mode.Harrass, new[] { SpellSlot.Q, SpellSlot.W }, new[] { 35, 35 });
            Menu.AddSubMenu(harassMenu);

            var farmMenu = new Menu("Lucian - Farm", "com.idzlucian.farm");
            farmMenu.AddModeMenu(Mode.Laneclear, new[] { SpellSlot.Q }, new[] { true });
            farmMenu.AddManaManager(Mode.Laneclear, new[] { SpellSlot.Q }, new[] { 35 });
            Menu.AddSubMenu(farmMenu);

            var miscMenu = new Menu("Lucian - Misc", "com.idzlucian.misc");

            Menu.AddToMainMenu();
        }

        #endregion
    }
}