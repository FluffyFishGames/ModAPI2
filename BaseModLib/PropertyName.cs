using System;
using System.Collections.Generic;
using System.Text;

namespace ModAPI
{
    /**
     * Only used for mod library
     */
    internal class PropertyName : System.Attribute
    {
        private string Name;
        public PropertyName(string propertyName)
        {
            Name = propertyName;
        }
    }
}
