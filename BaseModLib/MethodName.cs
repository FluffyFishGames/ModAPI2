using System;
using System.Collections.Generic;
using System.Text;

namespace ModAPI
{
    /**
     * Only used for mod library
     */
    internal class MethodName : System.Attribute
    {
        private string Name;
        public MethodName(string propertyName)
        {
            Name = propertyName;
        }
    }
}
