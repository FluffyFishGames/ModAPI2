using ModAPI.Data;
using ModAPI.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI.Version
{
    public class VersionChecker
    {
        private static NLog.Logger Logger = NLog.LogManager.GetLogger("VersionChecker");

        public static string FindVersion(string name, DirectoryInfo directory)
        {
            return "Unknown";
        }
    }
}
