using System;
using System.Collections.Generic;
using System.Text;

namespace ModAPI
{
    /**
     * Only use this if necessary. The better way to achieve this functionality is to create your own MonoBehaviour class and add it to the
     * scene via ExecuteOnApplicationStart or ExecuteOnLevelLoad
     * 
     * Treats the method as if it is static. So use static or don't use this without null check.
     */
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ExecuteOnFixedUpdate : System.Attribute
    {
        public ExecuteOnFixedUpdate()
        {

        }
    }
}
