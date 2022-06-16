using System;
using System.Collections.Generic;
using System.Text;

namespace BaseModLib
{
    public class ModAPI
    {
        private static string _Directory;
        public static string Directory
        {
            get
            {
                if (_Directory == null)
                    LoadConfiguration();
                return _Directory;
            }
        }

        private static void LoadConfiguration()
        {
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(ModAPI));
            using (var stream = assembly.GetManifestResourceStream("Configuration"))
            {
                var reader = new System.IO.StreamReader(stream);
                var data = reader.ReadToEnd();
                var lines = data.Split(new char[] { '\r', '\n' });
                foreach (var line in lines)
                {
                    var parts = line.Split(new char[] { '=' });
                    if (parts.Length == 2)
                    {
                        var name = parts[0].Trim();
                        var value = parts[1].Trim();
                        if (name == "directory")
                            _Directory = value;
                    }
                }
            }
        }
    }
}
