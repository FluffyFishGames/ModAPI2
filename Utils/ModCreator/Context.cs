using ModAPI.ViewModels;
using ModAPI.ViewModels.ModProject;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Mono.Cecil.Rocks;

namespace ModAPI.Utils
{
    internal partial class ModCreator
    {
        private class Context : Input
        {
            /// <summary>
            /// The project to create a mod from
            /// </summary>
            public ModProject Project;

            /// <summary>
            /// The progress handler (retrieved from provided input)
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

            /// <summary>
            /// The progress of parsing the mod
            /// </summary>
            private ProgressHandler ModProgress;

            /// <summary>
            /// The reader parameters for mod library assemblies
            /// </summary>
            private ReaderParameters ModLibraryReaderParameters;

            /// <summary>
            /// PrivateContext will add sub progress handlers to the ProgressHandler received from input and prepare the project for building.
            /// </summary>
            /// <param name="input">The provided input</param>
            public Context(Input input)
            {
                Project = input.Project;

                // Remove old values
                Project.Configuration.MethodReplaces.Clear();
                Project.Configuration.MethodHookAfter.Clear();
                Project.Configuration.MethodHookBefore.Clear();
                Project.Configuration.MethodChain.Clear();

                // Initialize progress handling
                ProgressHandler = input.ProgressHandler;
                ProgressHandler.AddProgressHandler((ModLibraryProgress = new ProgressHandler()), 0.5f);
                ProgressHandler.AddProgressHandler((ModProgress = new ProgressHandler()), 0.5f);

                // Create assembly resolver
                var resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(Project.Game.ModLibrary.LibraryDirectory);
                ModLibraryReaderParameters = new ReaderParameters()
                {
                    AssemblyResolver = resolver,
                    ReadWrite = true
                };

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
                ModLibraryProgress.Finish();
            }

