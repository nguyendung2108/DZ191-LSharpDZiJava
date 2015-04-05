using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace iDZed.Utils
{
    static class VectorHelper
    {
        public static Vector3[] GetVertices(Obj_AI_Hero target, bool forZhonyas = false) // Zhonyas triangular ult
        {
            Shadow ultShadow = ShadowManager.RShadow;
            if (!ultShadow.Exists)
            {
                return new[] { Vector3.Zero, Vector3.Zero };
            }

            if (forZhonyas)
            {
                Vector2 vertex1 = ObjectManager.Player.ServerPosition.To2D() +
                                  Vector2.Normalize(
                                      target.ServerPosition.To2D() - ultShadow.ShadowObject.ServerPosition.To2D()) *
                                  Zed._spells[SpellSlot.W].Range;
                Vector2 vertex2 = ObjectManager.Player.ServerPosition.To2D() +
                                  Vector2.Normalize(
                                      target.ServerPosition.To2D() - ultShadow.ShadowObject.ServerPosition.To2D())
                                      .Perpendicular() * Zed._spells[SpellSlot.W].Range;
                Vector2 vertex3 = ObjectManager.Player.ServerPosition.To2D() +
                                  Vector2.Normalize(vertex1 - vertex2).Perpendicular() *
                                  Zed._spells[SpellSlot.W].Range;
                Vector2 vertex4 = ObjectManager.Player.ServerPosition.To2D() +
                                  Vector2.Normalize(vertex2 - vertex1).Perpendicular() *
                                  Zed._spells[SpellSlot.W].Range;

                return new[] { vertex3.To3D(), vertex4.To3D() };
            }

            Vector2 vertex5 = ObjectManager.Player.ServerPosition.To2D() +
                              Vector2.Normalize(
                                  target.ServerPosition.To2D() - ultShadow.ShadowObject.ServerPosition.To2D())
                                  .Perpendicular() * Zed._spells[SpellSlot.W].Range;
            Vector2 vertex6 = ObjectManager.Player.ServerPosition.To2D() +
                              Vector2.Normalize(
                                  ultShadow.ShadowObject.ServerPosition.To2D() - target.ServerPosition.To2D())
                                  .Perpendicular() * Zed._spells[SpellSlot.W].Range;
            return new[] { vertex5.To3D(), vertex6.To3D() };
        }

        public static Vector3 GetBestPosition(Vector3 firstPosition, Vector3 secondPosition)
        {
            if (firstPosition.IsWall() && !secondPosition.IsWall())
            // if firstposition is a wall and second position isn't
            {
                return secondPosition; //return second position
            }
            if (secondPosition.IsWall() && !firstPosition.IsWall())
            // if secondPosition is a wall and first position isn't
            {
                return firstPosition; // return first position
            }

            return firstPosition;
        }
    }
}
