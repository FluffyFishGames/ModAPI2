using ModAPI.ViewModels;
using ModAPI.ViewModels.ModProject;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI.Utils
{
    internal partial class ModCreator
    {
        private class PrivateContext : Context
        {
            public ModProject Project;
            public ProgressHandler ProgressHandler;
            public Dictionary<string, AssemblyDefinition> Assemblies = new Dictionary<string, AssemblyDefinition>();
            public Dictionary<string, TypeDefinition> AllTypes = new Dictionary<string, TypeDefinition>();
            public Dictionary<string, MonoHelper.Delegate> Delegates = new Dictionary<string, MonoHelper.Delegate>();
            public Dictionary<string, MethodDefinition> AllMethods = new Dictionary<string, MethodDefinition>();
            public Dictionary<string, PropertyDefinition> AllProperties = new Dictionary<string, PropertyDefinition>();
            public AssemblyDefinition BaseModLib;
            public Dictionary<string, TypeDefinition> BaseModLibTypes = new Dictionary<string, TypeDefinition>();
            public Dictionary<string, MethodDefinition> AttributeConstructors = new Dictionary<string, MethodDefinition>();
            public TypeDefinition InjectionTypeType;
            public const int InjectionChain = 0;
            public const int InjectionHookBefore = 1;
            public const int InjectionHookAfter = 2;
            public const int InjectionReplace = 3;
            private static List<string> BaseModLibAttributes = new List<string>() { "ModAPI.ExecuteOnApplicationQuit", "ModAPI.ExecuteOnApplicationStart", "ModAPI.ExecuteOnFixedUpdate", "ModAPI.ExecuteOnLateUpdate", "ModAPI.ExecuteOnLevelLoad", "ModAPI.ExecuteOnUpdate", "ModAPI.Injection", "ModAPI.ModAPI", "ModAPI.Priority" };

            public PrivateContext(Context context)
            {
                Project = context.Project;
                ProgressHandler = context.ProgressHandler;
            }

            public void InitializeBaseModLib()
            {
                foreach (var type in BaseModLib.MainModule.Types)
                {
                    BaseModLibTypes.Add(type.FullName, type);
                }
                foreach (var attribute in BaseModLibAttributes)
                {
                    if (!BaseModLibTypes.ContainsKey(attribute))
                        throw new Exception("BaseModLib is incomplete. Reinstall ModAPI.");
                    foreach (var m in BaseModLibTypes[attribute].Methods)
                    {
                        if (m.IsConstructor)
                            AttributeConstructors[attribute] = m;
                    }
                }
                foreach (var nestedType in BaseModLibTypes["ModAPI.Injection"].NestedTypes)
                {
                    if (nestedType.Name == "Type")
                    {
                        InjectionTypeType = nestedType;
                        break;
                    }
                }
                if (InjectionTypeType == null)
                    throw new Exception("BaseModLib is incomplete. Reinstall ModAPI.");
            }

            public List<MethodDefinition> FindBaseMethods(string typeName, string methodName)
            {
                var ret = new List<MethodDefinition>();
                while (typeName != null)
                {
                    if (AllTypes.ContainsKey(typeName))
                    {
                        var baseType = AllTypes[typeName];
                        foreach (var m in baseType.Methods)
                        {
                            if (m.Name == methodName)
                                ret.Add(m);
                        }
                        if (baseType.BaseType != null)
                            typeName = baseType.BaseType.FullName;
                        else
                            break;
                    }
                    else break;
                }
                return ret;
            }

            public List<string> GetAllAssignableTypes(TypeDefinition type)
            {
                List<string> ret = new List<string>();
                ret.Add(type.FullName);
                foreach (var @interface in type.Interfaces)
                {
                    if (AllTypes.ContainsKey(@interface.InterfaceType.FullName))
                        GetAllAssignableTypes(AllTypes[@interface.InterfaceType.FullName]);
                    else ret.Add(@interface.InterfaceType.FullName);
                }
                if (type.BaseType != null)
                {
                    if (AllTypes.ContainsKey(type.BaseType.FullName))
                        GetAllAssignableTypes(AllTypes[type.BaseType.FullName]);
                    else ret.Add(type.BaseType.FullName);
                }
                return ret;
            }
        }

    }
}
