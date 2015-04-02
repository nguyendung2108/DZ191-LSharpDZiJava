using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;

namespace iDZed
{
    class ShadowManager
    {
        public static List<Shadow> _shadowsList = new List<Shadow>
        {
            new Shadow { State = ShadowState.NotActive, Type = ShadowType.Normal },
            new Shadow { State = ShadowState.NotActive, Type = ShadowType.Ult}
        };
        private const String ZedWMissileName = "ZedShadowDashMissile";
        private const String ZedRMissileName = "ZedUltMissile";
        private const String ZedShadowName = "zedshadow";
        private const String ZedW2SsName = "ZedW2";
        private const String ZedR2SsName = "ZedR2";//TODO Check this

        public static void OnLoad()
        {
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
        }
        static void GameObject_OnCreate(GameObject sender, EventArgs args)
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
                            Utility.DelayAction.Add(4200, () =>
                            {
                                    myShadow.State = ShadowState.NotActive;
                                    myShadow.ShadowObject = null;
                            });
                        }
                    }
                    break;
                default:
                    var spell = (Obj_SpellMissile)sender;
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

        static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            //This is bugged and does not happen immediately as the zed shadow disappear, but instead 3-4 seconds later.
            if (sender != null && sender.IsAlly)
            {
                var myShadow = _shadowsList.Find(shadow => shadow.ShadowObject != null && shadow.ShadowObject.NetworkId.Equals(sender.NetworkId));
                if (myShadow != null)
                {
                    myShadow.State = ShadowState.NotActive;
                    myShadow.ShadowObject = null;
                }
            }
        }

        public static bool CanGoToShadow(Shadow shadow)
        {
            return shadow.State == ShadowState.Created;
        }
    }

    class Shadow
    {
        public Obj_AI_Minion ShadowObject { get; set; }
        public ShadowState State { get; set; }
        public ShadowType Type { get; set; }
    }

    enum ShadowType
    {
        Normal,
        Ult
    }

    enum ShadowState
    {
        NotActive,
        Travelling,
        Created,
        Used
    }
}
