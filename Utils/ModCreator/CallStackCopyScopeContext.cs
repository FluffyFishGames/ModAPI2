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
        private class CallStackCopyScopeContext
        {
            public enum TypeEnum
            {
                DISPLAY_CLASS,
                METHOD
            }
            public TypeEnum Type;
            public DisplayClass DisplayClass;
            public MethodDefinition Method;
            public ParameterDefinition NumParam;
            public ParameterDefinition ChainParam;
            public CallStackCopyScopeContext Parent;
            public string OriginalName;
        }
    }
}
