using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI
{
    public partial class Configuration
    {

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
            public List<string> Executeables;

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
                Executeables = new List<string>();
                if (gameConfiguration.ContainsKey("executeables") && gameConfiguration["executeables"] is JArray execs)
                {
                    foreach (var exec in execs)
                        Executeables.Add(exec.ToString());
                }
                GameLocators = new List<GameLocator>();
                if (gameConfiguration.ContainsKey("steam") && gameConfiguration["steam"] is JObject steamObj && steamObj.ContainsKey("app_id"))
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
                        (lib.EndsWith("*") && libraryName.StartsWith(lib.Substring(0, lib.Length - 1))) ||
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
    }
}
