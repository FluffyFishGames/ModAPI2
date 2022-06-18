using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;
using Gameloop.Vdf.Linq;
using Gameloop.Vdf;
#if !(MACOS || LINUX)
using Microsoft.Win32;
#endif

namespace ModAPI.Models
{
    public static class Configuration
    {
        private static NLog.ILogger Logger = NLog.LogManager.GetLogger("Configuration");
        public static Dictionary<string, Game> Games = new Dictionary<string, Game>();

        static Configuration()
        {
            if (File.Exists("Games.json"))
            {
                var games = JObject.Parse(File.ReadAllText("Games.json"));
                foreach (var game in games)
                {
                    if (game.Value is JObject j)
                        Games.Add(game.Key, new Game(game.Key, j));
                }
            }
            else Logger.Warn("Games.json does not exist.");
        }
        
        public class Game
        {
            public enum TypeEnum
            {
                Unity
            };

            public string ID;
            public string Name;
            public TypeEnum Type;
            public List<string> Libraries;
            public List<GameLocator> GameLocators;

            public Game(string id, JObject gameConfiguration)
            {
                ID = id;
                Name = gameConfiguration["name"]?.ToString() ?? "Unknown";
                Type = gameConfiguration.ContainsKey("type") ? Enum.Parse<TypeEnum>(gameConfiguration["type"].ToString()) : TypeEnum.Unity;
                Libraries = new List<string>();
                if (gameConfiguration.ContainsKey("libraries") && gameConfiguration["libraries"] is JArray libs)
                {
                    foreach (var lib in libs)
                        Libraries.Add(lib.ToString());
                }
                GameLocators = new List<GameLocator>();
                if (gameConfiguration.ContainsKey("steam") && gameConfiguration["steam"] is JObject steamObj)
                {
                    GameLocators.Add(new SteamGameLocator()
                    {
                        AppID = steamObj["app_id"].ToString()
                    });
                }
            }

            public bool IsInLibraries(string libraryName)
            {
                foreach (var lib in Libraries)
                {
                    if (libraryName == lib ||
                        (lib.EndsWith("*") && libraryName.StartsWith(lib.Substring(0, lib.Length - 1)) ||
                        (lib.StartsWith("*") && libraryName.EndsWith(lib.Substring(1))))
                        return true;
                }
                return false;
            }

            public string FindGamePath()
            {
                foreach (var locator in GameLocators)
                {
                    var path = locator.FindGamePath();
                    if (path != null)
                        return path;
                }
                return null;
            }
        }

        public class GameLocator
        {
            public virtual string FindGamePath()
            {
                return null;
            }
        }

        public class SteamGameLocator : GameLocator
        {
            public string AppID;
            public override string FindGamePath()
            {
#if MACOS
                string steamDirectory = System.IO.Path.GetFullPath("~/Library/Application Support/Steam/");
                return AnalyzeSteamManifest(steamDirectory);
#elif LINUX
                string steamDirectory = System.IO.Path.GetFullPath(System.Environment.GetEnvironmentVariable("HOME") + "/.local/share/Steam");
                return AnalyzeSteamManifest(steamDirectory);
#else
                string steamDirectory = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null);
                if (steamDirectory != null)
                    return AnalyzeSteamManifest(steamDirectory);
                return null;
#endif
            }

            private string AnalyzeSteamManifest(string steamDirectory)
            {
                var libraryFile = System.IO.Path.Combine(steamDirectory, "steamapps", "libraryfolders.vdf");
                if (System.IO.File.Exists(libraryFile))
                {
                    VProperty folders = VdfConvert.Deserialize(System.IO.File.ReadAllText(libraryFile));
                    foreach (var kv in folders.Value as VObject)
                    {
                        if (kv.Value is Gameloop.Vdf.Linq.VObject obj)
                        {
                            if (kv.Value["path"] != null)
                            {
                                var path = kv.Value["path"].ToString();
                                if (FindGame(path, out var gamePath))
                                {
                                    return gamePath;
                                }
                            }
                        }
                    }
                }
                return null;
            }

            private bool FindGame(string path, out string gamePath)
            {
                var manifestFile = System.IO.Path.Combine(path, "steamapps", "appmanifest_" + AppID + ".acf");
                if (System.IO.File.Exists(manifestFile))
                {
                    VProperty manifest = VdfConvert.Deserialize(System.IO.File.ReadAllText(manifestFile));
                    if (manifest.Value is VObject mObj)
                    {
                        if (mObj["installdir"] != null)
                        {
                            gamePath = System.IO.Path.Combine(path, "steamapps", "common", mObj["installdir"].ToString());
                            return true;
                        }
                    }
                }
                gamePath = null;
                return false;
            }
        }
    }
}
