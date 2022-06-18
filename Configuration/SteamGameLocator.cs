using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
#if (!LINUX && !MACOS)
using Microsoft.Win32;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI
{
    public partial class Configuration
    {

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
