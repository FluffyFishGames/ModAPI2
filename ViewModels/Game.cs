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
        private FileSystemWatcher ProjectsWatcher;
        private FileSystemWatcher ModsWatcher;
        private static NLog.Logger Logger = NLog.LogManager.GetLogger("Game");
        private List<string> ForbiddenAssemblies = new List<string>() { };// "Steamworks.NET" };

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

        private ViewModelBase _Page;
        public ViewModelBase Page { get => _Page; set => this.RaiseAndSetIfChanged<Game, ViewModelBase>(ref _Page, value, "Page"); }

        private ModProject.ModProject _SelectedProject;
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

        private Mod _SelectedMod;
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
        private IImage _ImageIcon;
        public IImage ImageIcon 
        { 
            get 
            { 
                if (_ImageIcon == null)
                {
                    var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                    var bitmap = new Bitmap(assets.Open(new Uri("avares://ModAPI2/Resources/" + ID + "/icon.png")));
                    _ImageIcon = bitmap;
                }
                return _ImageIcon;
            } 
        }

        private IImage _Banner;
        public IImage Banner 
        { 
            get 
            {
                if (_Banner == null)
                {
                    var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                    var bitmap = new Bitmap(assets.Open(new Uri("avares://ModAPI2/Resources/" + ID + "/banner.png")));
                    _Banner = bitmap;
                }
                return _Banner;
            } 
        }

        private string _ID;
        public string ID { get => _ID; set => this.RaiseAndSetIfChanged<Game, string>(ref _ID, value, "ID"); }

        private string _DisplayName;
        public string DisplayName { get => _DisplayName; set => this.RaiseAndSetIfChanged<Game, string>(ref _DisplayName, value, "DisplayName"); }

        private string _Executeable;
        public string Executeable { get => _Executeable; set => this.RaiseAndSetIfChanged<Game, string>(ref _Executeable, value, "Executeable"); }

        private string _GameDirectory;
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

        private string _ManagedDirectory;
        public string ManagedDirectory { get => _ManagedDirectory; set => this.RaiseAndSetIfChanged<Game, string>(ref _ManagedDirectory, value, "ManagedDirectory"); }

        private string _DataDirectory;
        public string DataDirectory { get => _DataDirectory; set => this.RaiseAndSetIfChanged<Game, string>(ref _DataDirectory, value, "DataDirectory"); }

        private GameInfo _GameInfo;
        public GameInfo GameInfo { get => _GameInfo; set => this.RaiseAndSetIfChanged<Game, GameInfo>(ref _GameInfo, value, "GameInfo"); }

        private Backup _Backup;
        public Backup Backup { get => _Backup; set => this.RaiseAndSetIfChanged<Game, Backup>(ref _Backup, value, "Backup"); }

        private ModLibrary _ModLibrary;
        public ModLibrary ModLibrary { get => _ModLibrary; set => this.RaiseAndSetIfChanged<Game, ModLibrary>(ref _ModLibrary, value, "ModLibrary"); }

        private bool _IsModded;
        public bool IsModded { get => _IsModded; set => this.RaiseAndSetIfChanged<Game, bool>(ref _IsModded, value, "IsModded"); }

        private bool _IsModable;
        public bool IsModable { get => _IsModable; set => this.RaiseAndSetIfChanged<Game, bool>(ref _IsModable, value, "IsModable"); }

        private ObservableCollection<Library> _ManagedLibraries = null;
        public ObservableCollection<Library> ManagedLibraries { get => _ManagedLibraries; set => this.RaiseAndSetIfChanged<Game, ObservableCollection<Library>>(ref _ManagedLibraries, value, "ManagedLibraries"); }

        private ObservableCollection<ModProject.ModProject> _ModProjects = null;
        public ObservableCollection<ModProject.ModProject> ModProjects { get => _ModProjects; set => this.RaiseAndSetIfChanged<Game, ObservableCollection<ModProject.ModProject>>(ref _ModProjects, value, "ModProjects"); }

        private ObservableCollection<Mod> _Mods = null;
        public ObservableCollection<Mod> Mods { get => _Mods; set => this.RaiseAndSetIfChanged<Game, ObservableCollection<Mod>>(ref _Mods, value, "Mods"); }

        public void SetConfiguration(JObject config)
        {
            if (config.ContainsKey("path"))
                GameDirectory = config["path"].ToString();
        }

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

        public void CheckIfIsModded()
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

        public Game(string id, string name, string executeable)
        {
            ID = id;
            DisplayName = name;
            Executeable = executeable; 
            
            this.PropertyChanged += GameViewModel_PropertyChanged;

            var tabs = new List<GameTab>();
            tabs.Add(new GameSetupTab(this));
            tabs.Add(new GameModsTab(this));
            tabs.Add(new GameModProjectsTab(this));
            Tabs = tabs;

            var config = Configuration.GetGameConfiguration(ID);
            if (config != null)
                this.SetConfiguration(config);
        }

        public void SetDirectory(string directory)
        {
            ModLibrary = null;
            ManagedDirectory = null;
            DataDirectory = null;
            GameInfo = null;
            Backup = null;
            ManagedLibraries = null;

            UnregisterWatchers();

            var gameDirectory = Path.GetFullPath(directory);
            Logger.Debug("Loading game at \"" + gameDirectory + "\"!");
            if (!System.IO.Directory.Exists(gameDirectory))
                throw new ArgumentException("Provided directory doesn't exist.");
            if (_Executeable != null)
            {
                var exec = System.IO.Path.Combine(gameDirectory, _Executeable);
                if (System.IO.File.Exists(exec))
                {
                    Logger.Trace("Checking file \"" + _Executeable + "\"");
                    FindGame(gameDirectory, Path.GetFileNameWithoutExtension(_Executeable));
                }
                else throw new ArgumentException("Game not found at \"" + Path.GetFullPath(gameDirectory) + "\"");
            }
            else
            {
                var files = Directory.GetFiles(gameDirectory);
                foreach (var file in files)
                {
                    if (Path.GetExtension(file).ToLowerInvariant() == ".exe")
                    {
                        Logger.Trace("Checking file \"" + Path.GetFullPath(file) + "\"");
                        FindGame(gameDirectory, Path.GetFileNameWithoutExtension(file));
                        if (DisplayName != null)
                        {
                            Logger.Trace("Found game for file \"" + Path.GetFullPath(file) + "\"");
                            break;
                        }
                    }
                }
            }
            if (DisplayName == null)
                throw new ArgumentException("Provided directory doesn't contain a valid game.");

            Logger.Trace("Finding managed libraries...");
            FindManagedLibraries();

            Logger.Trace("Getting game information...");
            GameDirectory = gameDirectory;
            GameInfo = new GameInfo(this);
            Backup = new Backup(this);
            ModLibrary = new ModLibrary(this);

            CheckIfModable();

            var modProjects = new ObservableCollection<ModProject.ModProject>();
            var modProjectsDirectory = Path.Combine(GameDirectory, "ModAPI", "Projects");
            if (!Directory.Exists(modProjectsDirectory))
                Directory.CreateDirectory(modProjectsDirectory);

            var modProjectsFolders = Directory.GetDirectories(modProjectsDirectory);
            foreach (var modProjectFolder in modProjectsFolders)
            {
                try
                {
                    modProjects.Add(new ModProject.ModProject(this, modProjectFolder));
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error while loading project at " + modProjectFolder);
                }
            }

            ModProjects = modProjects;
            var modsDirectory = Path.Combine(GameDirectory, "ModAPI", "Mods");
            if (!Directory.Exists(modsDirectory))
                Directory.CreateDirectory(modsDirectory);
            var modFiles = Directory.GetFiles(modsDirectory);
            var mods = new ObservableCollection<Mod>();
            foreach (var modFile in modFiles)
            {
                var mod = CreateMod(modFile);
                if (mod != null)
                    mods.Add(mod);
            }
            Mods = mods;
            
            RegisterWatchers();
            Logger.Debug("Game loaded successfully!");
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

        private void UnregisterWatchers()
        {
            if (ProjectsWatcher != null)
                ProjectsWatcher.Dispose();
        }
        private void RegisterWatchers()
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

        public void CheckIfModable()
        {
            IsModable = ModLibrary != null && ModLibrary.IsUpToDate && Backup != null && Backup.IsUpToDate;
        }

        private void FindManagedLibraries()
        {
            var managedLibraries = new ObservableCollection<Library>();
            if (ManagedDirectory != null)
            {
                var files = Directory.GetFiles(ManagedDirectory);
                foreach (var file in files)
                {
                    if (Path.GetExtension(file).ToLowerInvariant() == ".dll")
                    {
                        var assemblyName = Path.GetFileNameWithoutExtension(file);
                        if (!ForbiddenAssemblies.Contains(assemblyName)) // || assemblyName.StartsWith("Unity")
                        {
                            managedLibraries.Add(new Library(this, Path.Combine(ManagedDirectory, assemblyName + ".dll")));
                        }
                    }
                }
            }
            ManagedLibraries = managedLibraries;
        }

        private void FindGame(string gameDirectory, string gameName)
        {
            var dataDirectory = new DirectoryInfo(Path.Combine(Path.GetFullPath(gameDirectory), gameName + "_Data"));
            var managedDirectory = new DirectoryInfo(Path.Combine(dataDirectory.FullName, "Managed"));
            if (dataDirectory.Exists && managedDirectory.Exists)
            {
                DataDirectory = dataDirectory.FullName;
                ManagedDirectory = managedDirectory.FullName;
                if (Executeable == null)
                {
                    DisplayName = gameName;
                }
            }
        }
    }
}
