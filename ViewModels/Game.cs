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

namespace ModAPI.ViewModels
{
    public class Game : ViewModelBase
    {
        protected FileSystemWatcher ProjectsWatcher;
        protected FileSystemWatcher ModsWatcher;
        protected static NLog.Logger Logger = NLog.LogManager.GetLogger("Game");
        protected List<string> ForbiddenAssemblies = new List<string>() { };// "Steamworks.NET" };

        private void GameViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsActive" && IsActive == true)
                AppViewModel.Instance.BrowseTo(this);
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

        protected ViewModelBase _Page;
        public ViewModelBase Page { get => _Page; set => this.RaiseAndSetIfChanged<Game, ViewModelBase>(ref _Page, value, "Page"); }

        protected ModProject.ModProject _SelectedProject;
        public ModProject.ModProject SelectedProject
        {
            get => _SelectedProject;
            set
            {
                if (value != _SelectedProject)
                {
                    foreach (var project in ModProjects)
                        if (project != value)
                            project.IsActive = false;
                    _SelectedProject = value;
                    this.RaisePropertyChanged<Game>("SelectedProject");
                }
            }
        }

        protected Mod _SelectedMod;
        public Mod SelectedMod
        {
            get => _SelectedMod;
            set
            {
                if (value != _SelectedMod)
                {
                    foreach (var mod in Mods)
                        if (mod != value)
                            mod.IsActive = false;
                    _SelectedMod = value;
                    this.RaisePropertyChanged<Game>("SelectedMod");
                }
            }
        }

        public List<GameTab> Tabs { get; set; }

        protected IImage _Banner;
        public IImage Banner { get => _Banner; set => this.RaiseAndSetIfChanged<Game, IImage>(ref _Banner, value, "Banner"); }

        protected IImage _ImageIcon;
        public IImage ImageIcon { get => _ImageIcon; set => this.RaiseAndSetIfChanged<Game, IImage>(ref _ImageIcon, value, "ImageIcon"); }

        protected string _ID;
        public string ID { get => GameConfiguration.ID; }

        protected string _DisplayName;
        public string DisplayName { get => GameConfiguration.Name; }

        //private string _Executeable;
        //public string Executeable { get => _Executeable; set => this.RaiseAndSetIfChanged<Game, string>(ref _Executeable, value, "Executeable"); }

        protected string _GameDirectory;
        public string GameDirectory 
        { 
            get => _GameDirectory; 
            set
            {
                if (_GameDirectory != value)
                {
                    _GameDirectory = value;
                    try
                    {
                        this.SetDirectory(value);
                        Configuration.Save();
                    }
                    catch (Exception e)
                    {

                    }
                    this.RaisePropertyChanged<Game>("GameDirectory");
                }
            }
        }

        protected string _ManagedDirectory;
        public string ManagedDirectory { get => _ManagedDirectory; set => this.RaiseAndSetIfChanged<Game, string>(ref _ManagedDirectory, value, "ManagedDirectory"); }

        protected string _DataDirectory;
        public string DataDirectory { get => _DataDirectory; set => this.RaiseAndSetIfChanged<Game, string>(ref _DataDirectory, value, "DataDirectory"); }

        protected UnityMonoGameInfo _GameInfo;
        public UnityMonoGameInfo GameInfo { get => _GameInfo; set => this.RaiseAndSetIfChanged<Game, UnityMonoGameInfo>(ref _GameInfo, value, "GameInfo"); }

        protected Backup _Backup;
        public Backup Backup { get => _Backup; set => this.RaiseAndSetIfChanged<Game, Backup>(ref _Backup, value, "Backup"); }

        protected ModLibrary _ModLibrary;
        public ModLibrary ModLibrary { get => _ModLibrary; set => this.RaiseAndSetIfChanged<Game, ModLibrary>(ref _ModLibrary, value, "ModLibrary"); }

        protected bool _IsModded;
        public bool IsModded { get => _IsModded; set => this.RaiseAndSetIfChanged<Game, bool>(ref _IsModded, value, "IsModded"); }

