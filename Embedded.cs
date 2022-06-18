using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using System.Text.RegularExpressions;

namespace ModAPI
{
    public static class Embedded
    {
        private static NLog.ILogger Logger = NLog.LogManager.GetLogger("Embedded");

        public static void Extract()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (Environment.Is64BitProcess)
                    Extract("ModAPI.libs.tinyfiledialogs64.dll.lz4hc", Path.GetFullPath(Configuration.DataDirectory + "/libs/tinyfiledialogs64.dll"));
                else
                    Extract("ModAPI.libs.tinyfiledialogs32.dll.lz4hc", Path.GetFullPath(Configuration.DataDirectory + "/libs/tinyfiledialogs32.dll"));
            }
            var names = typeof(Embedded).Assembly.GetManifestResourceNames();
            var regex = new Regex("ModAPI\\.Games\\.([^\\.]+)\\.(.*)");
            foreach (var name in names)
            {
                var match = regex.Match(name);
                if (match.Success)
                {
                    Extract(name, "Games/" + match.Groups[1].Value + "/" + match.Groups[2].Value, false);
                }
            }
        }

        public static void Extract(string resourceName, string filePath, bool overwrite = true)
        {
            if (!overwrite && System.IO.File.Exists(filePath))
                return;
            Logger.Info($"Extracting {resourceName}...");
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var assembly = Assembly.GetExecutingAssembly();
                if (resourceName.EndsWith("lz4hc"))
                {
                    using (var file = File.Create(filePath))
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    using (var decoder = LZ4Stream.Decode(stream))
                    {
                        decoder.CopyTo(file);
                    }
                }
                else
                {
                    using (var file = File.Create(filePath))
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        stream.CopyTo(file);
                    }

                }
                Logger.Info($"Extracted resource {resourceName} to {filePath}");
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Error while extracting resource {resourceName}");
            }
        }
    }
}
