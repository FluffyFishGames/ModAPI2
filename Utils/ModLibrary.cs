using ModAPI.ViewModels;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI.Utils
{
    internal class ModLibrary : IDisposable
    {
        /// <summary>
        /// The library this class analyzes
        /// </summary>
        private ViewModels.ModLibrary Library;

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
        /// All required attributes to be found in BaseModLib.dll
        /// </summary>
        private static List<string> BaseModLibAttributes = new List<string>() { "ModAPI.ExecuteOnApplicationQuit", "ModAPI.ExecuteOnApplicationStart", "ModAPI.ExecuteOnFixedUpdate", "ModAPI.ExecuteOnLateUpdate", "ModAPI.ExecuteOnLevelLoad", "ModAPI.ExecuteOnUpdate", "ModAPI.Injection", "ModAPI.ModAPI", "ModAPI.Priority", "ModAPI.Inheritance" };

        public ModLibrary(ViewModels.ModLibrary modLibrary)
        {
            Library = modLibrary;
        }

        /// <summary>
        /// Loads all libraries from the generated mod library and fetches references to all types, methods, delegates and properties
        /// </summary>
        /// <exception cref="FileNotFoundException">Is thrown when the BaseModLib.dll couldn't be found</exception>
        public void Load(ProgressHandler progressHandler = null)
        {
            var libraries = Library.Libraries;
            var baseModLibPath = System.IO.Path.Combine(Library.LibraryDirectory, "BaseModLib.dll");
            if (!File.Exists(baseModLibPath))
                throw new FileNotFoundException($"BaseModLib wasn't found at \"{baseModLibPath}\"");

            var loaded = 1;
            var total = (libraries.Count + 1);
            var setProgress = () =>
            {
                progressHandler.ChangeProgress($"Loading mod library assemblies ({loaded}/{total})...", ((float)(loaded - 1) / (float)(total)));
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
            progressHandler.Finish();
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

        /// <summary>
        /// Resolves a type by given TypeReference
        /// </summary>
        /// <param name="ref">The TypeReference</param>
        /// <returns>TypeDefinition if found</returns>
        public TypeDefinition ResolveType(TypeReference @ref)
        {
            if (AllTypes.ContainsKey(@ref.FullName))
                return AllTypes[@ref.FullName];
            return null;
        }

        /// <summary>
        /// Resolves a type by given name
        /// </summary>
        /// <param name="ref">The name</param>
        /// <returns>TypeDefinition if found</returns>
        public TypeDefinition ResolveType(string name)
        {
            if (AllTypes.ContainsKey(name))
                return AllTypes[name];
            return null;
        }

        /// <summary>
        /// Resolves a type by given TypeReference
        /// </summary>
        /// <param name="ref">The TypeReference</param>
        /// <param name="type">The found TypeDefinition</param>
        /// <returns>If a type was found</returns>
        public bool ResolveType(TypeReference @ref, out TypeDefinition type)
        {
            if (AllTypes.ContainsKey(@ref.FullName))
            {
                type = AllTypes[@ref.FullName];
                return true;
            }
            type = null;
            return false;
        }

        /// <summary>
        /// Resolves a type by given name
        /// </summary>
        /// <param name="ref">The name</param>
        /// <param name="type">The found TypeDefinition</param>
        /// <returns>If a type was found</returns>
        public bool ResolveType(string name, out TypeDefinition type)
        {
            if (AllTypes.ContainsKey(name))
            {
                type = AllTypes[name];
                return true;
            }
            type = null;
            return false;
        }

        /// <summary>
        /// Return all known assignable types of a provided TypeDefinition
        /// </summary>
        /// <param name="type">The TypeDefinition to search assignable types for</param>
        /// <returns>All assignable types as names</returns>
        public List<string> GetAllAssignableTypes(TypeDefinition type)
        {
            List<string> ret = new List<string>();
            ret.Add(type.FullName);
            foreach (var @interface in type.Interfaces)
            {
                if (ResolveType(@interface.InterfaceType.FullName, out var t))
                    GetAllAssignableTypes(t);
                else ret.Add(@interface.InterfaceType.FullName);
            }
            if (type.BaseType != null)
            {
                if (ResolveType(type.BaseType.FullName, out var t))
                    GetAllAssignableTypes(t);
                else ret.Add(type.BaseType.FullName);
            }
            return ret;
        }

        public void Dispose()
        {
            foreach (var assembly in Assemblies)
                assembly.Value.Dispose();
            Assemblies.Clear();
            AllTypes.Clear();
            Delegates.Clear();
            AllMethods.Clear();
            AllProperties.Clear();
            BaseModLib.Dispose();
            BaseModLibTypes.Clear();
            AttributeConstructors.Clear();
            GC.Collect();
        }
    }
}
