using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI.Utils
{
    public static class MethodDefinitionExtension
    {
        public static PropertyDefinition GetProperty(this MethodDefinition method)
        {
            if (method.IsGetter || method.IsSetter)
            {
                PropertyDefinition foundProperty = null;
                foreach (var property in method.DeclaringType.Properties)
                {
                    if (property.GetMethod == method || property.SetMethod == method)
                    {
                        return property;
                    }
                }
            }
            return null;
        }
        public static bool GetProperty(this MethodDefinition method, out PropertyDefinition returnProperty)
        {
            if (method.IsGetter || method.IsSetter)
            {
                PropertyDefinition foundProperty = null;
                foreach (var property in method.DeclaringType.Properties)
                {
                    if (property.GetMethod == method || property.SetMethod == method)
                    {
                        returnProperty = property;
                        return true;
                    }
                }
            }
            returnProperty = null;
            return false;
        }
    }
}
