using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI.Utils
{
    internal partial class CallStack
    {
        public class CallStackCopyContext
        {
            public Dictionary<string, string> ClassMappings = new();
            public Dictionary<string, string> MethodMappings = new();
            public Dictionary<string, DisplayClass> DisplayClasses = new();
            public Dictionary<string, (MethodDefinition, Instruction[])> Methods = new();
            public Dictionary<string, TypeReference> AddParameters = new();
            public ModLibrary ModLibrary;
            public TypeDefinition Type;
            public ModuleDefinition Module;
            public int HighestDisplayClassNum;
            public int HighestDisplayClassSub;
        }
    }
}
