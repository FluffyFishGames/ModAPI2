using ModAPI.ViewModels;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI.Utils
{
    internal partial class ModApplier
    {

        private class PrivateContext : Context
        {
            public Game Game;
            public List<Mod> Mods;
            public ProgressHandler ProgressHandler;
            public AssemblyDefinition BaseModLib;
            public Dictionary<string, AssemblyDefinition> ModAssemblies = new();
            public Dictionary<string, AssemblyDefinition> ModLibraryAssemblies = new();
            public Dictionary<string, AssemblyDefinition> Assemblies = new();
            public Dictionary<string, TypeDefinition> AllTypes = new();
            public Dictionary<string, MethodDefinition> AllMethods = new();
            public Dictionary<string, PropertyDefinition> AllProperties = new();
            public Dictionary<string, FieldDefinition> AllFields = new();
            public Dictionary<string, TypeDefinition> ModLibraryTypes = new();
            public Dictionary<string, MethodDefinition> ModLibraryMethods = new();
            public Dictionary<string, PropertyDefinition> ModLibraryProperties = new();
            public Dictionary<string, FieldDefinition> ModLibraryFields = new();
            public Dictionary<string, TypeDefinition> BaseModLibTypes = new();
            public Dictionary<string, MethodDefinition> AttributeConstructors = new();
            public Dictionary<string, PropertyDefinition> FieldToProperty = new();
            public Dictionary<string, Dictionary<Injection.Type, List<MethodDefinition>>> MethodReplaces = new();
            public Dictionary<string, MethodDefinition> NewMethods = new Dictionary<string, MethodDefinition>();
            public Dictionary<string, Replacement> Replacements = new();
            public Dictionary<string, MonoHelper.Delegate> Delegates = new();
            public HashSet<string> CompleteProperties = new HashSet<string>();
            public HashSet<string> AccessedFields = new HashSet<string>();
            public HashSet<string> AccessedMethods = new HashSet<string>();

            public Dictionary<string, MethodReference> Test = new();

            public TypeDefinition InjectionTypeType;
            private static List<string> BaseModLibAttributes = new List<string>() { "ModAPI.ExecuteOnApplicationQuit", "ModAPI.ExecuteOnApplicationStart", "ModAPI.ExecuteOnFixedUpdate", "ModAPI.ExecuteOnLateUpdate", "ModAPI.ExecuteOnLevelLoad", "ModAPI.ExecuteOnUpdate", "ModAPI.Injection", "ModAPI.ModAPI", "ModAPI.Priority" };

            public PrivateContext(Context context)
            {
                Mods = context.Mods;
                ProgressHandler = context.ProgressHandler;
                Game = context.Game;
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
                        typeName = baseType.BaseType.FullName;
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
