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

using LeagueSharp;
using LeagueSharp.Common;

namespace iYasuo.utils
{
    internal static class DamagePrediction
    {
        public delegate void OnKillableDelegate(Obj_AI_Hero sender, Obj_AI_Hero target, SpellData sData);

        static DamagePrediction()
        {
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        public static event OnKillableDelegate OnSpellWillKill;

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!(sender is Obj_AI_Hero) || !(args.Target is Obj_AI_Hero))
            {
                return;
            }
            var senderH = (Obj_AI_Hero) sender;
            var targetH = (Obj_AI_Hero) args.Target;
            var damage = Orbwalking.IsAutoAttack(args.SData.Name)
                ? sender.GetAutoAttackDamage(targetH)
                : GetDamage(senderH, targetH, senderH.GetSpellSlot(args.SData.Name));
            //DebugHelper.AddEntry("Damage to "+targetH.ChampionName+" from spell "+args.SData.Name+" -> "+senderH.ChampionName+" ("+senderH.GetSpellSlot(args.SData.Name)+")",damage.ToString());
            if (damage > targetH.Health + 20)
            {
                // ReSharper disable once UseNullPropagation
                if (OnSpellWillKill != null)
                {
                    OnSpellWillKill(senderH, targetH, args.SData);
                }
            }
        }

        private static float GetDamage(Obj_AI_Hero hero, Obj_AI_Hero target, SpellSlot slot)
        {
            return (float) hero.GetSpellDamage(target, slot);
        }
    }
}