using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ReactiveUI;
using System.Security.Cryptography;

namespace ModAPI.ViewModels
{
    public class Backup : ViewModelBase
    {
        private static NLog.Logger Logger = NLog.LogManager.GetLogger("Backup");

        private bool _Exists;
        public bool Exists { get => _Exists; private set => this.RaiseAndSetIfChanged<Backup, bool>(ref _Exists, value, "Exists"); }

        private bool _IsUpToDate;
        public bool IsUpToDate { get => _IsUpToDate; private set => this.RaiseAndSetIfChanged<Backup, bool>(ref _IsUpToDate, value, "IsUpToDate"); }

        private string _BackupDirectory;
        public string BackupDirectory { get => _BackupDirectory; private set => this.RaiseAndSetIfChanged<Backup, string>(ref _BackupDirectory, value, "BackupDirectory"); }

        private Game _Game;
        public Game Game { get => _Game; private set => this.RaiseAndSetIfChanged<Backup, Game>(ref _Game, value, "Game"); }

        private Dictionary<string, string> Checksums = new Dictionary<string, string>();

        public string GetChecksum(string fileName)
        {
            if (Checksums.ContainsKey(fileName))
                return Checksums[fileName];
            return null;
        }

        public void Create(ProgressHandler handler)
        {
            if (Game.IsModded)
                throw new InvalidOperationException("Can't create a backup of modified game files.");
            Logger.Debug("Creating backup...");
            handler.ChangeProgress("Creating backup...", 0f);
            var i = 0;
            if (!Directory.Exists(BackupDirectory))
            {
                Logger.Debug("Creating backup directory at " + Path.GetFullPath(BackupDirectory));
                Directory.CreateDirectory(BackupDirectory);
                Logger.Debug("Backup directory successfully created!");
            }
            foreach (var library in Game.ManagedLibraries)
            {
                if (library.IsMod)
                    continue;
                var fileName = Path.Combine(Path.GetFullPath(BackupDirectory), Path.GetFileName(library.File));
                try
                {
                    var originalHash = library.GetOriginalChecksum();
                    Logger.Debug("Backing up file \"" + Path.GetFileName(library.File) + "\"");
                    File.Copy(library.File, fileName, true);
                    Checksums.Add(Path.GetFileName(library.File), originalHash);

                    i++;
                    handler.ChangeProgress(((float)i) / (float)Game.ManagedLibraries.Count);
                }
                catch (IOException e)
                {
                    Logger.Error("There was an error writing the backup file \"" + fileName + "\"", e);
                }
                catch (UnauthorizedAccessException e2)
                {
                    Logger.Error("The user is not authorized to copy the file \"" + library.File + "\" to \"" + fileName + "\". Maybe try launching ModAPI as an administrator?", e2);
                }
            }
            handler.Finish();
        }
        public Backup(Game game)
        {
            Game = game;
            BackupDirectory = Path.Combine(Path.GetFullPath(game.GameDirectory), "ModAPI", "Backup");
            if (Directory.Exists(BackupDirectory))
            {
                Logger.Debug("Checking hashes of backup...");
                var libraries = game.ManagedLibraries;
                var checksumMismatch = false;

                using (SHA256 sha256Hash = SHA256.Create())
                {
                    foreach (var library in libraries)
                    {
                        if (library.IsMod)
                            continue;
                        var backupFile = new FileInfo(Path.Combine(Path.GetFullPath(BackupDirectory), Path.GetFileName(library.File)));
                        if (!backupFile.Exists)
                        {
                            Logger.Debug("Backup of file " + Path.GetFileName(library.File) + " does not exist.");
                            checksumMismatch = true;
                            //return;
                        }
                        else
                        {
                            byte[] data = sha256Hash.ComputeHash(backupFile.OpenRead());
                            var stringBuilder = new StringBuilder();
                            for (int i = 0; i < data.Length; i++)
                            {
                                stringBuilder.Append(data[i].ToString("x2"));
                            }
                            var hash = stringBuilder.ToString();
                            Logger.Debug("Hash of backup of library \"" + Path.GetFileName(library.File) + "\": " + hash);
                            var originalHash = library.GetOriginalChecksum();
                            Logger.Debug("Original hash of library \"" + Path.GetFileName(library.File) + "\": " + originalHash);
                            if (hash.ToLowerInvariant() != originalHash.ToLowerInvariant())
                            {
                                Logger.Debug("Checksum mismatched!");
                                checksumMismatch = true;
                                continue;
                            }
                            else
                            {
                                Checksums.Add(backupFile.Name, hash);
                            }
                        }
                    }
                }
                IsUpToDate = !checksumMismatch;
                if (!IsUpToDate)
                    Checksums.Clear();
                Exists = true;
                Logger.Debug("Finished checking backup. Is up-to-date: " + IsUpToDate + ".");
            }
        }
    }
}
