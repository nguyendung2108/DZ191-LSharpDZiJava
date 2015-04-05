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
using iDZed.Activator;
using iDZed.Utils;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace iDZed
{
    internal static class Zed
    {
        public static Menu Menu;
        private static Orbwalking.Orbwalker _orbwalker;
        private static readonly SpellDataInst _wShadowSpell = Player.Spellbook.GetSpell(SpellSlot.W);
        private static readonly SpellDataInst _rShadowSpell = Player.Spellbook.GetSpell(SpellSlot.R);
        private static bool _deathmarkKilled;

        private static Obj_AI_Hero Player
        {
            get { return ObjectManager.Player; }
        }

        private delegate void OnOrbwalkingMode();

        // ReSharper disable once InconsistentNaming
        public static readonly Dictionary<SpellSlot, Spell> _spells = new Dictionary<SpellSlot, Spell>
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q, 900f) },
            { SpellSlot.W, new Spell(SpellSlot.W, 550f) },
            { SpellSlot.E, new Spell(SpellSlot.E, 275f) },
            { SpellSlot.R, new Spell(SpellSlot.R, 625f) }
        };

        private static Dictionary<Orbwalking.OrbwalkingMode, OnOrbwalkingMode> _orbwalkingModesDictionary;

        private static bool IsPassWall(Vector3 start, Vector3 end)
        {
            double count = Vector3.Distance(start, end);
            for (uint i = 0; i <= count; i += 25)
            {
                Vector2 pos = start.To2D().Extend(Player.ServerPosition.To2D(), -i);
                if (pos.IsWall())
                {
                    return true;
                }
            }
            return false;
        }

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

        #region superduper combos

        private static void DoLineCombo(Obj_AI_Hero target)
        {
            if (ShadowManager.RShadow.IsUsable)
            {
                if (_spells[SpellSlot.R].IsReady() && _spells[SpellSlot.R].IsInRange(target))
                {
                    _spells[SpellSlot.R].Cast(target);
                }
            }

            if (ShadowManager.RShadow.Exists && ShadowManager.WShadow.IsUsable)
            {
                Vector3 wCastLocation = Player.ServerPosition -
                                        Vector3.Normalize(target.ServerPosition - Player.ServerPosition) * 400;

                if (ShadowManager.WShadow.IsUsable && _wShadowSpell.ToggleState == 0 &&
                    Environment.TickCount - _spells[SpellSlot.W].LastCastAttemptT > 0)
                {
                    _spells[SpellSlot.W].Cast(wCastLocation);
                        // Maybe add a delay giving the target a chance to flash / zhonyas then it will place w at best location for more damage
                    _spells[SpellSlot.W].LastCastAttemptT = Environment.TickCount + 500;
                }
            }

            ItemManager.UseDeathmarkItems();

            if (ShadowManager.WShadow.Exists && ShadowManager.RShadow.Exists)
            {
                CastQ(target, true);
                CastE();
            }

            if (ShadowManager.CanGoToShadow(ShadowManager.WShadow, true) && _wShadowSpell.ToggleState == 2 &&
                !_deathmarkKilled)
            {
                if (Menu.Item("com.idz.zed.combo.swapw").GetValue<bool>() &&
                    ShadowManager.WShadow.ShadowObject.Distance(target.ServerPosition) <
                    Player.Distance(target.ServerPosition))
                {
                    _spells[SpellSlot.W].Cast();
                }
            }
        }

        private static void DoShadowCoax(Obj_AI_Hero target)
        {
            //TODO
        }

        private static void DoTriangleCombo(Obj_AI_Hero target)
            //I'm dumb, this triangular combo is only good for targets the Zhonyas, we can still use it for that i guess :^)
        {
            if (!_spells[SpellSlot.R].IsReady() || !_spells[SpellSlot.W].IsReady() ||
                !HasEnergy(new[] { SpellSlot.R, SpellSlot.W }))
            {
                return;
            }

            if (ShadowManager.RShadow.IsUsable) // Cast Ultimate m8 :S
            {
                if (_spells[SpellSlot.R].IsReady() && _spells[SpellSlot.R].IsInRange(target))
                {
                    _spells[SpellSlot.R].Cast(target);
                }
            }

            if (ShadowManager.RShadow.Exists && ShadowManager.WShadow.IsUsable)
            {
                Vector3 bestWPosition = VectorHelper.GetBestPosition(VectorHelper.GetVertices(target)[0], VectorHelper.GetVertices(target)[1]);
                    // Maybe add a delay giving the target a chance to flash / zhonyas then it will place w at best perpendicular location m8
                if (_wShadowSpell.ToggleState == 0 && Environment.TickCount - _spells[SpellSlot.W].LastCastAttemptT > 0)
                {
                    _spells[SpellSlot.W].Cast(bestWPosition);
                        //Allow half a second for the target to flash / zhonyas? :S
                    _spells[SpellSlot.W].LastCastAttemptT = Environment.TickCount + 500;
                }
            }

            ItemManager.UseDeathmarkItems();

            if (ShadowManager.WShadow.Exists && ShadowManager.RShadow.Exists)
            {
                CastQ(target);
                CastE();
            }
        }

        #endregion

        #region Spell Casting

        private static void CastQ(Obj_AI_Hero target, bool usePrediction = false)
        {
            if (_spells[SpellSlot.Q].IsReady())
            {
                if (ShadowManager.WShadow.Exists)
                {
                    _spells[SpellSlot.Q].UpdateSourcePosition(
                        ShadowManager.WShadow.ShadowObject.ServerPosition,
                        ShadowManager.WShadow.ShadowObject.ServerPosition);
                    if (usePrediction)
                    {
                        PredictionOutput prediction = _spells[SpellSlot.Q].GetPrediction(target);
                        if (prediction.Hitchance >= HitChance.High)
                        {
                            if (ShadowManager.WShadow.ShadowObject.Distance(target) <= _spells[SpellSlot.Q].Range)
                            {
                                _spells[SpellSlot.Q].Cast(prediction.CastPosition);
                            }
                        }
                    }
                    else
                    {
                        if (ShadowManager.WShadow.ShadowObject.Distance(target) <= _spells[SpellSlot.Q].Range)
                        {
                            _spells[SpellSlot.Q].Cast(target.ServerPosition);
                        }
                    }
                }
                else if (ShadowManager.RShadow.Exists)
                {
                    _spells[SpellSlot.Q].UpdateSourcePosition(
                        ShadowManager.RShadow.ShadowObject.ServerPosition,
                        ShadowManager.RShadow.ShadowObject.ServerPosition);
                    if (usePrediction)
                    {
                        PredictionOutput prediction = _spells[SpellSlot.Q].GetPrediction(target);
                        if (prediction.Hitchance >= HitChance.High)
                        {
                            if (ShadowManager.RShadow.ShadowObject.Distance(target) <= _spells[SpellSlot.Q].Range)
                            {
                                _spells[SpellSlot.Q].Cast(prediction.CastPosition);
                            }
                        }
                    }
                    else
                    {
                        if (ShadowManager.RShadow.ShadowObject.Distance(target) <= _spells[SpellSlot.Q].Range)
                        {
                            _spells[SpellSlot.Q].Cast(target.ServerPosition);
                        }
                    }
                }
                else
                {
                    _spells[SpellSlot.Q].UpdateSourcePosition(Player.ServerPosition, Player.ServerPosition);
                    if (usePrediction)
                    {
                        PredictionOutput prediction = _spells[SpellSlot.Q].GetPrediction(target);
                        if (prediction.Hitchance >= HitChance.High)
                        {
                            if (_spells[SpellSlot.Q].IsInRange(target) &&
                                target.IsValidTarget(_spells[SpellSlot.Q].Range))
                            {
                                _spells[SpellSlot.Q].Cast(prediction.CastPosition);
                            }
                        }
                    }
                    else
                    {
                        _spells[SpellSlot.Q].Cast(target.ServerPosition);
                    }
                }
            }
        }

        private static void CastW(Obj_AI_Hero target)
        {
            if (!HasEnergy(new[] { SpellSlot.W, SpellSlot.Q }))
            {
                return;
            }
            if (ShadowManager.WShadow.IsUsable)
            {
                if (_spells[SpellSlot.W].IsReady() && _wShadowSpell.ToggleState == 0 &&
                    Environment.TickCount - _spells[SpellSlot.W].LastCastAttemptT > 0)
                {
                    Vector2 position = Player.ServerPosition.To2D()
                        .Extend(target.ServerPosition.To2D(), _spells[SpellSlot.W].Range);
                    if (position.Distance(target) <= _spells[SpellSlot.Q].Range)
                    {
                        if (IsPassWall(Player.ServerPosition, target.ServerPosition))
                        {
                            return;
                        }

                        _spells[SpellSlot.W].Cast(position);
                        _spells[SpellSlot.W].LastCastAttemptT = Environment.TickCount + 500;
                    }
                }
            }
            if (ShadowManager.CanGoToShadow(ShadowManager.WShadow, true) && _wShadowSpell.ToggleState == 2)
            {
                if (Menu.Item("com.idz.zed.combo.swapw").GetValue<bool>() &&
                    ShadowManager.WShadow.ShadowObject.Distance(target.ServerPosition) <
                    Player.Distance(target.ServerPosition))
                {
                    _spells[SpellSlot.W].Cast();
                }
            }
        }

        private static void CastE()
        {
            if (!_spells[SpellSlot.E].IsReady())
            {
                return;
            }
            if (
                HeroManager.Enemies.Count(
                    hero =>
                        hero.IsValidTarget() &&
                        (hero.Distance(Player.ServerPosition) <= _spells[SpellSlot.E].Range ||
                         (ShadowManager.WShadow.ShadowObject != null &&
                          hero.Distance(ShadowManager.WShadow.ShadowObject.ServerPosition) <= _spells[SpellSlot.E].Range) ||
                         (ShadowManager.RShadow.ShadowObject != null &&
                          hero.Distance(ShadowManager.RShadow.ShadowObject.ServerPosition) <= _spells[SpellSlot.E].Range))) >
                0)
            {
                _spells[SpellSlot.E].Cast();
            }
        }

        private static bool HasEnergy(IEnumerable<SpellSlot> spells)
        {
            if (!Menu.Item("energyManagement").GetValue<bool>())
            {
                return true;
            }
            float totalCost = spells.Sum(slot => Player.Spellbook.GetSpell(slot).ManaCost);
            return Player.Mana >= totalCost;
        }

        #endregion

        #region Modes Region

        private static void Combo()
        {
            Obj_AI_Hero target = GetAssasinationTarget();

            // Game.PrintChat("First: " + GetVertices(target)[0]);
            //Game.PrintChat("Second: " + GetVertices(target)[1]);

            switch (Menu.Item("com.idz.zed.combo.mode").GetValue<StringList>().SelectedIndex)
            {
                case 0:
                    // NORMAL MODE  //TODO if target is killable with R damage + 3 q's and 2 e's + item damage then go ham? :S
                    if (Menu.Item("com.idz.zed.combo.usew").GetValue<bool>())
                    {
                        CastW(target);
                    }
                    if (Menu.Item("com.idz.zed.combo.useq").GetValue<bool>())
                    {
                        CastQ(target, true);
                    }
                    if (Menu.Item("com.idz.zed.combo.usee").GetValue<bool>())
                    {
                        CastE();
                    }
                    break;
                case 1: // Line mode
                    if (Menu.Item("com.idz.zed.combo.user").GetValue<bool>() && _spells[SpellSlot.R].IsReady() &&
                        HasEnergy(new[] { SpellSlot.W, SpellSlot.R }))
                    {
                        DoLineCombo(target);
                    }
                    else
                    {
                        if (Menu.Item("com.idz.zed.combo.usew").GetValue<bool>())
                        {
                            CastW(target);
                        }
                        if (Menu.Item("com.idz.zed.combo.useq").GetValue<bool>())
                        {
                            CastQ(target, true);
                        }
                        if (Menu.Item("com.idz.zed.combo.usee").GetValue<bool>())
                        {
                            CastE();
                        }
                    }
                    break;

                case 2: // triangle mode
                    if (Menu.Item("com.idz.zed.combo.user").GetValue<bool>() && _spells[SpellSlot.R].IsReady() &&
                        _spells[SpellSlot.W].IsReady() && HasEnergy(new[] { SpellSlot.R, SpellSlot.W }))
                    {
                        DoTriangleCombo(target);
                    }
                    else
                    {
                        if (Menu.Item("com.idz.zed.combo.usew").GetValue<bool>())
                        {
                            CastW(target);
                        }
                        if (Menu.Item("com.idz.zed.combo.useq").GetValue<bool>())
                        {
                            CastQ(target, true);
                        }
                        if (Menu.Item("com.idz.zed.combo.usee").GetValue<bool>())
                        {
                            CastE();
                        }
                    }
                    break;
            }
        }

        private static void Harass()
        {
            if (!Menu.Item("com.idz.zed.harass.useHarass").GetValue<bool>())
            {
                return;
            }

            Obj_AI_Hero target = TargetSelector.GetTarget(
                _spells[SpellSlot.W].Range + _spells[SpellSlot.Q].Range, TargetSelector.DamageType.Physical);
            Vector2 wPosition = Player.ServerPosition.To2D()
                .Extend(target.ServerPosition.To2D(), _spells[SpellSlot.E].Range);
            var wCastTime = (int) (Player.Distance(wPosition) / 2000f);
            switch (Menu.Item("com.idz.zed.harass.harassMode").GetValue<StringList>().SelectedIndex)
            {
                case 0: // "Q-E"
                    if (!HasEnergy(new[] { SpellSlot.E, SpellSlot.Q }))
                    {
                        return;
                    }
                    CastQ(target, true);
                    CastE();
                    break;
                case 1: //"W-E-Q"
                    if (!HasEnergy(new[] { SpellSlot.W, SpellSlot.E, SpellSlot.Q }))
                    {
                        return;
                    }
                    if (_spells[SpellSlot.W].IsReady() && ShadowManager.WShadow.IsUsable &&
                        _wShadowSpell.ToggleState == 0 &&
                        Environment.TickCount - _spells[SpellSlot.W].LastCastAttemptT > 0)
                    {
                        if (wPosition.Distance(target) <= _spells[SpellSlot.Q].Range)
                        {
                            if (IsPassWall(Player.ServerPosition, target.ServerPosition))
                            {
                                return;
                            }

                            _spells[SpellSlot.W].Cast(wPosition);
                            _spells[SpellSlot.W].LastCastAttemptT = Environment.TickCount + 500;
                        }
                    }
                    if (ShadowManager.WShadow.Exists || ShadowManager.WShadow.State == ShadowState.Travelling)
                    {
                        CastE();
                        Utility.DelayAction.Add(wCastTime, () => CastQ(target, true));
                    }
                    else
                    {
                        CastQ(target, true);
                        CastE();
                    }

                    break;
                case 2: //"W-Q-E" 
                    if (!HasEnergy(new[] { SpellSlot.W, SpellSlot.E, SpellSlot.Q }))
                    {
                        return;
                    }
                    if (_spells[SpellSlot.W].IsReady() && ShadowManager.WShadow.IsUsable &&
                        _wShadowSpell.ToggleState == 0 &&
                        Environment.TickCount - _spells[SpellSlot.W].LastCastAttemptT > 0)
                    {
                        if (wPosition.Distance(target) <= _spells[SpellSlot.Q].Range)
                        {
                            _spells[SpellSlot.W].Cast(target);
                            _spells[SpellSlot.W].LastCastAttemptT = Environment.TickCount + 500;
                        }
                    }
                    if (ShadowManager.WShadow.Exists)
                    {
                        CastQ(target, true);
                        CastE();
                    }
                    else
                    {
                        CastQ(target, true);
                        CastE();
                    }
                    break;
            }
        }

        private static void Farm()
        {
            var allMinions = MinionManager.GetMinions(Player.ServerPosition, 1000f);
            Obj_AI_Base qMinion =
                allMinions.FirstOrDefault(
                    x => x.IsValidTarget(_spells[SpellSlot.Q].Range) && _spells[SpellSlot.Q].IsInRange(x));
            if (Menu.Item("com.idz.zed.farm.useQ").GetValue<bool>() && _spells[SpellSlot.Q].IsReady())
            {
                var bestPosition = _spells[SpellSlot.Q].GetLineFarmLocation(allMinions);
                if (bestPosition.MinionsHit >= 2)
                {
                    _spells[SpellSlot.Q].Cast(bestPosition.Position);
                }
            }
            if (Menu.Item("com.idz.zed.farm.useE").GetValue<bool>() && _spells[SpellSlot.E].IsReady())
            {
                foreach (Obj_AI_Base minion in
                    MinionManager.GetMinions(Player.ServerPosition, _spells[SpellSlot.E].Range)
                        .Where(
                            minion =>
                                !Orbwalking.InAutoAttackRange(minion) &&
                                minion.Health < 0.75 * _spells[SpellSlot.E].GetDamage(minion)))
                {
                    _spells[SpellSlot.E].Cast(minion);
                }
            }
        }

        #endregion

        #region Initialization Region

        private static void InitMenu()
        {
            Menu = new Menu("iDZed - Reloaded", "com.idz.zed", true);
            Menu tsMenu = new Menu("[iDZed] TargetSelector", "com.idz.zed.targetselector");
            TargetSelector.AddToMenu(tsMenu);
            Menu.AddSubMenu(tsMenu);
            new AssassinManager();

            Menu orbwalkMenu = new Menu("[iDZed] Orbwalker", "com.idz.zed.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkMenu);
            Menu.AddSubMenu(orbwalkMenu);

            Menu comboMenu = new Menu("[iDZed] Combo", "com.idz.zed.combo");
            {
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.useq", "Use Q").SetValue(true));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.usew", "Use W").SetValue(true));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.usee", "Use E").SetValue(true));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.user", "Use R").SetValue(true));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.swapw", "Swap W For Follow").SetValue(false));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.swapr", "Swap R On kill").SetValue(true));
                comboMenu.AddItem(
                    new MenuItem("com.idz.zed.combo.mode", "Combo Mode").SetValue(
                        new StringList(new[] { "Normal Mode / No Ult", "Line Mode", "Triangle Mode" })));
            }
            Menu.AddSubMenu(comboMenu);

            Menu harassMenu = new Menu("[iDZed] Harass", "com.idz.zed.harass");
            {
                harassMenu.AddItem(new MenuItem("com.idz.zed.harass.useHarass", "Use Harass").SetValue(true));
                harassMenu.AddItem(
                    new MenuItem("com.idz.zed.harass.harassMode", "Harass Mode").SetValue(
                        new StringList(new[] { "Q-E", "W-E-Q", "W-Q-E" })));
            }
            Menu.AddSubMenu(harassMenu);

            Menu farmMenu = new Menu("[iDZed] Farm", "com.idz.zed.farm");
            {
                farmMenu.AddItem(new MenuItem("com.idz.zed.farm.useQ", "Use Q in Farm").SetValue(true));
                farmMenu.AddItem(new MenuItem("com.idz.zed.farm.useE", "Use E in Farm").SetValue(true));
            }
            Menu.AddSubMenu(farmMenu);

            Menu miscMenu = new Menu("[iDZed] Misc", "com.idz.zed.misc");
            {
                miscMenu.AddItem(new MenuItem("energyManagement", "Use Energy Management").SetValue(true));
                miscMenu.AddItem(new MenuItem("dodgeWithR", "Dodge Dangerous Spells with Ult").SetValue(true));
            }
            Menu.AddSubMenu(miscMenu);
            ItemManager.OnLoad(Menu);

            Menu.AddToMainMenu();
        }

        private static void InitSpells()
        {
            _spells[SpellSlot.Q].SetSkillshot(0.25f, 50f, 1700f, false, SkillshotType.SkillshotLine);
            _spells[SpellSlot.W].SetSkillshot(.25f, 270f, float.MaxValue, false, SkillshotType.SkillshotCircle);
            _spells[SpellSlot.E].SetSkillshot(0f, 220f, float.MaxValue, false, SkillshotType.SkillshotCircle);
        }

        private static void InitEvents()
        {
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            GameObject.OnCreate += OnCreateObject;
            Obj_AI_Base.OnProcessSpellCast += OnSpellCast;
        }

        #endregion

        #region Events Region

        private static void Game_OnUpdate(EventArgs args)
        {
            // ZedUltTargetMark :S
            /* foreach (BuffInstance buff in HeroManager.Enemies.Where(x => x.IsValidTarget(1000)).SelectMany(hero => hero.Buffs)) {
                 Game.PrintChat(string.Format("Buff Name: {0}", buff.Name));
             }*/
            _orbwalkingModesDictionary[_orbwalker.ActiveMode]();
        }

        private static void OnCreateObject(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_GeneralParticleEmitter))
            {
                return;
            }

            if (sender.Name == "Zed_Base_R_buf_tell.troy")
            {
                _deathmarkKilled = true;
                if (_rShadowSpell.ToggleState == 2 && ShadowManager.CanGoToShadow(ShadowManager.RShadow, true) &&
                    Menu.Item("com.idz.zed.combo.swapr").GetValue<bool>())
                {
                    _spells[SpellSlot.R].Cast();
                }
            }
            else
            {
                _deathmarkKilled = false;
            }
        }

        private static void OnSpellCast(Obj_AI_Base sender1, GameObjectProcessSpellCastEventArgs args)
        {
            Obj_AI_Hero sender = sender1 as Obj_AI_Hero;
            if (sender != null && sender.IsEnemy && sender.Team != Player.Team)
                // TODO this works asuna, just not all the time, pls make better or smth :S
            {
                //Game.PrintChat("Name: " +args.SData.Name);
                if (args.SData.Name == "ZhonyasHourglass" && sender.HasBuff("zedulttargetmark"))
                {
                    Vector3 bestPosition = VectorHelper.GetBestPosition(VectorHelper.GetVertices(sender, true)[0], VectorHelper.GetVertices(sender, true)[1]);
                    // TODO when i eventually finish this do more and more checks so we don't fuck up on anything  :S
                    if (_spells[SpellSlot.W].IsReady() && _wShadowSpell.ToggleState == 0 &&
                        Environment.TickCount - _spells[SpellSlot.W].LastCastAttemptT > 0)
                    {
                        _spells[SpellSlot.W].Cast(bestPosition);
                        _spells[SpellSlot.W].LastCastAttemptT = Environment.TickCount + 500;
                    }
                }

                if (Menu.Item("dodgeWithR").GetValue<bool>() && _spells[SpellSlot.R].IsReady() &&
                    ShadowManager.RShadow.IsUsable)
                {
                    foreach (string spellName in DangerousSpells.DangerousList) // TODO possibly use evade databse for all dangeroous spells, and or spells that will kill.
                    {
                        if (args.SData.Name == spellName) // 
                        {
                            if (_spells[SpellSlot.R].IsInRange(sender))
                                _spells[SpellSlot.R].Cast(sender);
                            else if (Player.Distance(args.End) < 250 && !_spells[SpellSlot.R].IsInRange(sender))
                            {
                                //Find another ultable target target kappa
                                Obj_AI_Hero selectedTarget = HeroManager.Enemies.FirstOrDefault(x => x.IsValidTarget(_spells[SpellSlot.R].Range) && x != sender);

                                //Cast Ult to that target :S
                                _spells[SpellSlot.R].Cast(selectedTarget);
                            }
                        }
                    }
                }

            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (Shadow shadow in
                ShadowManager._shadowsList.Where(sh => sh.State != ShadowState.NotActive && sh.ShadowObject != null))
            {
                Render.Circle.DrawCircle(shadow.ShadowObject.Position, 60f, System.Drawing.Color.Orange);
            }

            //var target = TargetSelector.GetTarget(1000, TargetSelector.DamageType.Physical);

            //Render.Circle.DrawCircle(GetVertices(target)[0], 100, System.Drawing.Color.BlueViolet);
            //Render.Circle.DrawCircle(GetVertices(target)[1], 100, System.Drawing.Color.Blue);
            //Render.Circle.DrawCircle(Player.ServerPosition, 100, System.Drawing.Color.Brown);
        }

        private static Obj_AI_Hero GetAssasinationTarget(float range = 0,
            TargetSelector.DamageType damageType = TargetSelector.DamageType.Physical)
        {
            if (Math.Abs(range) < 0.00001)
            {
                range = _spells[SpellSlot.R].IsReady()
                    ? _spells[SpellSlot.R].Range
                    : _spells[SpellSlot.W].Range + _spells[SpellSlot.Q].Range;
            }

            if (!Menu.Item("AssassinActive").GetValue<bool>())
            {
                return TargetSelector.GetTarget(range, damageType);
            }

            var assassinRange = Menu.Item("AssassinSearchRange").GetValue<Slider>().Value;

            var vEnemy =
                HeroManager.Enemies.Where(
                    enemy =>
                        enemy.Team != Player.Team && !enemy.IsDead && enemy.IsVisible &&
                        Menu.Item("Assassin" + enemy.ChampionName) != null &&
                        Menu.Item("Assassin" + enemy.ChampionName).GetValue<bool>() &&
                        Player.Distance(enemy) < assassinRange);

            if (Menu.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex == 1)
            {
                vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
            }

            Obj_AI_Hero[] objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();

            Obj_AI_Hero target = !objAiHeroes.Any() ? TargetSelector.GetTarget(range, damageType) : objAiHeroes[0];

            return target;
        }

        #endregion
    }
}