        protected bool _IsModable;
        public bool IsModable { get => _IsModable; set => this.RaiseAndSetIfChanged<Game, bool>(ref _IsModable, value, "IsModable"); }

        protected ObservableCollection<Library> _ManagedLibraries = null;
        public ObservableCollection<Library> ManagedLibraries { get => _ManagedLibraries; set => this.RaiseAndSetIfChanged<Game, ObservableCollection<Library>>(ref _ManagedLibraries, value, "ManagedLibraries"); }

        protected ObservableCollection<ModProject.ModProject> _ModProjects = null;
        public ObservableCollection<ModProject.ModProject> ModProjects { get => _ModProjects; set => this.RaiseAndSetIfChanged<Game, ObservableCollection<ModProject.ModProject>>(ref _ModProjects, value, "ModProjects"); }

        protected ObservableCollection<Mod> _Mods = null;
        public ObservableCollection<Mod> Mods { get => _Mods; set => this.RaiseAndSetIfChanged<Game, ObservableCollection<Mod>>(ref _Mods, value, "Mods"); }

        public void SetConfiguration(JObject config)
        {
            if (config.ContainsKey("path"))
                GameDirectory = config["path"].ToString();
        }

        protected bool _Loaded = false;
        public bool Loaded { get => _Loaded; set => this.RaiseAndSetIfChanged<Game, bool>(ref _Loaded, value); }
        public JObject GetConfiguration()
        {
            JObject ret = new JObject();
            ret["path"] = GameDirectory;
            var modsObjects = new JObject();
            if (Mods != null)
            {
                foreach (var mod in Mods)
                {
                    modsObjects[Path.GetFileNameWithoutExtension(mod.File)] = mod.Configuration.ToJSON();
                }
            }
            ret["mods"] = modsObjects;
            return ret;
        }

        public virtual void CheckIfIsModded()
        {
            var isModded = false;
            foreach (var library in ManagedLibraries)
            {
                if (library.IsModded)
                {
                    isModded = true;
                    break;
                }
            }
            if (isModded != _IsModded)
                IsModded = isModded;
        }

        protected Configuration.Game GameConfiguration;

        public Game(Configuration.Game gameConfiguration)//string id, string name, string executeable)
        {
            var tabs = new List<GameTab>();
            tabs.Add(new GameSetupTab(this));
            tabs.Add(new GameModsTab(this));
            tabs.Add(new GameModProjectsTab(this));

            Tabs = tabs;

            GameConfiguration = gameConfiguration;
            this.PropertyChanged += GameViewModel_PropertyChanged;
            LoadIcon();
        }

