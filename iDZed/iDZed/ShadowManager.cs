using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;

namespace iDZed
{
    class ShadowManager
    {
        private static List<Shadow> _shadowsList = new List<Shadow>
        {
            new Shadow { State = ShadowState.NotActive, Type = ShadowType.Normal },
            new Shadow { State = ShadowState.NotActive, Type = ShadowType.Ult}
        };
        private const String ZedWMissileName = "ZedShadowDashMissile";
        private const String ZedRMissileName = "ZedUltMissile";
        private const String ZedShadowName = "ZedShadow";
        private const String ZedW2SsName = "ZedW2";
        private const String ZedR2SsName = "ZedR2";//TODO Check this

        public static void OnLoad()
        {
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
        }
        static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            switch (sender.Type)
            {
                case GameObjectType.obj_AI_Minion:
                    var minion = sender as Obj_AI_Minion;
                    if (minion != null && minion.Name.Equals(ZedShadowName))
                    {
                        var myShadow = _shadowsList.FirstOrDefault(shadow => (shadow.State == ShadowState.Travelling));
                        if (myShadow != null)
                        {
                            myShadow.State = ShadowState.Created;
                        }
                    }
                    break;
                case GameObjectType.obj_SpellMissile:
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
        }

        public static bool CanGoToShadow(Shadow shadow)
        {
            return shadow.State == ShadowState.Created;
        }
    }

    class Shadow
    {
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
