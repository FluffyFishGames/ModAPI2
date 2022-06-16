using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI.ViewModels
{
    public class GameSetupTab : GameTab
    {
        public GameSetupTab(Game vm) : base(vm, "Game setup", Material.Icons.MaterialIconKind.GearBox)
        {
        }
    }
}
