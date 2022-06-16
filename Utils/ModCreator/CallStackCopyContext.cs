using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI.Utils
{
    internal partial class ModCreator
    {
        private class CallStackCopyContext
        {
            public Dictionary<string, string> ClassMappings = new();
            public Dictionary<string, string> MethodMappings = new();
            public Dictionary<string, DisplayClass> DisplayClasses = new();
            public Dictionary<string, MethodDefinition> Methods = new();
            public MonoHelper.Delegate Delegate;
            public int HighestDisplayClassNum;
            public int HighestDisplayClassSub;
        }
    }
}
