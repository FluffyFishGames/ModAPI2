using ModAPI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;

namespace ModAPI
{
    public partial class Configuration
    {
        private static NLog.ILogger Logger = NLog.LogManager.GetLogger("Configuration");
        private static bool Loading = false;
        private static JObject ConfigObject;
        public static Dictionary<string, Configuration.Game> Games = new Dictionary<string, Configuration.Game>();

        static Configuration()
        {
            var configFile = Path.Combine(DataDirectory, "config.json");
            Loading = true;
            if (File.Exists(configFile))
            {
                try
                {
                    ConfigObject = JObject.Parse(File.ReadAllText(configFile));
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error while loading configuration");
                }
            }

            /*Games = new List<Game>()
            {
                new Game("hokkolife", "Hokko Life", "Hokko Life.exe"),
                new Game("sunhaven", "Sun Haven", "Sun Haven.exe")
            };*/

            Loading = false;

            if (File.Exists("Games.json"))
            {
                var games = JObject.Parse(File.ReadAllText("Games.json"));
                foreach (var game in games)
                {
                    if (game.Value is JObject j)
                        Games.Add(game.Key, new Configuration.Game(game.Key, j));
                }
            }
            else Logger.Warn("Games.json does not exist.");
        }

        public static JObject GetGameConfiguration(string id)
        {
            if (ConfigObject.ContainsKey(id) && ConfigObject[id] is JObject gameConfig)
            {
                return gameConfig;
            }
            return null;
        }

        public static void Save()
        {
            if (Loading) return;
            JObject config = new JObject();
            foreach (var game in Games)
            {
                config[game.Key] = App.Instance.Games[game.Key].GetConfiguration();
            }
            var configFile = Path.Combine(DataDirectory, "config.json");
            File.WriteAllText(configFile, config.ToString());
        }

        private static string _DataDirectory;
        public static string DataDirectory
        {
            get
            {
                if (_DataDirectory == null)
                {
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    {
                        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ModAPI");
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);
                        _DataDirectory = path;
                    }
                }
                return _DataDirectory;
            }
        }
    }
}
