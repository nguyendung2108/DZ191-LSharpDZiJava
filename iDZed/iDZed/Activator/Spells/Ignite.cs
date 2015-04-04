using System.Linq;
using iDzed.Activator.Spells;
using LeagueSharp;
using LeagueSharp.Common;

namespace iDZed.Activator.Spells
{
    class Ignite : ISummonerSpell
    {
        public void OnLoad() { }
        public string GetDisplayName()
        {
            return "Ignite";
        }

        public void AddToMenu(Menu menu)
        {
        }

        public bool RunCondition()
        {
            return GetSummonerSpell().IsReady() &&
                   MenuHelper.isMenuEnabled("com.idz.zed.activator.summonerspells." + GetName() + ".enabled") &&
                   ObjectManager.Player.GetEnemiesInRange(GetSummonerSpell().Range)
                       .Any(
                           h =>
                               h.Health + 20 <
                               ObjectManager.Player.GetSummonerSpellDamage(h, Damage.SummonerSpell.Ignite) &&
                               h.IsValidTarget(GetSummonerSpell().Range) && h.CountAlliesInRange(550f) < 3);
        }

        public void Execute()
        {
            var target = ObjectManager.Player.GetEnemiesInRange(GetSummonerSpell().Range).Find(h => h.Health + 20 < ObjectManager.Player.GetSummonerSpellDamage(h, Damage.SummonerSpell.Ignite));
            if (target.IsValidTarget(GetSummonerSpell().Range))
            {
                GetSummonerSpell().Cast(target);
            }
        }

        public SummonerSpell GetSummonerSpell()
        {
            return SummonerSpells.Ignite;
        }

        public string GetName()
        {
            return GetSummonerSpell().Names.First().ToLowerInvariant();
        }
    }
}
