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
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace iDZed
{
    internal static class Zed
    {
        private static Menu _menu;
        private static Orbwalking.Orbwalker _orbwalker;

        private static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        private delegate void OnOrbwalkingMode();

        // ReSharper disable once InconsistentNaming
        private static readonly Dictionary<SpellSlot, Spell> _spells = new Dictionary<SpellSlot, Spell>
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q, 900f) },
            { SpellSlot.W, new Spell(SpellSlot.W, 550f) },
            { SpellSlot.E, new Spell(SpellSlot.E, 290f) },
            { SpellSlot.R, new Spell(SpellSlot.R, 625f) }
        };

        private static Dictionary<Orbwalking.OrbwalkingMode, OnOrbwalkingMode> _orbwalkingModesDictionary;

        public static void OnLoad()
        {
            Game.PrintChat("iDZed loaded!");
            ShadowManager.OnLoad();
            _orbwalkingModesDictionary = new Dictionary<Orbwalking.OrbwalkingMode, OnOrbwalkingMode>
            {
                { Orbwalking.OrbwalkingMode.Combo, Combo },
                { Orbwalking.OrbwalkingMode.Mixed, Harass },
                { Orbwalking.OrbwalkingMode.LastHit, Farm },
                { Orbwalking.OrbwalkingMode.LaneClear, Farm },
                { Orbwalking.OrbwalkingMode.None, () => { } }
            };
            InitMenu();
            InitSpells();
            InitEvents();
        }

        #region Spell Casting

        private static void CastQ(Obj_AI_Hero target)
        {
            if (_spells[SpellSlot.Q].IsReady())
            {
                if (ShadowManager.WShadow != null && ShadowManager.WShadow.State == ShadowState.Created)
                {
                    _spells[SpellSlot.Q].UpdateSourcePosition(
                        ShadowManager.WShadow.ShadowObject.Position, ShadowManager.WShadow.ShadowObject.Position);
                    _spells[SpellSlot.Q].Cast(target);
                }
                else
                {
                    _spells[SpellSlot.Q].UpdateSourcePosition(Player.ServerPosition, Player.ServerPosition);
                    _spells[SpellSlot.Q].Cast(target);
                }
            }
        }

        private static void CastW(Obj_AI_Hero target)
        {
            if (ShadowManager.WShadow.State == ShadowState.NotActive)
            {
                if (_spells[SpellSlot.W].IsReady())
                {
                    Vector2 position = Player.ServerPosition.To2D()
                        .Extend(target.ServerPosition.To2D(), _spells[SpellSlot.W].Range);
                    if (position.Distance(target) <= _spells[SpellSlot.Q].Range)
                    {
                        _spells[SpellSlot.W].Cast(position);
                    }
                }
            }
        }

        private static void CastE(Obj_AI_Hero target)
        {
            if (_spells[SpellSlot.E].IsReady())
            {
                if (ShadowManager.WShadow != null && ShadowManager.WShadow.State == ShadowState.Created)
                {
                    _spells[SpellSlot.E].UpdateSourcePosition(
                        ShadowManager.WShadow.ShadowObject.Position, ShadowManager.WShadow.ShadowObject.Position);
                    if (_spells[SpellSlot.E].IsInRange(target))
                        _spells[SpellSlot.E].Cast();
                }
                else
                {
                    _spells[SpellSlot.E].UpdateSourcePosition(Player.ServerPosition, Player.ServerPosition);
                    if (_spells[SpellSlot.E].IsInRange(target))
                        _spells[SpellSlot.E].Cast();
                }
            }
        }

        #endregion

        #region Modes Region

        private static void Combo()
        {
            Obj_AI_Hero target = TargetSelector.GetTarget(
                _spells[SpellSlot.W].Range + _spells[SpellSlot.Q].Range, TargetSelector.DamageType.Physical);

            CastW(target);
            CastE(target);
            CastQ(target);
        }

        private static void Harass() {}
        private static void Farm() {}

        #endregion

        #region Initialization Region

        private static void InitMenu()
        {
            _menu = new Menu("iDZed - Reloaded", "com.idz.zed", true);
            Menu tsMenu = new Menu("[iDZed] TargetSelector", "com.idz.zed.targetselector");
            TargetSelector.AddToMenu(tsMenu);
            _menu.AddSubMenu(tsMenu);

            var orbwalkMenu = new Menu("[iDZed] Orbwalker", "com.idz.zed.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkMenu);
            _menu.AddSubMenu(orbwalkMenu);

            var comboMenu = new Menu("[iDZed] Combo", "com.idz.zed.combo");
            {
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.useq", "Use Q").SetValue(true));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.usew", "Use W").SetValue(true));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.usee", "Use E").SetValue(true));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.user", "Use R").SetValue(true));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.swapw", "Swap W For Follow").SetValue(false));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.swapr", "Swap R On kill").SetValue(true));
            }
            ;
            _menu.AddSubMenu(comboMenu);
            _menu.AddToMainMenu();
        }

        private static void InitSpells()
        {
            _spells[SpellSlot.Q].SetSkillshot(0.25f, 50f, 1700f, false, SkillshotType.SkillshotLine);
        }

        private static void InitEvents()
        {
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        #endregion

        #region Events Region

        private static void Game_OnUpdate(EventArgs args)
        {
            _orbwalkingModesDictionary[_orbwalker.ActiveMode]();
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (Shadow shadow in
                ShadowManager._shadowsList.Where(sh => sh.State != ShadowState.NotActive && sh.ShadowObject != null))
            {
                Render.Circle.DrawCircle(shadow.ShadowObject.Position, 60f, System.Drawing.Color.Orange);
            }
        }

        #endregion
    }
}