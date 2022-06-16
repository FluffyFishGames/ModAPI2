using System;

namespace BaseModLib
{
    /**
     * Defines in which assembly the class you want to override is in.
     * Not necessary for Assembly-CSharp.
     */
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class Assembly : System.Attribute
    {
        public string AssemblyName;

        public Assembly(string assemblyName)
        {
            this.AssemblyName = assemblyName;
        }
    }
}
