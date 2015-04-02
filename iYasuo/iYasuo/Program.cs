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
using iYasuo.Evade;
using iYasuo.utils;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using SpellData = LeagueSharp.SpellData;

#endregion

namespace iYasuo
{
    internal enum Spells
    {
        Q,
        W,
        E,
        R
    }

    internal static class Program
    {
        //The menu instance
        public static Menu Menu;
        //The common orbwalker
        private static Orbwalking.Orbwalker _orbwalker;
        //The player
        private static Obj_AI_Hero _player;
        //The Spell Values for Q, W, E and R
        // ReSharper disable once InconsistentNaming
        private static readonly Dictionary<Spells, Spell> _spells = new Dictionary<Spells, Spell>
        {
            { Spells.Q, new Spell(SpellSlot.Q, 500f) },
            { Spells.W, new Spell(SpellSlot.W, 400f) },
            { Spells.E, new Spell(SpellSlot.E, 475f) },
            { Spells.R, new Spell(SpellSlot.R, 1200f) }
        };

        public static readonly List<Skillshot> DetectedSkillShots = new List<Skillshot>();
        private static readonly List<Skillshot> EvadeDetectedSkillshots = new List<Skillshot>();

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
            Obj_AI_Hero target = GetEnemy(1500f);

            if (Menu.Item("useEGap").GetValue<bool>() && _spells[Spells.E].IsReady())
            {
                Obj_AI_Base dashObject = GetBestDashObject(GetEnemy(1500f));
                Vector3 positionAfterE = V3E(_player.ServerPosition, dashObject.ServerPosition, 475);
                if (_player.Distance(dashObject) <= _spells[Spells.E].Range && _player.Distance(target) > _spells[Spells.Q].Range)
                {
                    if (Menu.Item("safetyCheck").GetValue<bool>() && positionAfterE.UnderTurret(true))
                    {
                        return;
                    }

                    _spells[Spells.E].Cast(dashObject);
                }
            }


            //Q Casting
            if (Menu.Item("useQC").GetValue<bool>() && _spells[Spells.Q].IsReady() &&
                target != null)
            {
                if (!_player.IsDashing())
                {
                    PredictionOutput prediction = _spells[Spells.Q].GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.Medium && _player.Distance(target) <= _spells[Spells.Q].Range)
                    {
                        _spells[Spells.Q].Cast(prediction.CastPosition);
                    }
                }
            }

            //Normal E cast
            if (Menu.Item("useEC").GetValue<bool>() && _spells[Spells.E].IsReady() &&
                _spells[Spells.E].IsInRange(target) && _player.CanDash(target))
            {
                if (target != null &&
                    V3E(_player.ServerPosition, target.ServerPosition, _spells[Spells.E].Range).UnderTurret(true))
                {
                    return;
                }
                _spells[Spells.E].CastOnUnit(target);
            }

