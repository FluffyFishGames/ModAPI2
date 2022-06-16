using System;
using System.Collections.Generic;
using System.Text;

namespace ModAPI
{
    /**
     * Only used for mod library to indicate properties which were fields previously.
     */
    internal class FormerField : System.Attribute
    {
        private string FieldName;
        public FormerField(string fieldName)
        {
            FieldName = fieldName;
        }
    }
}
