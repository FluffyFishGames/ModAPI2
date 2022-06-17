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
        public class CallStackCopyScope
        {
            public enum TypeEnum
            {
                DISPLAY_CLASS,
                METHOD
            }
            public TypeEnum Type;
            public CallStack.DisplayClass DisplayClass;
            public CallStack.Method Method;
            public CallStackCopyScope Parent;
            public string OriginalName;
        }
    }
}