            public void CreateMod()
            {
                var modAssembly = AssemblyDefinition.ReadAssembly(Path.Combine(Project.Directory, Project.Configuration.Name + ".dll"), this.ModLibraryReaderParameters);
                var total = modAssembly.MainModule.Types.Count;
                var loaded  = 0;
                var setProgress = () =>
                {
                    ModProgress.ChangeProgress($"Loading and transpiling mod library types ({loaded}/{total})...", ((float)(loaded - 1) / (float)(total)) * 0.9f);
                };
                setProgress();
                foreach (var type in modAssembly.MainModule.Types)
                {
                    ParseModType(type);

                    loaded++;
                    setProgress();
                }

                ModProgress.ChangeProgress($"Adding mod configuration...", 0.9f);
                
                for (var i = 0; i < modAssembly.MainModule.Resources.Count; i++)
                {
                    var resource = modAssembly.MainModule.Resources[i];
                    if (resource.ResourceType == ResourceType.Embedded && resource.Name == "ModConfiguration")
                    {
                        Logger.Warn("There is already a resource called ModConfiguration. Removing it.");
                        modAssembly.MainModule.Resources.RemoveAt(i);
                        i--;
                    }
                }

                modAssembly.MainModule.Resources.Add(new EmbeddedResource("ModConfiguration", ManifestResourceAttributes.Public, System.Text.Encoding.UTF8.GetBytes(Project.Configuration.ToJSON(true).ToString())));

                // writing
                ModProgress.ChangeProgress($"Saving mod...", 0.95f);
                modAssembly.Write(Path.Combine(Project.Directory, "Output.dll"));
                
                // unloading
                modAssembly.Dispose();
                foreach (var assembly in Assemblies)
                {
                    assembly.Value.Dispose();
                }
                ModProgress.ChangeProgress($"Saving mod...", 1f);
                ModProgress.Finish();
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

            private TypeDefinition ResolveType(TypeReference @ref)
            {
                if (AllTypes.ContainsKey(@ref.FullName))
                    return AllTypes[@ref.FullName];
                return null;
            }

            private TypeDefinition ResolveType(string name)
            {
                if (AllTypes.ContainsKey(name))
                    return AllTypes[name];
                return null;
            }
            private bool ResolveType(TypeReference @ref, out TypeDefinition type)
            {
                if (AllTypes.ContainsKey(@ref.FullName))
                {
                    type = AllTypes[@ref.FullName];
                    return true;
                }
                type = null;
                return false;
            }

            private bool ResolveType(string name, out TypeDefinition type)
            {
                if (AllTypes.ContainsKey(name))
                {
                    type = AllTypes[name];
                    return true;
                }
                type = null;
                return false;
            }

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

            private MethodDefinition FindBaseMethodAndValidate(TypeDefinition baseType, MethodDefinition method, Injection.Type injectionType, string methodName)
            {
                Logger.Info($"Basic check for {method.FullName}");
                var returnType = method.ReturnType.FullName;

                if (injectionType == Injection.Type.HookBefore && method.ReturnType.FullName != "System.Void")
                {
                    Logger.Warn($"Method {method.FullName} is of injection type HookBefore but has a different return type than System.Void!");
                    return null;
                }
                if (injectionType == Injection.Type.HookAfter)
                {
                    if (method.Parameters.Count == 0)
                    {
                        Logger.Warn($"Method {method.FullName} is of injection type HookAfter but has no parameters!");
                        return null;
                    }
                    else if (!method.Parameters[method.Parameters.Count -1].ParameterType.IsByReference)
                    {
                        Logger.Warn($"Method {method.FullName} is of injection type HookAfter but last parameter is not passed by reference!");
                        return null;
                    }
                    else
                        returnType = method.Parameters[method.Parameters.Count - 1].ParameterType.FullName.Replace("&", "");
                }
                Logger.Info($"Fetching candidates for " + method.FullName);
                var typeName = baseType.FullName;
                var candidates = new List<MethodDefinition>();
                while (typeName != null)
                {
                    if (ResolveType(typeName, out var t))
                    {
                        foreach (var m in t.Methods)
                        {
                            if (m.Name == methodName)
                                candidates.Add(m);
                        }
                        if (t.BaseType != null)
                            typeName = t.BaseType.FullName;
                        else
                            break;
                    }
                    else
                        break;
                }
                Logger.Info($"Found {candidates.Count} candidates for {method.FullName}");

                var assignableTypes = GetAllAssignableTypes(baseType);
                foreach (var candidate in candidates)
                {
                    if (injectionType != Injection.Type.HookBefore && candidate.ReturnType.FullName != returnType)
                    {
                        Logger.Trace($"Return type of candidate {candidate.FullName} is not a match.");
                        continue;
                    }
                    if (candidate.GenericParameters.Count != method.GenericParameters.Count)
                    {
                        Logger.Trace($"Generic parameters of candidate {candidate.FullName} are not a match.");
                        continue;
                    }
                    for (var i = 0; i < candidate.GenericParameters.Count; i++)
                    {
                        if (!candidate.GenericParameters[i].MatchingSignature(method.GenericParameters[i]))
                        {
                            Logger.Trace($"Generic parameters of candidate {candidate.FullName} are not a match.");
                            continue;
                        }
                    }
                    var paramsCount = method.Parameters.Count;
                    // offset for parameter count check
                    var paramsOffset = 0;
                    var paramStart = 0;

                    // in case a modder does not want to use inheritance paradigm for injection
                    if (!candidate.IsStatic && method.IsStatic)
                    {
                        if (method.Parameters.Count == 0)
                        {
                            Logger.Trace($"Candidate {candidate.FullName} wasn't suitable as method is static and has no parameters (so most likely injecting into static method is the goal).");
                            continue;
                        }
                        if (!assignableTypes.Contains(method.Parameters[0].ParameterType.FullName))
                        {
                            Logger.Trace($"Candidate {candidate.FullName} wasn't suitable for first parameter of type {method.Parameters[0].ParameterType.FullName}");
                            continue;
                        }
                        paramsOffset++;
                        paramStart++;
                    }
                    if (injectionType == Injection.Type.HookAfter && candidate.ReturnType.FullName != "System.Void")
                        paramsOffset++;

                    if (candidate.Parameters.Count != paramsCount - paramsOffset)
                    {
                        Logger.Trace($"Parameters of candidate {candidate.FullName} are not a match.");
                        continue;
                    }
                    for (var i = 0; i < candidate.Parameters.Count; i++)
                    {
                        if (!candidate.Parameters[i].MatchingSignature(method.Parameters[i + paramStart]))
                        {
                            Logger.Trace($"Parameters of candidate {candidate.FullName} are not a match.");
                            continue;
                        }
                    }

                    // we got him. It's a match! :)
                    return candidate;
                }
                return null;
            }

            private void ExtractAttributes(MethodDefinition method, out CustomAttribute injectionAttribute, out Injection.Type injectionType, out string methodName, out string typeName, out string fieldName, out string propertyName)
            {
                injectionType = Injection.Type.Chain;
                methodName = null;
                typeName = null;
                fieldName = null;
                propertyName = null;
                injectionAttribute = method.CustomAttributes.First(a => a.AttributeType.FullName == "ModAPI.Injection");
                if (injectionAttribute != null)
                {
                    injectionType = (Injection.Type)injectionAttribute.ConstructorArguments[0].Value;
                    for (var i = 0; i < injectionAttribute.Properties.Count; i++)
                    {
                        var property = injectionAttribute.Properties[i];
                        if (property.Name == "MethodName")
                            methodName = (string)property.Argument.Value;
                        if (property.Name == "TypeName")
                            typeName = (string)property.Argument.Value;
                        if (property.Name == "FieldName")
                            fieldName = (string)property.Argument.Value;
                        if (property.Name == "PropertyName")
                            propertyName = (string)property.Argument.Value;
                        if (property.Name == "MethodName" || property.Name == "TypeName" || property.Name == "FieldName" || property.Name == "PropertyName") // for now remove it. we'll add them later again
                        {
                            injectionAttribute.Properties.RemoveAt(i);
                            i--;
                            continue;
                        }
                    }
                }
            }
            private void ParseModType(TypeDefinition type)
            {
                TypeDefinition baseType = null;
                TypeReference baseTypeReference = null;

                // check if we should parse this method
                var inheritanceAttribute = type.CustomAttributes.First(c => c.AttributeType.FullName == "ModAPI.Inheritance");
                if (inheritanceAttribute != null)
                {
                    Logger.Info($"Ignoring type {type.FullName} because ModAPI.Inheritance attribute is present.");
                    return;
                }

                // search for the base type
                if (type.BaseType != null)
                {
                    var baseTypeName = type.BaseType.FullName;
                    if (AllTypes.ContainsKey(baseTypeName))
                    {
                        baseType = AllTypes[baseTypeName];
                        baseTypeReference = type.Module.ImportReference(baseType);
                        Logger.Info($"Found base type \"{type.BaseType.FullName}\" of \"{type.FullName}\" in mod library.");
                    }
                }

                int highestDisplayClass = MonoHelper.GetHighestDisplayClassGroup(type);

                List<MethodDefinition> foundMethods = new List<MethodDefinition>();
                bool foundMethod = false;
                for (var m = 0; m < type.Methods.Count; m++)
                {
                    var method = type.Methods[m];
                    var module = method.Module;

                    if (method.IsConstructor)
                        continue;

                    ExtractAttributes(method, out var injectionAttribute, out var injectionType, out var methodName, out var typeName, out var fieldName, out var propertyName);

                    if (typeName == null)
                    {
                        if (baseType == null)
                        {
                            Logger.Info($"No base type found for method {method.FullName}");
                            continue;
                        }
                        else
                            typeName = baseType.FullName;
                    }

                    TypeReference methodBaseTypeReference = null;
                    if (baseType != null && typeName == baseType.FullName)
                        methodBaseTypeReference = baseTypeReference;
                    else if (typeName != null && AllTypes.ContainsKey(typeName))
                        methodBaseTypeReference = method.Module.ImportReference(AllTypes[typeName]);

                    if (methodBaseTypeReference == null)
                    {
                        // type needs to exist
                        Logger.Warn($"Base type {typeName} for method {method.FullName} was not found...");
                        continue;
                    }

                    string returnType = null;
                    if (methodName == null)
                        methodName = method.Name;

                    var baseMethod = FindBaseMethodAndValidate(methodBaseTypeReference.Resolve(), method, injectionType, methodName);
                    if (baseMethod != null)
                    {
                        Logger.Info("Found base method {baseMethod.FullName} for {method.FullName}");
                        foundMethod = true;
                        if (method.GetProperty(out var foundProperty))
                        {
                            CustomAttribute formerFieldAttribute = null;
                            foreach (var attr in foundProperty.CustomAttributes)
                            {
                                if (attr.Constructor.DeclaringType.FullName == "ModAPI.FormerField")
                                {
                                    formerFieldAttribute = attr;
                                    break;
                                }
                            }
                            if (formerFieldAttribute != null)
                                fieldName = formerFieldAttribute.ConstructorArguments[0].Value.ToString();
                            else
                                propertyName = foundProperty.FullName;
                        }
                        
                        if (injectionAttribute == null)
                        {
                            injectionAttribute = new CustomAttribute(method.Module.ImportReference(AttributeConstructors["ModAPI.Injection"]));
                            method.CustomAttributes.Add(injectionAttribute);
                        }

                        methodName = baseMethod.FullName;
                        if (fieldName != null)
                        {
                            // we need to rename the method name in accordance to ModApplier
                            methodName = methodName.Replace("::get_", "::get___ModAPI_").Replace("::set_", "::set__ModAPI_"); // for example: get_property to get___ModAPI_property
                        }

                        if (injectionAttribute.ConstructorArguments.Count == 0)
                            injectionAttribute.ConstructorArguments.Add(new CustomAttributeArgument(module.ImportReference(InjectionTypeType), injectionType));
                        else
                            injectionAttribute.ConstructorArguments[0] = new CustomAttributeArgument(module.ImportReference(InjectionTypeType), injectionType);

                        injectionAttribute.Properties.Add(new CustomAttributeNamedArgument("FullMethodName", new CustomAttributeArgument(module.TypeSystem.String, methodName)));
                        injectionAttribute.Properties.Add(new CustomAttributeNamedArgument("TypeName", new CustomAttributeArgument(module.TypeSystem.String, methodBaseTypeReference.FullName)));
                        if (fieldName != null)
                            injectionAttribute.Properties.Add(new CustomAttributeNamedArgument("FullFieldName", new CustomAttributeArgument(module.TypeSystem.String, fieldName)));
                        if (propertyName != null)
                            injectionAttribute.Properties.Add(new CustomAttributeNamedArgument("FullPropertyName", new CustomAttributeArgument(module.TypeSystem.String, propertyName)));

                        method.IsGetter = false;
                        method.IsSetter = false;
                        method.IsVirtual = false;
                        method.IsHideBySig = true;
                        method.IsSpecialName = false;
                        if (!method.IsStatic && !baseMethod.IsStatic)
                        {
                            method.IsStatic = true;
                            method.HasThis = false;
                            method.Parameters.Insert(0, new ParameterDefinition("self", ParameterAttributes.None, module.ImportReference(baseMethod.DeclaringType)));
                        }

                        if (injectionType == Injection.Type.Chain)
                        {
                            if (Delegates.ContainsKey(baseMethod.FullName))
                            {
                                var @delegate = Delegates[baseMethod.FullName];
                                
                                method.Body.SimplifyMacros();
                                var chainParam = new ParameterDefinition("__modapi_chain_methods", ParameterAttributes.None, method.Module.ImportReference(@delegate.Type.MakeArrayType()));
                                var numParam = new ParameterDefinition("__modapi_chain_num", ParameterAttributes.None, method.Module.TypeSystem.Int32);
                                method.Parameters.Add(chainParam);
                                method.Parameters.Add(numParam);

                                var routes = CallStack.FindCallsTo(method, baseMethod);
                                ExtendRoutesToBaseCall(this, type, routes, method, chainParam, numParam, @delegate, highestDisplayClass);
                                method.Body.Optimize();
                            }
                        }
                        if (injectionType == Injection.Type.Replace)
                            Project.Configuration.MethodReplaces.Add(methodName);
                        if (injectionType == Injection.Type.HookAfter)
                            Project.Configuration.MethodHookAfter.Add(methodName);
                        if (injectionType == Injection.Type.HookBefore)
                            Project.Configuration.MethodHookBefore.Add(methodName);
                        if (injectionType == Injection.Type.Chain)
                            Project.Configuration.MethodChain.Add(methodName);

                        foundMethods.Add(method);
                    }
                    else
                        Logger.Warn("Couldn't find base method of " + method.FullName);
                }
                if (foundMethod)
                    type.IsPublic = true;

                var removeProperties = new List<PropertyDefinition>();
                foreach (var property in type.Properties)
                {
                    if (foundMethods.Contains(property.SetMethod))
                        property.SetMethod = null;
                    if (foundMethods.Contains(property.GetMethod))
                        property.GetMethod = null;
                    if (property.GetMethod == null && property.SetMethod == null)
                        removeProperties.Add(property);
                }
                foreach (var p in removeProperties)
                    type.Properties.Remove(p);
            }
        }
    }
}
