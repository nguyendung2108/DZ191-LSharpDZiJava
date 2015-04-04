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

namespace iDZed
{
    internal static class ShadowManager
    {
        // ReSharper disable once InconsistentNaming
        public static readonly List<Shadow> _shadowsList = new List<Shadow>
        {
            new Shadow { State = ShadowState.NotActive, Type = ShadowType.Normal },
            new Shadow { State = ShadowState.NotActive, Type = ShadowType.Ult }
        };

        private const string ZedWMissileName = "ZedShadowDashMissile";
        private const string ZedRMissileName = "ZedUltMissile";
        private const string ZedShadowName = "zedshadow";
        private const string ZedW2SsName = "ZedW2";
        private const string ZedR2SsName = "ZedR2"; //TODO Check this

        public static Shadow WShadow
        {
            get { return _shadowsList.Find(x => x.Type == ShadowType.Normal); }
        }

        public static Shadow RShadow
        {
            get { return _shadowsList.Find(x => x.Type == ShadowType.Ult); }
        }

        public static void OnLoad()
        {
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (!(sender is Obj_AI_Minion) && !(sender is Obj_SpellMissile))
            {
                return;
            }
            switch (sender.Type)
            {
                case GameObjectType.obj_AI_Minion:
                    var minion = sender as Obj_AI_Minion;
                    if (minion != null && minion.BaseSkinName.Equals(ZedShadowName))
                    {
                        var myShadow = _shadowsList.FirstOrDefault(shadow => (shadow.State == ShadowState.Travelling));
                        if (myShadow != null)
                        {
                            myShadow.State = ShadowState.Created;
                            myShadow.ShadowObject = minion;
                            //Hacky workaround, TODO: Find a better way
                            Utility.DelayAction.Add(
                                4200, () =>
                                {
                                    myShadow.State = ShadowState.NotActive;
                                    myShadow.ShadowObject = null;
                                });
                        }
                    }
                    break;
                default:
                    var spell = (Obj_SpellMissile) sender;
                    var caster = spell.SpellCaster;
                    var spellName = spell.SData.Name;
                    if (caster.IsMe)
                    {
                        switch (spellName)
                        {
                            case ZedRMissileName:
                                var rShadow = _shadowsList.FirstOrDefault(shadow => shadow.Type == ShadowType.Ult);
                                if (rShadow != null)
                                {
                                    rShadow.State = ShadowState.Travelling;
                                }
                                break;
                            case ZedWMissileName:
                                var wShadow = _shadowsList.FirstOrDefault(shadow => shadow.Type == ShadowType.Normal);
                                if (wShadow != null)
                                {
                                    wShadow.State = ShadowState.Travelling;
                                }
                                break;
                        }
                    }
                    break;
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            //This is bugged and does not happen immediately as the zed shadow disappear, but instead 3-4 seconds later.
            if (sender != null && sender.IsAlly)
            {
                var myShadow =
                    _shadowsList.Find(
                        shadow => shadow.ShadowObject != null && shadow.ShadowObject.NetworkId.Equals(sender.NetworkId));
                if (myShadow != null)
                {
                    myShadow.State = ShadowState.NotActive;
                    myShadow.ShadowObject = null;
                }
            }
        }

        public static bool CanGoToShadow(Shadow shadow, bool safetyCheck = false) //TODO safety Checks lel
        {
            if (safetyCheck)
            {
                if (shadow.State == ShadowState.Created)
                {
                    if (ObjectManager.Player.HealthPercent < 35 || shadow.ShadowObject.Position.UnderTurret(true) ||
                        (shadow.ShadowObject.CountEnemiesInRange(1200f) > 1 &&
                         shadow.ShadowObject.CountEnemiesInRange(1200f) < 2))
                        // add a slider for the health percent.
                    {
                        return false;
                    }
                }
            }

            return shadow.State == ShadowState.Created;
        }
    }

    internal class Shadow
    {
        public Obj_AI_Minion ShadowObject { get; set; }
        public ShadowState State { get; set; }
        public ShadowType Type { get; set; }

        public bool IsUsable
        {
            get { return ShadowObject == null && State == ShadowState.NotActive; }
        }

        public bool Exists
        {
            get { return ShadowObject != null && State != ShadowState.NotActive; }
        }
    }

    internal enum ShadowType
    {
        Normal,
        Ult
    }

    internal enum ShadowState
    {
        NotActive,
        Travelling,
        Created,
        Used
    }
}