using ModAPI.ViewModels;
using ModAPI.ViewModels.ModProject;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ModAPI.Utils
{
    internal partial class ModCreator
    {
        private class PrivateContext : Context
        {
            /// <summary>
            /// The project to create a mod from
            /// </summary>
            public ModProject Project;

            /// <summary>
            /// The progress handler (retrieved from provided context)
            /// </summary>
            public ProgressHandler ProgressHandler;

            /// <summary>
            /// Dictionary holding all assemblies of the ModLibrary
            /// </summary>
            public Dictionary<string, AssemblyDefinition> Assemblies = new Dictionary<string, AssemblyDefinition>();

            /// <summary>
            /// Dictionary holding all types found in the ModLibrary assemblies
            /// </summary>
            public Dictionary<string, TypeDefinition> AllTypes = new Dictionary<string, TypeDefinition>();

            /// <summary>
            /// Dictionary holding all (generated) delegates found in the ModLibrary assemblies
            /// </summary>
            public Dictionary<string, MonoHelper.Delegate> Delegates = new Dictionary<string, MonoHelper.Delegate>();

            /// <summary>
            /// Dictionary holding all methods found in the ModLibrary assemblies
            /// </summary>
            public Dictionary<string, MethodDefinition> AllMethods = new Dictionary<string, MethodDefinition>();

            /// <summary>
            /// Dictionary holding all properties found in the ModLibrary assemblies
            /// </summary>
            public Dictionary<string, PropertyDefinition> AllProperties = new Dictionary<string, PropertyDefinition>();

            /// <summary>
            /// The BaseModLib
            /// </summary>
            public AssemblyDefinition BaseModLib;

            /// <summary>
            /// Dictionary holding all types of the BaseModLib
            /// </summary>
            public Dictionary<string, TypeDefinition> BaseModLibTypes = new Dictionary<string, TypeDefinition>();
            
            /// <summary>
            /// Dictionary holding all constructors of attributes types of the BaseModLib
            /// </summary>
            public Dictionary<string, MethodDefinition> AttributeConstructors = new Dictionary<string, MethodDefinition>();

            /// <summary>
            /// The type of the InjectionType enum
            /// </summary>
            public TypeDefinition InjectionTypeType;

            /// <summary>
            /// The value for InjectionType.Chain
            /// </summary>
            public const int InjectionChain = 0;

            /// <summary>
            /// The value for InjectionType.HookBefore
            /// </summary>
            public const int InjectionHookBefore = 1;

            /// <summary>
            /// The value for InjectionType.HookAfter
            /// </summary>
            public const int InjectionHookAfter = 2;

            /// <summary>
            /// The value for InjectionType.Replace
            /// </summary>
            public const int InjectionReplace = 3;

            /// <summary>
            /// All required attributes to be found in BaseModLib.dll
            /// </summary>
            private static List<string> BaseModLibAttributes = new List<string>() { "ModAPI.ExecuteOnApplicationQuit", "ModAPI.ExecuteOnApplicationStart", "ModAPI.ExecuteOnFixedUpdate", "ModAPI.ExecuteOnLateUpdate", "ModAPI.ExecuteOnLevelLoad", "ModAPI.ExecuteOnUpdate", "ModAPI.Injection", "ModAPI.ModAPI", "ModAPI.Priority" };

            /// <summary>
            /// The progress of fetching types and their methods, delegates etc. from the ModLibrary.
            /// </summary>
            private ProgressHandler ModLibraryProgress;
            
            /*/// <summary>
            /// The progress of cha
            /// </summary>
            private ProgressHandler ModProgress;*/

            /// <summary>
            /// PrivateContext will add sub progress handlers to the ProgressHandler received from Context.
            /// </summary>
            /// <param name="context">The provided context</param>
            public PrivateContext(Context context)
            {
                Project = context.Project;
                ProgressHandler = context.ProgressHandler;

                ProgressHandler.AddProgressHandler((ModLibraryProgress = new ProgressHandler()), 0.5f);
            }

            /// <summary>
            /// Loads all libraries from the generated mod library and fetches references to all types, methods, delegates and properties
            /// </summary>
            /// <exception cref="FileNotFoundException">Is thrown when the BaseModLib.dll couldn't be found</exception>
            public void LoadModLibrary()
            {
                var libraries = Project.Game.ModLibrary.Libraries;
                var baseModLibPath = System.IO.Path.Combine(Project.Game.ModLibrary.LibraryDirectory, "BaseModLib.dll");
                if (!File.Exists(baseModLibPath))
                    throw new FileNotFoundException($"BaseModLib wasn't found at \"{baseModLibPath}\"");

                var loaded = 1;
                var total = (libraries.Count + 1);
                var setProgress = () =>
                {
                    ModLibraryProgress.ChangeProgress($"Loading mod library assemblies ({loaded}/{total})...", ((float)(loaded - 1) / (float)(total)));
                };

                setProgress();
                BaseModLib = AssemblyDefinition.ReadAssembly(baseModLibPath);
                InitializeBaseModLib();
                
                loaded++;
                setProgress();

                foreach (var library in libraries)
                {
                    var assembly = library.LoadAssembly();

                    Assemblies[assembly.FullName] = assembly;
                    foreach (var type in assembly.MainModule.Types)
                    {
                        FetchReferences(type);
                    }

                    loaded++;
                    setProgress();
                }
            }

            /// <summary>
            /// Fetches all methods, properties and delegates (which are added by ModAPI) of this type and its nested types
            /// </summary>
            /// <param name="type">The type to fetch information from</param>
            private void FetchReferences(TypeDefinition type)
            {
                if (type.BaseType != null && 
                    type.BaseType.FullName == "System.MulticastDelegate" && 
                    type.HasCustomAttributes && 
                    type.CustomAttributes[0].Constructor.DeclaringType.FullName == "ModAPI.MethodName" && 
                    !this.Delegates.ContainsKey((string)type.CustomAttributes[0].ConstructorArguments[0].Value))
                {
                    this.Delegates.Add((string)type.CustomAttributes[0].ConstructorArguments[0].Value, new MonoHelper.Delegate(type));
                }
                var typeName = type.FullName;
                if (!this.AllTypes.ContainsKey(typeName))
                    this.AllTypes.Add(typeName, type);

                foreach (var nestedType in type.NestedTypes)
                    FetchReferences(nestedType);

                foreach (var method in type.Methods)
                {
                    var name = method.FullName;
                    if (this.AllMethods.ContainsKey(name))
                        this.AllMethods.Add(name, method);
                }
                foreach (var property in type.Properties)
                {
                    var name = property.FullName;
                    if (this.AllProperties.ContainsKey(name))
                        this.AllProperties.Add(name, property);
                }
            }

            /// <summary>
            /// Finds references to important methods and attributes for parsing the mod
            /// </summary>
            /// <exception cref="Exception">Is thrown when there is a problem with the BaseModLib.dll</exception>
            private void InitializeBaseModLib()
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
