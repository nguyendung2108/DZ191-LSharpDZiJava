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
        private const String ZedWShadowName = "";
        private const String ZedRShadowName = "";
        private const String ZedW2SsName = "";
        private const String ZedR2SsName = "";

        public static void OnLoad()
        {
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
        }
        static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            throw new NotImplementedException();
        }

        static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            throw new NotImplementedException();
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
