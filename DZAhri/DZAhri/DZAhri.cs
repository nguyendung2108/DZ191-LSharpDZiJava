using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace DZAhri
{
    class DZAhri
    {
        public static Menu _menu;
        public static Orbwalking.Orbwalker _orbwalker;
        private static readonly Dictionary<SpellSlot, Spell> _spells = new Dictionary<SpellSlot, Spell>
        {
            { SpellSlot.Q, new Spell(SpellSlot.Q, 1100f) },
            { SpellSlot.W, new Spell(SpellSlot.W, 800f) },
            { SpellSlot.E, new Spell(SpellSlot.E, 980f) },
            { SpellSlot.R, new Spell(SpellSlot.R, 3000f) }
        };

        public static void OnLoad()
        {
            SetUpMenu();
            SetUpSpells();
            SetUpEvents();
        }

        #region Events, Spells, Menu
        private static void SetUpEvents()
        {
            throw new NotImplementedException();
        }

        private static void SetUpSpells()
        {
            throw new NotImplementedException();
        }

        private static void SetUpMenu()
        {
             _menu = new Menu("DZAhri","dz191.ahri",true);
            var _orbMenu = new Menu("[Ahri] Orbwalker", "dz191.ahri.orbwalker");
            _orbwalker = new Orbwalking.Orbwalker(_orbMenu);
            _menu.AddSubMenu(_orbMenu);
            var _tsMenu = new Menu("[Ahri] Target Selector", "dz191.ahri.ts");
            TargetSelector.AddToMenu(_tsMenu);
            _menu.AddSubMenu(_tsMenu);
            var _comboMenu = new Menu("[Ahri] Combo", "dz191.ahri.combo");
            {
                _comboMenu.AddItem(new MenuItem("dz191.ahri.combo.useq", "Use Q Combo").SetValue(true));
                _comboMenu.AddItem(new MenuItem("dz191.ahri.combo.usew", "Use W Combo").SetValue(true));
                _comboMenu.AddItem(new MenuItem("dz191.ahri.combo.usee", "Use E Combo").SetValue(true));
                _comboMenu.AddItem(new MenuItem("dz191.ahri.combo.user", "Use R Combo").SetValue(true));
                _comboMenu.AddItem(new MenuItem("dz191.ahri.combo.mana", "Min Combo Mana").SetValue(new Slider(20)));
            }
            _menu.AddSubMenu(_comboMenu);

            var _harassMenu = new Menu("[Ahri] Harass", "dz191.ahri.harass");
            {
                _harassMenu.AddItem(new MenuItem("dz191.ahri.harass.useq", "Use Q Harass").SetValue(true));
                _harassMenu.AddItem(new MenuItem("dz191.ahri.harass.usew", "Use W Harass").SetValue(true));
                _harassMenu.AddItem(new MenuItem("dz191.ahri.harass.usee", "Use E Harass").SetValue(true));
                _harassMenu.AddItem(new MenuItem("dz191.ahri.harass.onlyqcharm", "Use Q Only when charmed").SetValue(true));
                _harassMenu.AddItem(new MenuItem("dz191.ahri.harass.mana", "Min Combo Mana").SetValue(new Slider(20)));
            }
            _menu.AddSubMenu(_harassMenu);

            var _miscMenu = new Menu("[Ahri] Misc", "dz191.ahri.misc");
            {
                _miscMenu.AddItem(new MenuItem("dz191.ahri.misc.egp", "Auto E Gapclosers").SetValue(true));
                _miscMenu.AddItem(new MenuItem("dz191.ahri.misc.eint", "Auto E Interrupter").SetValue(true));
                _miscMenu.AddItem(new MenuItem("dz191.ahri.misc.rgap", "R away gapclosers if E on CD").SetValue(false));
                _miscMenu.AddItem(new MenuItem("dz191.ahri.misc.autoq", "Auto Q Charmed targets").SetValue(false));
            }
            _menu.AddSubMenu(_miscMenu);

        }
        #endregion

    }
}
