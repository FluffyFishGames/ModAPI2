using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI.ViewModels
{
    public class UnityMonoGame : Game
    {
        public UnityMonoGame(Configuration.Game configuration) : base(configuration)
        {

        }

        public override void SetDirectory(string directory)
        {
            try
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
                if (GameConfiguration.Executeables != null)
                {
                    bool gameFound = false;
                    foreach (var executeable in GameConfiguration.Executeables)
                    {
                        var exec = System.IO.Path.Combine(gameDirectory, executeable + ".exe"); // @TODO checks for linux/mac
                        Logger.Trace("Checking file \"" + exec + "\"");
                        if (System.IO.File.Exists(exec))
                        {
                            if (FindGame(gameDirectory, Path.GetFileNameWithoutExtension(exec)))
                                gameFound = true;
                        }
                    }
                    if (!gameFound)
                        throw new ArgumentException("Game not found at \"" + Path.GetFullPath(gameDirectory) + "\"");
                }
                else
                {
                    var files = Directory.GetFiles(gameDirectory);
                    foreach (var file in files)
                    {
                        if (Path.GetExtension(file).ToLowerInvariant() == ".exe")
                        {
                            Logger.Trace("Checking file \"" + Path.GetFullPath(file) + "\"");
                            if (FindGame(gameDirectory, Path.GetFileNameWithoutExtension(file)))
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
                GameInfo = new UnityMonoGameInfo(this);
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
            catch (Exception ex)
            {
                Logger.Warn(ex, "Error while setting directory");
                var autoPath = GameConfiguration.FindGamePath();
                if (autoPath != null && directory != autoPath)
                    GameDirectory = autoPath;
                else throw;
            }
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

        private bool FindGame(string gameDirectory, string gameName)
        {
            var dataDirectory = new DirectoryInfo(Path.Combine(Path.GetFullPath(gameDirectory), gameName + "_Data"));
            var managedDirectory = new DirectoryInfo(Path.Combine(dataDirectory.FullName, "Managed"));
            if (dataDirectory.Exists && managedDirectory.Exists)
            {
                DataDirectory = dataDirectory.FullName;
                ManagedDirectory = managedDirectory.FullName;
                return true;
            }
            return false;
        }
    }
}