            //R cast delay
            if (Menu.Item("useRC").GetValue<bool>() && _spells[Spells.R].IsReady() &&
                _spells[Spells.R].IsInRange(target) && target.IsAirborne())
            {
                var knockedUpEnemies =
                    HeroManager.Enemies.Where(x => x.IsAirborne() && x.IsValidTarget(_spells[Spells.R].Range));

                if (knockedUpEnemies.Count() >= Menu.Item("rCount").GetValue<Slider>().Value)
                {
                    _spells[Spells.R].Cast();
                }
                else
                {
                    if (Menu.Item("delayUltimate").GetValue<bool>())
                    {
                        Utility.DelayAction.Add(
                            (int) (AirborneTimeLeft(target) * 1000 - 200), () => _spells[Spells.R].Cast());
                    }
                }
            }
        }

        private static void OnFarm()
        {
            List<Obj_AI_Base> minions = MinionManager.GetMinions(
                _spells[Spells.Q].Range, MinionTypes.All, MinionTeam.NotAlly);
            Obj_AI_Base qMinion = minions.FirstOrDefault(min => min.IsValidTarget(_spells[Spells.Q].Range));

            Obj_AI_Minion minion =
                ObjectManager.Get<Obj_AI_Minion>()
                    .Where(x => x.IsValidTarget(_spells[Spells.E].Range))
                    .FirstOrDefault(x => _player.GetSpellDamage(x, SpellSlot.E) > x.Health);

            if (minion != null &&
                !V3E(_player.ServerPosition, minion.ServerPosition, _spells[Spells.E].Range).UnderTurret(true) &&
                Menu.Item("useEF").GetValue<bool>())
            {
                _spells[Spells.E].CastOnUnit(minion);
            }

            if (qMinion != null && qMinion.IsValidTarget(_spells[Spells.Q].Range) && _spells[Spells.Q].IsReady() && Menu.Item("useQF").GetValue<bool>())
            {
                var bestPosition = _spells[Spells.Q].GetLineFarmLocation(minions);
                if (!_player.IsDashing())
                {
                   _spells[Spells.Q].Cast(bestPosition.Position);
                }
            }
        }

        private static void OnHarass()
        {
            var target = TargetSelector.GetTarget(1500f, TargetSelector.DamageType.Physical);

            //Q Casting
            if (Menu.Item("useQH").GetValue<bool>() && _spells[Spells.Q].IsReady() &&
                target != null)
            {
                if (!_player.IsDashing())
                {
                    PredictionOutput prediction = _spells[Spells.Q].GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.High && _player.Distance(target) <= _spells[Spells.Q].Range)
                    {
                        _spells[Spells.Q].Cast(prediction.CastPosition);
                    }
                }
                else
                {
                    if (_player.Distance(V3E(_player.Position, target.Position, 475)) < 40 &&
                        target.Distance(V3E(_player.Position, target.Position, 475)) < 315)
                    {
                        _spells[Spells.Q].Cast(target.Position);
                    }
                }
            }

            if (Menu.Item("useEH").GetValue<bool>() && _spells[Spells.E].IsReady() &&
                _spells[Spells.E].IsInRange(target) && target != null)
            {
                if (Menu.Item("safetyCheck").GetValue<bool>() &&
                    V3E(_player.Position, target.Position, 475).UnderTurret(true))
                {
                    return;
                }

                _spells[Spells.E].CastOnUnit(target);
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
                            min.Distance(Game.CursorPos) < 400 && _player.Distance(min) <= 475f)
                    .OrderBy(min => min.Distance(Game.CursorPos))
                    .FirstOrDefault();

            _player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if (_spells[Spells.E].IsReady() && _spells[Spells.E].IsInRange(dashTarget) && _player.CanDash(dashTarget))
            {
                if (dashTarget != null &&
                    (Menu.Item("safetyCheck").GetValue<bool>() &&
                     V3E(_player.ServerPosition, dashTarget.ServerPosition, _spells[Spells.E].Range).UnderTurret(true)))
                {
                    return;
                }

                _spells[Spells.E].CastOnUnit(dashTarget);
            }
            if (Menu.Item("stackQ").GetValue<bool>() && !_player.HasEmpoweredSpell() && _spells[Spells.Q].IsReady() &&
                   _spells[Spells.Q].IsInRange(dashTarget) && dashTarget.IsValidTarget(_spells[Spells.Q].Range))
            {
                Utility.DelayAction.Add(
                    dashTarget.GetDistanceCastTime(_spells[Spells.E]), () => _spells[Spells.Q].Cast(dashTarget));
            }
        }

        private static void OnGapcloser(ActiveGapcloser gapcloser)
        {
            if (_spells[Spells.Q].IsReady() && _player.HasEmpoweredSpell())
            {
                if (_player.Distance(gapcloser.End) < 200)
                {
                    _spells[Spells.Q].Cast(gapcloser.Sender);
                }
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
            AntiGapcloser.OnEnemyGapcloser += OnGapcloser;
            SkillshotDetector.OnDetectSkillshot += OnDetectSkillshot;
            SkillshotDetector.OnDeleteMissile += OnDeleteMissile;
            DamagePrediction.OnSpellWillKill += OnKillableSpell;
            GameObject.OnCreate += OnCreateObject;
            Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
        }

        /// <summary>
        ///     Performs the update task
        /// </summary>
        /// <param name="args">The event arguments.</param>
        private static void OnGameUpdate(EventArgs args)
        {
            EvadeDetectedSkillshots.RemoveAll(skillshot => !skillshot.IsActive());
            if (_player.IsDead)
            {
                return;
            }

            if (_player.HasEmpoweredSpell())
            {
                _spells[Spells.Q].Range = 1000;
                _spells[Spells.Q].SetSkillshot(0.75f, 90, 1500, false, SkillshotType.SkillshotLine);
            }
            else
            {
                _spells[Spells.Q].Range = 500;
                _spells[Spells.Q].SetSkillshot(0.35f, 15, 8700, false, SkillshotType.SkillshotLine);
            }

            if (Menu.Item("blockDangerous").GetValue<bool>() && _spells[Spells.W].IsReady())
            {
                AutoWindwall();
            }

            if (Menu.Item("dodgeE").GetValue<bool>())
            {
                DodgeSkillshot();
            }

            if (Menu.Item("fleeKey").GetValue<KeyBind>().Active)
            {
                OnFlee();
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
            }
        }

        private static void OnCreateObject(GameObject sender, EventArgs arguments)
        {
            if (!(sender is Obj_SpellMissile) || !sender.IsValid)
            {
                return;
            }

            Obj_SpellMissile args = (Obj_SpellMissile) sender;

            if (args.SData.Name == "CaitlynAceintheHoleMissile" && args.Name == "LineMissile")
            {
                Vector3 startPosition = args.StartPosition;
                Vector3 castPosition = _player.ServerPosition.Extend(startPosition, 10);

                if (_spells[Spells.W].IsReady() && Menu.Item("blockDangerous").GetValue<bool>())
                {
                    Utility.DelayAction.Add(
                        ((int) (startPosition.Distance(_player.Position) / 2000f + Game.Ping / 2f)),
                        () => _spells[Spells.W].Cast(castPosition));
                }
            }
        }

        private static void OnProcessSpell(Obj_AI_Base sender1, GameObjectProcessSpellCastEventArgs args)
        {
            var sender = sender1 as Obj_AI_Hero;
            if (sender == null || sender.IsMe || sender.IsAlly || !args.Target.IsMe)
            {
                //Game.PrintChat(string.Format("Spell Name: {0} - Delay: {1}", args.SData.Name, args.SData.SpellCastTime));
                return;
            }

            Vector3 startPosition = args.Start;
            Vector3 castPosition = _player.ServerPosition.Extend(startPosition, 10);

            //TODO - Get correct spell names Kappo
            if (Menu.Item("blockDangerous").GetValue<bool>() && _spells[Spells.W].IsReady())
            {
                if (sender.ChampionName == "Syndra" && args.SData.Name == "SyndraR")
                {
                    Utility.DelayAction.Add((int) 0.25f, () => _spells[Spells.W].Cast(castPosition));
                }
                else if (sender.ChampionName == "Vayne" && args.SData.Name == "VayneCondemn")
                {
                    Utility.DelayAction.Add((int) 0.25f, () => _spells[Spells.W].Cast(castPosition));
                    // TODO possible check if the condem is going to stun, if not then dont block.
                }
                else if (sender.ChampionName == "Tristana" && args.SData.Name == "TristanaR")
                {
                    //Game.PrintChat(string.Format("Spell Name: {0} - Delay: {1}", args.SData.Name, args.SData.SpellCastTime));
                    Utility.DelayAction.Add((int) 0.25f, () => _spells[Spells.W].Cast(castPosition));
                }
                else if (sender.ChampionName == "Brand" && args.SData.Name == "BrandR")
                {
                    Utility.DelayAction.Add((int) 0.25f, () => _spells[Spells.W].Cast(castPosition));
                }
            }
        }

        private static void OnKillableSpell(Obj_AI_Hero sender, Obj_AI_Hero target, SpellData sdata)
        {
            if (sender.IsAlly) {}

            //TODO: blockable targeded spells :S
        }

        private static void OnDetectSkillshot(Skillshot skillshot)
        {
            //Check if the skillshot is already added.
            var alreadyAdded = false;
            foreach (var item in EvadeDetectedSkillshots)
            {
                if (item.SpellData.SpellName == skillshot.SpellData.SpellName &&
                    (item.Caster.NetworkId == skillshot.Caster.NetworkId &&
                     (skillshot.Direction).AngleBetween(item.Direction) < 5 &&
                     (skillshot.Start.Distance(item.Start) < 100 || skillshot.SpellData.FromObjects.Length == 0)))
                {
                    alreadyAdded = true;
                }
            }
            //Check if the skillshot is from an ally.
            if (skillshot.Caster.Team == ObjectManager.Player.Team)
            {
                return;
            }
            //Check if the skillshot is too far away.
            if (skillshot.Start.Distance(ObjectManager.Player.ServerPosition.To2D()) >
                (skillshot.SpellData.Range + skillshot.SpellData.Radius + 1000) * 1.5)
            {
                return;
            }
            //Add the skillshot to the detected skillshot list.
            if (!alreadyAdded)
            {
                //Multiple skillshots like twisted fate _spells[Spells.Q].
                if (skillshot.DetectionType == DetectionType.ProcessSpell)
                {
                    if (skillshot.SpellData.MultipleNumber != -1)
                    {
                        var originalDirection = skillshot.Direction;
                        for (var i = -(skillshot.SpellData.MultipleNumber - 1) / 2;
                            i <= (skillshot.SpellData.MultipleNumber - 1) / 2;
                            i++)
                        {
                            var end = skillshot.Start +
                                      skillshot.SpellData.Range *
                                      originalDirection.Rotated(skillshot.SpellData.MultipleAngle * i);
                            var skillshotToAdd = new Skillshot(
                                skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start, end,
                                skillshot.Caster);
                            EvadeDetectedSkillshots.Add(skillshotToAdd);
                        }
                        return;
                    }
                    if (skillshot.SpellData.SpellName == "UFSlash")
                    {
                        skillshot.SpellData.MissileSpeed = 1600 + (int) skillshot.Caster.MoveSpeed;
                    }
                    if (skillshot.SpellData.Invert)
                    {
                        var newDirection = -(skillshot.End - skillshot.Start).Normalized();
                        var end = skillshot.Start + newDirection * skillshot.Start.Distance(skillshot.End);
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, skillshot.Start, end,
                            skillshot.Caster);
                        EvadeDetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }
                    if (skillshot.SpellData.Centered)
                    {
                        var start = skillshot.Start - skillshot.Direction * skillshot.SpellData.Range;
                        var end = skillshot.Start + skillshot.Direction * skillshot.SpellData.Range;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Caster);
                        EvadeDetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }
                    if (skillshot.SpellData.SpellName == "SyndraE" || skillshot.SpellData.SpellName == "syndrae5")
                    {
                        const int angle = 60;
                        const int fraction = -angle / 2;
                        var edge1 =
                            (skillshot.End - skillshot.Caster.ServerPosition.To2D()).Rotated(
                                fraction * (float) Math.PI / 180);
                        var edge2 = edge1.Rotated(angle * (float) Math.PI / 180);
                        foreach (var minion in ObjectManager.Get<Obj_AI_Minion>())
                        {
                            var v = minion.ServerPosition.To2D() - skillshot.Caster.ServerPosition.To2D();
                            if (minion.Name == "Seed" && edge1.CrossProduct(v) > 0 && v.CrossProduct(edge2) > 0 &&
                                minion.Distance(skillshot.Caster) < 800 && (minion.Team != ObjectManager.Player.Team))
                            {
                                var start = minion.ServerPosition.To2D();
                                var end = skillshot.Caster.ServerPosition.To2D()
                                    .Extend(
                                        minion.ServerPosition.To2D(),
                                        skillshot.Caster.Distance(minion) > 200 ? 1300 : 1000);
                                var skillshotToAdd = new Skillshot(
                                    skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                                    skillshot.Caster);
                                EvadeDetectedSkillshots.Add(skillshotToAdd);
                            }
                        }
                        return;
                    }
                    if (skillshot.SpellData.SpellName == "AlZaharCalloftheVoid")
                    {
                        var start = skillshot.End - skillshot.Direction.Perpendicular() * 400;
                        var end = skillshot.End + skillshot.Direction.Perpendicular() * 400;
                        var skillshotToAdd = new Skillshot(
                            skillshot.DetectionType, skillshot.SpellData, skillshot.StartTick, start, end,
                            skillshot.Caster);
                        EvadeDetectedSkillshots.Add(skillshotToAdd);
                        return;
                    }
                    if (skillshot.SpellData.SpellName == "ZiggsQ")
                    {
                        var d1 = skillshot.Start.Distance(skillshot.End);
                        var d2 = d1 * 0.4f;
                        var d3 = d2 * 0.69f;
                        var bounce1SpellData = SpellDatabase.GetByName("ZiggsQBounce1");
                        var bounce2SpellData = SpellDatabase.GetByName("ZiggsQBounce2");
                        var bounce1Pos = skillshot.End + skillshot.Direction * d2;
                        var bounce2Pos = bounce1Pos + skillshot.Direction * d3;
                        bounce1SpellData.Delay =
                            (int) (skillshot.SpellData.Delay + d1 * 1000f / skillshot.SpellData.MissileSpeed + 500);
                        bounce2SpellData.Delay =
                            (int) (bounce1SpellData.Delay + d2 * 1000f / bounce1SpellData.MissileSpeed + 500);
                        var bounce1 = new Skillshot(
                            skillshot.DetectionType, bounce1SpellData, skillshot.StartTick, skillshot.End, bounce1Pos,
                            skillshot.Caster);
                        var bounce2 = new Skillshot(
                            skillshot.DetectionType, bounce2SpellData, skillshot.StartTick, bounce1Pos, bounce2Pos,
                            skillshot.Caster);
                        EvadeDetectedSkillshots.Add(bounce1);
                        EvadeDetectedSkillshots.Add(bounce2);
                    }
                    if (skillshot.SpellData.SpellName == "ZiggsR")
                    {
                        skillshot.SpellData.Delay =
                            (int) (1500 + 1500 * skillshot.End.Distance(skillshot.Start) / skillshot.SpellData.Range);
                    }
                    if (skillshot.SpellData.SpellName == "JarvanIVDragonStrike")
                    {
                        var endPos = new Vector2();
                        foreach (var s in EvadeDetectedSkillshots)
                        {
                            if (s.Caster.NetworkId == skillshot.Caster.NetworkId && s.SpellData.Slot == SpellSlot.E)
                            {
                                endPos = s.End;
                            }
                        }
                        foreach (var m in ObjectManager.Get<Obj_AI_Minion>())
                        {
                            if (m.BaseSkinName == "jarvanivstandard" && m.Team == skillshot.Caster.Team &&
                                skillshot.IsDanger(m.Position.To2D()))
                            {
                                endPos = m.Position.To2D();
                            }
                        }
                        if (!endPos.IsValid())
                        {
                            return;
                        }
                        skillshot.End = endPos + 200 * (endPos - skillshot.Start).Normalized();
                        skillshot.Direction = (skillshot.End - skillshot.Start).Normalized();
                    }
                }
                if (skillshot.SpellData.SpellName == "OriannasQ")
                {
                    var endCSpellData = SpellDatabase.GetByName("OriannaQend");
                    var skillshotToAdd = new Skillshot(
                        skillshot.DetectionType, endCSpellData, skillshot.StartTick, skillshot.Start, skillshot.End,
                        skillshot.Caster);
                    EvadeDetectedSkillshots.Add(skillshotToAdd);
                }
                //Dont allow fow detection.
                if (skillshot.SpellData.DisableFowDetection && skillshot.DetectionType == DetectionType.RecvPacket)
                {
                    return;
                }
                EvadeDetectedSkillshots.Add(skillshot);
            }
        }

        private static void OnDeleteMissile(Skillshot skillshot, Obj_SpellMissile missile)
        {
            if (skillshot.SpellData.SpellName == "VelkozQ")
            {
                var spellData = SpellDatabase.GetByName("VelkozQSplit");
                var direction = skillshot.Direction.Perpendicular();
                if (EvadeDetectedSkillshots.Count(s => s.SpellData.SpellName == "VelkozQSplit") == 0)
                {
                    for (var i = -1; i <= 1; i = i + 2)
                    {
                        var skillshotToAdd = new Skillshot(
                            DetectionType.ProcessSpell, spellData, Environment.TickCount, missile.Position.To2D(),
                            missile.Position.To2D() + i * direction * spellData.Range, skillshot.Caster);
                        EvadeDetectedSkillshots.Add(skillshotToAdd);
                    }
                }
            }
        }

        #endregion

        #region calculations and shit

        /// <summary>
        ///     Gets the time that it takes for the spell to reach the target
        /// </summary>
        /// <param name="target"> the target </param>
        /// <param name="spell"> the spell </param>
        /// <returns></returns>
        private static int GetDistanceCastTime(this Obj_AI_Base target, Spell spell)
        {
            return (int) (((_player.Distance(target) / spell.Speed) + spell.Delay) + Game.Ping / 2f);
        }

        /// <summary>
        ///     Uses windwall on any spell, that is dangerous or has a danger value above or equal to 3
        /// </summary>
        private static void AutoWindwall()
        {
            string[] exceptions = { "SejuaniArcticAssault" };

            foreach (Skillshot skillshot in EvadeDetectedSkillshots)
            {
                if (skillshot.SpellData.Type != SkillShotType.SkillshotCircle ||
                    skillshot.SpellData.Type != SkillShotType.SkillshotRing)
                {
                    var damage =
                        skillshot.Caster.GetDamageSpell(_player, skillshot.SpellData.SpellName).CalculatedDamage;

                    if (skillshot.SpellData.IsDangerous && skillshot.SpellData.DangerValue >= 3 ||
                        _player.Health < damage + 15)
                        // only block dangerous spells todo: get the skillshot damage and if is higher then my health > block.
                    {
                        if (!skillshot.IsAboutToHit(350, _player) ||
                            exceptions.Any(exception => skillshot.SpellData.SpellName == exception))
                        {
                            return;
                        }

                        //Game.PrintChat("BLOCK IT");

                        Vector3 castVector = _player.ServerPosition.Extend(skillshot.MissilePosition.To3D(), 10);
                        _spells[Spells.W].Cast(castVector);
                    }
                }
            }
        }

        private static bool HasEmpoweredSpell(this Obj_AI_Hero player)
        {
            return player.HasBuff("yasuoQ3W", true);
        }

        private static void DodgeSkillshot()
        {
            if (!Menu.Item("dodgeE").GetValue<bool>())
            {
                return;
            }

            foreach (Skillshot skillshot in EvadeDetectedSkillshots)
            {
                if (!skillshot.IsAboutToHit(250, _player))
                {
                    continue;
                }
                var dashObjects =
                    ObjectManager.Get<Obj_AI_Base>()
                        .Where(x => _player.CanDash(x) && x.IsValidTarget(_spells[Spells.E].Range))
                        .OrderBy(x => x.Distance(_player.Position))
                        .FirstOrDefault();

                bool isSafe = dashObjects != null &&
                              skillshot.IsSafe(V3E(_player.Position, dashObjects.Position, 475).To2D());

                if (dashObjects != null && _spells[Spells.E].IsReady() && isSafe)
                {
                    _spells[Spells.E].Cast(dashObjects);
                }
            }
        }

        private static Obj_AI_Base GetBestDashObject(Obj_AI_Hero target)
        {
            return
                ObjectManager.Get<Obj_AI_Base>()
                    .OrderByDescending(x => x.Distance(_player))
                    .FirstOrDefault(
                        x =>
                            x.IsValidTarget(_spells[Spells.E].Range) &&
                            x.Distance(target.ServerPosition) < _player.Distance(target.ServerPosition));
        }


        private static Obj_AI_Hero GetEnemy(float range = 0, TargetSelector.DamageType damageType = TargetSelector.DamageType.Physical)
        {
            if (Math.Abs(range) < 0.00001)
                range = _spells[Spells.Q].Range;

            if (!Menu.Item("AssassinActive").GetValue<bool>())
                return TargetSelector.GetTarget(range, damageType);

            var assassinRange = Menu.Item("AssassinSearchRange").GetValue<Slider>().Value;

            var vEnemy =
                HeroManager.Enemies
                    .Where(
                        enemy =>
                            enemy.Team != _player.Team && !enemy.IsDead && enemy.IsVisible &&
                            Menu.Item("Assassin" + enemy.ChampionName) != null &&
                            Menu.Item("Assassin" + enemy.ChampionName).GetValue<bool>() &&
                            _player.Distance(enemy) < assassinRange);

            if (Menu.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex == 1)
            {
                vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
            }

            Obj_AI_Hero[] objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();

            Obj_AI_Hero target = !objAiHeroes.Any()
                ? TargetSelector.GetTarget(range, damageType)
                : objAiHeroes[0];

            return target;
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

        #endregion

        #region menu and spells

        /// <summary>
        ///     Sets the spells skillshot values if needed
        /// </summary>
        private static void LoadSpells()
        {
            //NOP
        }

        /// <summary>
        ///     Creats the menu for the specified champion
        /// </summary>
        private static void CreateMenu()
        {
            Menu = new Menu("iYasuo", "com.iyasuo", true);

            Menu tsMenu = new Menu("iYasuo - Target Selector", "com.iyasuo.ts");
            TargetSelector.AddToMenu(tsMenu);
            Menu.AddSubMenu(tsMenu);
            new AssassinManager();

            Menu orbMenu = new Menu("iYasuo - Orbwalker", "com.iyasuo.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbMenu);
            Menu.AddSubMenu(orbMenu);

            Menu comboMenu = new Menu("iYasuo - Combo", "com.iyasuo.combo");
            {
                //Q Menu
                Menu qMenu = new Menu("Steel Tempest (Q)", "steelTempest");
                {
                    qMenu.AddItem(new MenuItem("useQC", "Enabled").SetValue(true));
                    //TODO add options for q gapclosers and interrupts
                    comboMenu.AddSubMenu(qMenu);
                }
                //W Menu
                Menu wMenu = new Menu("Windwall (W)", "windwall");
                {
                    wMenu.AddItem(new MenuItem("useWC", "Enabled").SetValue(true));
                    wMenu.AddItem(new MenuItem("blockDangerous", "Only Block Dangerous Spells").SetValue(true));

                    //TODO only wall dangerous etc etc
                    //TODO spell customizability
                    comboMenu.AddSubMenu(wMenu);
                }
                //E Menu
                Menu eMenu = new Menu("Sweeping Blade (E)", "sweepingBlade");
                {
                    eMenu.AddItem(new MenuItem("useEC", "Enabled").SetValue(true));
                    eMenu.AddItem(new MenuItem("useEGap", "Gapclose With E").SetValue(true));
                    eMenu.AddItem(new MenuItem("safetyCheck", "Safety Checks for dashing").SetValue(true));
                    eMenu.AddItem(new MenuItem("dodgeE", "Dodge with E").SetValue(true));
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
                Menu.AddSubMenu(comboMenu);
            }

            Menu harassMenu = new Menu("iYasuo - Harass", "com.iyasuo.harass");
            {
                harassMenu.AddItem(new MenuItem("useQH", "Use Q Harass").SetValue(false));
                harassMenu.AddItem(new MenuItem("useEH", "Use E Harass").SetValue(false));
                Menu.AddSubMenu(harassMenu);
            }

            Menu farmMenu = new Menu("iYasuo - Farm", "com.iyasuo.farm");
            {
                farmMenu.AddItem(new MenuItem("useQF", "Use Q to Farm").SetValue(true));
                farmMenu.AddItem(new MenuItem("useEF", "Use E to Farm").SetValue(true));
                Menu.AddSubMenu(farmMenu);
            }

            Menu fleeMenu = new Menu("iYasuo - Flee", "com.iyasuo.flee");
            {
                fleeMenu.AddItem(
                    new MenuItem("fleeKey", "Fleeing Key").SetValue(
                        new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
                fleeMenu.AddItem(new MenuItem("stackQ", "Stack Q while fleeing").SetValue(true));
                Menu.AddSubMenu(fleeMenu);
            }

            Menu.AddToMainMenu();
        }

        #endregion
    }
}