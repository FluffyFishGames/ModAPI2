using System;
using System.Collections.Generic;
using System.Text;

namespace ModAPI
{
    /**
     * Only use this if necessary. This will make your mod incompatible with other mods overriding the same method.
     * Your mod will try to replace an original method of the game entirely.
     */
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class Injection : System.Attribute
    {
        private string _FieldName;
        public string FieldName
        {
            get => _FieldName;
            set => _FieldName = value;
        }
        private string _PropertyName;
        public string PropertyName
        {
            get => _PropertyName;
            set => _PropertyName = value;
        }
        private string _MethodName;
        public string MethodName
        {
            get => _MethodName;
            set => _MethodName = value;
        }
        private string _TypeName;
        public string TypeName
        {
            get => _TypeName;
            set => _TypeName = value;
        }

        private Type InjectionType;
        public enum Type : int
        {
            Chain = 0,
            HookBefore = 1,
            HookAfter = 2,
            Replace = 3
        }

        public Injection(Type type)
        {
            InjectionType = type;
        }
    }
}
