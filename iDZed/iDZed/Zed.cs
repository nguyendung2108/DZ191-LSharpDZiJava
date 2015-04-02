using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

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
            throw new NotImplementedException();
        }
        private static void Harass()
        {
            throw new NotImplementedException();
        }
        private static void Farm()
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Initialization Region
        private static void InitMenu()
        {
            _menu = new Menu("iDZed - Reloaded","com.idz.zed",true);
            TargetSelector.AddToMenu(_menu);
            var orbwalkMenu = new Menu("[iDZed] Orbwalker", "com.idz.zed.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(orbwalkMenu);
            var comboMenu = new Menu("[iDZed] Combo", "com.idz.zed.combo");
            {
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.useq", "Use Q"));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.usew", "Use W"));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.usee", "Use E"));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.user", "Use R"));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.swapw", "Swap W For Follow"));
                comboMenu.AddItem(new MenuItem("com.idz.zed.combo.swapr", "Swap R On kill"));
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
        }
        #endregion

        #region Events Region
        static void Game_OnUpdate(EventArgs args)
        {
            _orbwalkingModesDictionary[_orbwalker.ActiveMode]();
        }
        #endregion
    }
}
