using System;
using System.Collections.Generic;
using System.Text;

namespace BaseModLib
{
    /**
     * Defines which priority your method has for daisy chaining overridden methods.
     * Higher values will be called first.
     * If you want to make your mod compatible with others adjusting this might be a good idea.
     */
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class Priority : System.Attribute
    {
        private int _Priority = 0;
        public Priority(int priority)
        {
            _Priority = priority;
        }
    }
}
