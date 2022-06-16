using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace ModAPI.ViewModels
{
    public class GameModProjectsTab : GameTab
    {
        private ModProject.ModProject _SelectedProject;
        public ModProject.ModProject SelectedProject { get => _SelectedProject; set => this.RaiseAndSetIfChanged<GameModProjectsTab, ModProject.ModProject>(ref _SelectedProject, value, "SelectedProject"); }

        public GameModProjectsTab(Game vm) : base(vm, "Mod projects", Material.Icons.MaterialIconKind.Wrench)
        {
        }
    }
}
