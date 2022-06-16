using System;
using System.Collections.Generic;
using System.Text;

namespace BaseModLib
{
    /**
     * Only use this if necessary. This will make your mod incompatible with other mods overriding the same method.
     * Your mod will try to replace an original method of the game entirely.
     */
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class Replace : System.Attribute
    {
        public Replace()
        {
        }
    }
}
