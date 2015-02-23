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

using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace iDzLucian.Helpers
{
    internal class PositionHelper
    {
        public static bool IsSafePosition(Vector3 position)
        {
            if (position.UnderTurret(true) && !ObjectManager.Player.UnderTurret(true))
            {
                return false;
            }
            var allies = position.CountAlliesInRange(ObjectManager.Player.AttackRange);
            var enemies = position.CountEnemiesInRange(ObjectManager.Player.AttackRange);
            var lhEnemies = GetLhEnemiesNearPosition(position, ObjectManager.Player.AttackRange).Count();

            if (enemies == 1) //It's a 1v1, safe to assume I can E
            {
                return true;
            }

            //Adding 1 for the Player
            return (allies + 1 > enemies - lhEnemies);
        }

        public static List<Obj_AI_Hero> GetLhEnemiesNearPosition(Vector3 position, float range)
        {
            return
                HeroManager.Enemies.Where(
                    hero => hero.IsValidTarget(range, true, position) && hero.HealthPercentage() <= 15).ToList();
        }
    }
}