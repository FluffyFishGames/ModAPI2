using System;
using System.Collections.Generic;
using System.Text;

namespace BaseModLib
{
    /**
     * Decorates your method to be run when the application is loaded.
     * 
     * Treats the method as if it is static. So use static or don't use this without null check.
     */
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ExecuteOnApplicationStart : System.Attribute
    {
        public ExecuteOnApplicationStart()
        {

        }
    }
}
