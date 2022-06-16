using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI.Data
{
    public class ModAPI
    {
        public static AssemblyDefinition BaseModLib;
        public static void Initialize()
        {
            var stream = typeof(ModAPI).Assembly.GetManifestResourceStream("ModAPI.BaseModLib.dll");
            BaseModLib = AssemblyDefinition.ReadAssembly(stream);
        }
    }
}
