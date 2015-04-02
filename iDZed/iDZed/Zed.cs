using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace iDZed
{
    class Zed
    {
        public static Menu _menu;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        private delegate void OnOrbwalkingMode();
        private static readonly Dictionary<SpellSlot, Spell> _spells = new Dictionary<SpellSlot, Spell>()
        {
            {SpellSlot.Q, new Spell(SpellSlot.Q, 900f)},
            {SpellSlot.W, new Spell(SpellSlot.W, 550f)},
            {SpellSlot.E, new Spell(SpellSlot.E, 290f)},
            {SpellSlot.R, new Spell(SpellSlot.R, 625f)}
        };
        private static Dictionary<Orbwalking.OrbwalkingMode, OnOrbwalkingMode> _orbwalkingModesDictionary;

        public static void OnLoad()
        {
            Game.PrintChat("iDZed loaded!");
            ShadowManager.OnLoad();
            _orbwalkingModesDictionary = new Dictionary<Orbwalking.OrbwalkingMode, OnOrbwalkingMode>()
            {
                {Orbwalking.OrbwalkingMode.Combo, Combo},
                {Orbwalking.OrbwalkingMode.Mixed, Harass},
                {Orbwalking.OrbwalkingMode.LastHit, Farm},
                {Orbwalking.OrbwalkingMode.LaneClear, Farm},
                {Orbwalking.OrbwalkingMode.None, () => {}},
            };
            InitMenu();
            InitSpells();
            InitEvents();
        }

        #region Modes Region
        private static void Combo()
        {
        }
        private static void Harass()
        {
        }
        private static void Farm()
        {
        }
        #endregion

        #region Initialization Region
        private static void InitMenu()
        {
            _menu = new Menu("iDZed - Reloaded","com.idz.zed",true);
            var tsMenu = new Menu("[iDZed] TargetSelector", "com.idz.zed.targetselector");
            TargetSelector.AddToMenu(tsMenu);
            _menu.AddSubMenu(tsMenu);
            var orbwalkMenu = new Menu("[iDZed] Orbwalker", "com.idz.zed.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkMenu);
            _menu.AddSubMenu(orbwalkMenu);
            var comboMenu = new Menu("[iDZed] Combo", "com.idz.zed.combo");
            {
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.useq", "Use Q").SetValue(true));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.usew", "Use W").SetValue(true));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.usee", "Use E").SetValue(true));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.user", "Use R").SetValue(true));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.swapw", "Swap W For Follow").SetValue(false));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.swapr", "Swap R On kill").SetValue(true));
            };
            _menu.AddSubMenu(comboMenu);
            _menu.AddToMainMenu();
        }

        private static void InitSpells()
        {
            _spells[SpellSlot.Q].SetSkillshot(0.25f, 50f, 1700f, false, SkillshotType.SkillshotLine);
        }

        private static void InitEvents()
        {
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }


        #endregion

        #region Events Region
        static void Game_OnUpdate(EventArgs args)
        {
            _orbwalkingModesDictionary[_orbwalker.ActiveMode]();
        }
        static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var shadow in ShadowManager._shadowsList.Where(sh => sh.State != ShadowState.NotActive && sh.ShadowObject != null))
            {
                Render.Circle.DrawCircle(shadow.ShadowObject.Position,60f,System.Drawing.Color.Orange);
            }
        }
        #endregion
    }
}
