using ModAPI.ViewModels;
using ModAPI.ViewModels.ModProject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using System.IO;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Text.RegularExpressions;

namespace ModAPI.Utils
{
    internal partial class ModCreator
    {
        private static NLog.ILogger Logger = NLog.LogManager.GetLogger("ModCreator");

        public class Input
        {
            public ModProject Project;
            public ProgressHandler ProgressHandler;
        }

        public static void Execute(Input ctxt)
        {
            var context = new Context(ctxt);
            try
            {
                context.LoadModLibrary();
                context.CreateMod();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while creating mod.");
                context.ProgressHandler.Error(ex.Message);
            }
        }
    }
}
