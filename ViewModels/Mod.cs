using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Svg;
using ModAPI.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json.Linq;
using Mono.Cecil;

namespace ModAPI.ViewModels
{
    public class Mod : ViewModelBase
    {
        private static NLog.Logger Logger = NLog.LogManager.GetLogger("Mod");
        
        private void ModViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsActive" && IsActive == true)
            {
                this.Game.SelectedMod = this;
            }
        }

        public void BrowseTo(ViewModelBase page)
        {
            foreach (var _tab in Tabs)
            {
                if (_tab != page)
                    _tab.IsActive = false;
            }
            Page = page;
        }

        private ViewModelBase _Page;
        public ViewModelBase Page { get => _Page; set => this.RaiseAndSetIfChanged<Mod, ViewModelBase>(ref _Page, value, "Page"); }

        public List<ModTab> Tabs { get; set; }
        
        private Game _Game;
        public Game Game { get => _Game; set => this.RaiseAndSetIfChanged<Mod, Game>(ref _Game, value, "Game"); }

        private ModProject.ModConfiguration _Configuration;
        public ModProject.ModConfiguration Configuration { get => _Configuration; set => this.RaiseAndSetIfChanged<Mod, ModProject.ModConfiguration>(ref _Configuration, value, "Configuration"); }

        private string _File;
        public string File { get => _File; set => this.RaiseAndSetIfChanged<Mod, string>(ref _File, value, "File"); }

        public void SetJSON(JObject config)
        {
            Configuration.SetJSON(config, false);
        }

        public JObject ToJSON()
        {
            return Configuration.ToJSON();
        }

        public Mod(Game game, string fileName)
        {
            Game = game;
            File = fileName;
            Configuration = new ModProject.ModConfiguration(this, false);

            Load();
            this.PropertyChanged += ModViewModel_PropertyChanged;

            var tabs = new List<ModTab>();
            /*tabs.Add(new GameSetupTab(this));
            tabs.Add(new GameTab(this, "Mods", Material.Icons.MaterialIconKind.Archive));
            tabs.Add(new GameModProjectsTab(this));*/
            Tabs = tabs;


        }

        public void Load()
        {
            Configuration.Load();
        }
    }
}