        public void Load()
        {
            if (!Loaded)
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    var config = Configuration.GetGameConfiguration(ID);
                    if (config != null)
                        this.SetConfiguration(config);
                    else
                    {
                        var autoPath = GameConfiguration.FindGamePath();
                        if (autoPath != null && this.GameDirectory != autoPath)
                            this.GameDirectory = autoPath;
                    }

                    Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        LoadBanner();
                        Loaded = true;
                    });
                });
            }
        }

        private void LoadIcon()
        {
            var iconFile = $"Games/{this.GameConfiguration.ID}/icon.png";
            if (System.IO.File.Exists(iconFile))
                ImageIcon = new Bitmap(iconFile);
        }

        private void LoadBanner()
        {
            var bannerFile = $"Games/{this.GameConfiguration.ID}/banner.png";
            if (System.IO.File.Exists(bannerFile))
                Banner = new Bitmap(bannerFile);
        }

        public virtual void SetDirectory(string directory)
        {
        }

        public Mod CreateMod(string fileName)
        {
            try
            {
                var mod = new Mod(this, fileName);
                var id = Path.GetFileNameWithoutExtension(fileName);
                var gameConfig = Configuration.GetGameConfiguration(this.ID);
                if (gameConfig != null && gameConfig.ContainsKey("mods") && gameConfig["mods"] is JObject modsConfig && modsConfig.ContainsKey(id) && modsConfig[id] is JObject modConfig)
                    mod.SetJSON(modConfig);
                return mod;
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while loading mod at " + fileName);
            }
            return null;
        }

        protected void UnregisterWatchers()
        {
            if (ProjectsWatcher != null)
                ProjectsWatcher.Dispose();
        }
        protected void RegisterWatchers()
        {
            var modProjectsDirectory = Path.Combine(GameDirectory, "ModAPI", "Projects");
            ProjectsWatcher = new FileSystemWatcher(modProjectsDirectory);

            ProjectsWatcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite;

            ProjectsWatcher.Deleted += (object sender, FileSystemEventArgs e) => {
                for (var i = 0; i < ModProjects.Count; i++)
                {
                    var project = ModProjects[i];
                    if (Path.GetFullPath(project.Directory) == Path.GetFullPath(e.FullPath))
                    {
                        ModProjects.RemoveAt(i);
                        break;
                    }
                }
            };
            ProjectsWatcher.Renamed += (object sender, RenamedEventArgs e) => {
                try
                {
                    var attributes = System.IO.File.GetAttributes(e.FullPath);
                    if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        for (var i = 0; i < ModProjects.Count; i++)
                        {
                            var project = ModProjects[i];
                            if (Path.GetFullPath(project.Directory) == Path.GetFullPath(e.OldFullPath))
                            {
                                project.Directory = e.FullPath;
                            }
                        }
                    }
                }
                catch (Exception) { }
            };
            
            ProjectsWatcher.IncludeSubdirectories = true;
            ProjectsWatcher.EnableRaisingEvents = true;

            var modsDirectory = Path.Combine(GameDirectory, "ModAPI", "Mods");
            ModsWatcher = new FileSystemWatcher(modsDirectory);

            ModsWatcher.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite;

            ModsWatcher.Created += (object sender, FileSystemEventArgs e) => {
                try
                {
                    var newMod = CreateMod(e.FullPath);
                    if (newMod != null)
                        Mods.Add(newMod);
                }
                catch (Exception)
                {

                }
            };
            ModsWatcher.Changed += (object sender, FileSystemEventArgs e) => {
                for (var i = 0; i < Mods.Count; i++)
                {
                    var mod = Mods[i];
                    if (Path.GetFullPath(mod.File) == Path.GetFullPath(e.FullPath))
                    {
                        try
                        {
                            Mods[i].Load();
                        }
                        catch (Exception)
                        {
                            Mods.RemoveAt(i);
                        }
                        break;
                    }
                }
            };
            ModsWatcher.Deleted += (object sender, FileSystemEventArgs e) => {
                for (var i = 0; i < Mods.Count; i++)
                {
                    var mod = Mods[i];
                    if (Path.GetFullPath(mod.File) == Path.GetFullPath(e.FullPath))
                    {
                        Mods.RemoveAt(i);
                        break;
                    }
                }
            };
            ModsWatcher.Renamed += (object sender, RenamedEventArgs e) => {
                for (var i = 0; i < Mods.Count; i++)
                {
                    var mod = Mods[i];
                    if (Path.GetFullPath(mod.File) == Path.GetFullPath(e.OldFullPath))
                    {
                        mod.File = e.FullPath;
                        var id = Path.GetFileNameWithoutExtension(e.FullPath);
                        var gameConfig = Configuration.GetGameConfiguration(this.ID);
                        if (gameConfig != null && gameConfig.ContainsKey("mods") && gameConfig["mods"] is JObject modsConfig && modsConfig.ContainsKey(id) && modsConfig[id] is JObject modConfig)
                            mod.SetJSON(modConfig);
                    }
                }
            };

            ModsWatcher.IncludeSubdirectories = false;
            ModsWatcher.EnableRaisingEvents = true;
        }

        public virtual void CheckIfModable()
        {
            IsModable = ModLibrary != null && ModLibrary.IsUpToDate && Backup != null && Backup.IsUpToDate;
        }
    }
}
