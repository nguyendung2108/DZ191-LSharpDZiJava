using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp.Common;

namespace iDZed.Activator
{
    class MenuHelper
    {
        public static bool isMenuEnabled(String item)
        {
            return Zed.Menu.Item(item).GetValue<bool>();
        }

        public static int getSliderValue(String item)
        {
            return Zed.Menu.Item(item) != null ? Zed.Menu.Item(item).GetValue<Slider>().Value : -1;
        }

        public static bool getKeybindValue(String item)
        {
            return Zed.Menu.Item(item).GetValue<KeyBind>().Active;
        }
    }
}
