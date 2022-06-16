using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ReactiveUI;
using ModAPI.Utils;

namespace ModAPI.ViewModels
{
    public class ModLibrary : ViewModelBase
    {
        private static NLog.Logger Logger = NLog.LogManager.GetLogger("ModLibrary");

        private bool _Exists;
        public bool Exists { get => _Exists; set => this.RaiseAndSetIfChanged<ModLibrary, bool>(ref _Exists, value, "Exists"); }

        private bool _IsUpToDate;
        public bool IsUpToDate { get => _IsUpToDate; set => this.RaiseAndSetIfChanged<ModLibrary, bool>(ref _IsUpToDate, value, "IsUpToDate"); }

        private string _LibraryDirectory;
        public string LibraryDirectory { get => _LibraryDirectory; set => this.RaiseAndSetIfChanged<ModLibrary, string>(ref _LibraryDirectory, value, "LibraryDirectory"); }

        private ObservableCollection<Library> _Libraries;
        public ObservableCollection<Library> Libraries { get => _Libraries; set => this.RaiseAndSetIfChanged<ModLibrary, ObservableCollection<Library>>(ref _Libraries, value, "Libraries"); }

        private Game _Game;
        public Game Game { get => _Game; set => this.RaiseAndSetIfChanged<ModLibrary, Game>(ref _Game, value, "Game"); }
        
        public ModLibrary(Game game)
        {
            Game = game;
            LibraryDirectory = Path.Combine(Path.GetFullPath(game.GameDirectory), "ModAPI", "Library");
            Check();
        }

        private void Check()
        {
            bool isUpToDate = true;
            var newLibraries = new ObservableCollection<Library>();
            if (Directory.Exists(LibraryDirectory))
            {
                Logger.Debug("Checking for sanity of modlibrary...");
                var libraries = Game.ManagedLibraries;
                foreach (var library in libraries)
                {
                    if (!library.IsMod)
                    {
                        var libraryFile = Path.Combine(LibraryDirectory, Path.GetFileName(library.File));
                        if (!File.Exists(libraryFile))
                        {
                            isUpToDate = false;
                            continue;
                        }
                        var lib = new Library(Game, libraryFile);
                        if (lib.IsModded && !lib.IsOutdated && lib.GetOriginalChecksum().ToLowerInvariant() == library.GetOriginalChecksum().ToLowerInvariant())
                        {
                            Logger.Debug("Library file \"" + library.File + "\" is valid!");
                            newLibraries.Add(lib);
                        }
                        else
                        {
                            if (!lib.IsModded)
                            {
                                Logger.Debug("Library file \"" + library.File + "\" is not modded or out-of-date. Modlibrary is corrupt and needs recreation.");
                                foreach (var l in newLibraries)
                                    l.Dispose();
                                newLibraries.Clear();
                            }
                            isUpToDate = false;
                        }
                    }
                }
                Logger.Debug("Modlibrary sanity check completed successfully!");
                Exists = true;
                IsUpToDate = isUpToDate;
            }
            Game.CheckIfModable();
            Libraries = newLibraries;
        }

        public void Create(ProgressHandler handler)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var libraryHandler = new ProgressHandler();
                    if (!Game.Backup.Exists || !Game.Backup.IsUpToDate)
                    {
                        var backupHandler = new ProgressHandler();
                        handler.AddProgressHandler(backupHandler, 0.1f);
                        handler.AddProgressHandler(libraryHandler, 0.9f);
                        Game.Backup.Create(backupHandler);
                    }
                    else
                        handler.AddProgressHandler(libraryHandler, 1f);

                    Logger.Debug("Creating modlibrary...");
                    if (!Directory.Exists(LibraryDirectory))
                    {
                        Logger.Debug("Modlibrary directory doesn't exist. Creating it...");
                        Directory.CreateDirectory(LibraryDirectory);
                        Logger.Debug("Modlibrary directory successfully created!");
                    }
                    var context = new ModLibraryCreator.Context();
                    foreach (var library in Game.ManagedLibraries)
                    {
                        if (library.IsMod)
                            continue;
                        var libraryFile = Path.Combine(Game.Backup.BackupDirectory, Path.GetFileName(library.File));
                        if (!System.IO.File.Exists(libraryFile))
                            throw new FileNotFoundException("File \"" + library.File + "\" wasn't found in backup. Can't proceed.");
                        
                        context.AssemblyFiles.Add(new Library(Game, libraryFile));
                    }
                    context.SaveTo = LibraryDirectory;
                    context.AssemblyResolverPath = Game.Backup.BackupDirectory;

                    context.ProgressHandler = libraryHandler;
                    ModLibraryCreator.Execute(context);
                    Check();
                }
                catch (Exception e)
                {
                    handler.Error(e.Message);
                }
            });
        }
    }
}
