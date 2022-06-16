using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using ReactiveUI;
using ModAPI.ViewModels;
using System;
using Newtonsoft.Json.Linq;

namespace ModAPI.ViewModels.ModProject
{
    public class ModProject : ViewModelBase
    {
        private Game _Game;
        public Game Game { get => _Game; set => this.RaiseAndSetIfChanged<ModProject, Game>(ref _Game, value, "Game"); }
        
        private string _Directory;
        public string Directory
        {
            get => _Directory;
            set
            {
                if (_Directory != value)
                {
                    _Directory = value;
                    this.Load();
                    this.RaisePropertyChanged<ModProject>("Directory");
                }
            }
        }

        private ProjectFile _ProjectFile;
        public ProjectFile ProjectFile { get => _ProjectFile; set => this.RaiseAndSetIfChanged<ModProject, ProjectFile>(ref _ProjectFile, value, "ProjectFile"); }

        private ModConfiguration _Configuration;
        public ModConfiguration Configuration { get => _Configuration; set => this.RaiseAndSetIfChanged<ModProject, ModConfiguration>(ref _Configuration, value, "Configuration"); }

        private bool _IsOutdated;
        public bool IsOutdated { get => _IsOutdated; set => this.RaiseAndSetIfChanged<ModProject, bool>(ref _IsOutdated, value, "IsOutdated"); }

        public static ModProject CreateProject(Game game, string name)
        {
            var directory = Path.Combine(game.GameDirectory, "ModAPI", "Projects", name);
            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);
            var modProject = new ModProject(game, directory, false);
            modProject.Configuration.Name = name;
            modProject.Configuration.Version = new ModVersion("0.0.1");
            modProject.Configuration.Save();
            modProject.ProjectFile.Save();

            return modProject;
        }

        public ModProject(Game game, string directory, bool loadConfiguration = true)
        {
            Game = game;
            _Directory = directory;// Path.Combine(Game.GameDirectory, "ModAPI", "Projects", name);
            this.Load(loadConfiguration);
            this.PropertyChanged += ModProject_PropertyChanged;
        }

        private void ModProject_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsActive")
            {
                if (this.IsActive)
                {
                    this.Game.SelectedProject = this;
                }
            }
        }

        public void Load(bool loadConfiguration = true)
        {
            if (!System.IO.Directory.Exists(Directory))
                System.IO.Directory.CreateDirectory(Directory);
            ProjectFile = new ProjectFile(this);
            Configuration = new ModConfiguration(this, loadConfiguration);
            IsOutdated = ProjectFile.UpdateRequired;
            ProjectFile.Save();
        }

        public void Save()
        {
            ProjectFile.Save();
        }

        public void Delete()
        {
            System.IO.Directory.Delete(Directory, true);
        }
    }
}
