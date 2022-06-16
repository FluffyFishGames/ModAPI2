using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace ModAPI.ViewModels
{
    public class GameModsTab : GameTab
    {
        private Mod _SelectedMod;
        public Mod SelectedMod { get => _SelectedMod; set => this.RaiseAndSetIfChanged<GameModsTab, Mod>(ref _SelectedMod, value, "SelectedMod"); }

        public GameModsTab(Game vm) : base(vm, "Mods", Material.Icons.MaterialIconKind.Archive)
        {
        }
    }
}
