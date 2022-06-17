using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI.Utils
{
    internal partial class CallStack
    {
        public class Method
        {
            public MethodDefinition MethodDefinition;
            public Dictionary<string, ParameterDefinition> AddedParameters;
        }
    }
}
