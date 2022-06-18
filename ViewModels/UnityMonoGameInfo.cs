using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using System.IO;
using ModAPI.Version;

namespace ModAPI.ViewModels
{
    public class UnityMonoGameInfo : ViewModelBase
    {
        private Game _Game;
        public Game Game { get => _Game; set => this.RaiseAndSetIfChanged<UnityMonoGameInfo, Game>(ref _Game, value, "Game"); }
        private string _Name;
        public string Name { get => _Name; set => this.RaiseAndSetIfChanged<UnityMonoGameInfo, string>(ref _Name, value, "Name"); }

        private string _Developer;
        public string Developer { get => _Developer; set => this.RaiseAndSetIfChanged<UnityMonoGameInfo, string>(ref _Developer, value, "Developer"); }

        private string _Version;
        public string Version { get => _Version; set => this.RaiseAndSetIfChanged<UnityMonoGameInfo, string>(ref _Version, value, "Version"); }

        public UnityVersion _UnityVersion;
        public UnityVersion UnityVersion { get => _UnityVersion; set => this.RaiseAndSetIfChanged<UnityMonoGameInfo, UnityVersion>(ref _UnityVersion, value, "UnityVersion"); }

        public bool _SupportsLegacyInput;
        public bool SupportsLegacyInput { get => _SupportsLegacyInput; set => this.RaiseAndSetIfChanged<UnityMonoGameInfo, bool>(ref _SupportsLegacyInput, value, "SupportsLegacyInput"); }

        public bool _SupportsLegacyIMGUI;
        public bool SupportsLegacyIMGUI { get => _SupportsLegacyIMGUI; set => this.RaiseAndSetIfChanged<UnityMonoGameInfo, bool>(ref _SupportsLegacyIMGUI, value, "SupportsLegacyIMGUI"); }

        public bool _SupportsUI;
        public bool SupportsUI { get => _SupportsUI; set => this.RaiseAndSetIfChanged<UnityMonoGameInfo, bool>(ref _SupportsUI, value, "SupportsUI"); }

        public bool _SupportsTextMeshPro;
        public bool SupportsTextMeshPro { get => _SupportsTextMeshPro; set => this.RaiseAndSetIfChanged<UnityMonoGameInfo, bool>(ref _SupportsTextMeshPro, value, "SupportsTextMeshPro"); }


        private static NLog.Logger Logger = NLog.LogManager.GetLogger("GameInfo");

        public UnityMonoGameInfo(Game game)
        {
            Game = game;

            var gameInfoFile = new FileInfo(Path.Combine(Path.GetFullPath(game.DataDirectory), "app.info"));
            Logger.Debug("Looking for game information at \"" + gameInfoFile.FullName + "\"");
            if (gameInfoFile.Exists)
            {
                var lines = File.ReadAllLines(gameInfoFile.FullName);
                if (lines.Length == 2)
                {
                    Developer = lines[0];
                    Name = lines[1];
                    Logger.Debug("Found game information. Name: " + Name + ". Developer: " + Developer);
                }
            }
            else throw new ArgumentException("Game information couldn't be found for game " + game.DisplayName);

            Version = VersionChecker.FindVersion(Name, new DirectoryInfo(game.GameDirectory));
            UnityVersion = new UnityVersion(game);

            CheckSupport();
            Logger.Debug("Finished looking for game information.");
        }

        private void CheckSupport()
        {
            bool isModular = false;
            bool supportsLegacyInput = false;
            bool supportsLegacyIMGUI = false;
            bool supportsUI = false;
            bool supportsTextMeshPro = false;
            foreach (var library in Game.ManagedLibraries)
            {
                var fileName = Path.GetFileName(library.File);
                if (fileName.StartsWith("Unity") && fileName.EndsWith("Module.dll"))
                    isModular = true;
                if (fileName == "UnityEngine.InputLegacyModule.dll")
                    supportsLegacyInput = true;
                else if (fileName == "UnityEngine.IMGUIModule.dll")
                    supportsLegacyIMGUI = true;
                else if (fileName == "UnityEngine.UIModule.dll")
                    supportsUI = true;
                else if (fileName == "Unity.TextMeshPro.dll")
                    supportsTextMeshPro = true;
            }
            if (!isModular)
            {
                supportsLegacyInput = true;
                supportsLegacyIMGUI = true;
                supportsUI = false;
                SupportsTextMeshPro = false;
            }

            SupportsLegacyInput = supportsLegacyInput;
            SupportsLegacyIMGUI = supportsLegacyIMGUI;
            SupportsUI = supportsUI;
            SupportsTextMeshPro = supportsTextMeshPro;
        }
    }
}
