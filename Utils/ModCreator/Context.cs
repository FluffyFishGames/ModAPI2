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
        /// <summary>
        /// This context holds all necessary information and runs all operations.
        /// </summary>
        private class Context
        {
            /// <summary>
            /// The project to create a mod from
            /// </summary>
            public ModProject Project;

            /// <summary>
            /// The mod library
            /// </summary>
            public ModLibrary ModLibrary;

            /// <summary>
            /// The reader parameters for mod library assemblies
            /// </summary>
            private ReaderParameters ModLibraryReaderParameters;

            /// <summary>
            /// The progress handler (retrieved from provided input)
            /// </summary>
            public ProgressHandler ProgressHandler;

            /// <summary>
            /// The progress of fetching types and their methods, delegates etc. from the ModLibrary.
            /// </summary>
            private ProgressHandler ModLibraryProgress;

            /// <summary>
            /// The progress of parsing the mod
            /// </summary>
            private ProgressHandler ModProgress;

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

                ModLibrary = new ModLibrary(Project.Game.ModLibrary);
            }

            /// <summary>
            /// Load the mod library
            /// </summary>
            public void LoadModLibrary()
            {
                ModLibrary.Load(ModLibraryProgress);
            }

            /// <summary>
            /// Applies changes to the mod assembly to make it compatible for applying it to the game
            /// </summary>
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
                ModLibrary.Dispose();

                ModProgress.ChangeProgress($"Saving mod...", 1f);
                ModProgress.Finish();
            }

            /// <summary>
            /// Searches for the intended method to inject into and checks if injection is possible with the given method.
            /// </summary>
            /// <param name="baseType">The base type to start looking at</param>
            /// <param name="method">The method to inject</param>
            /// <param name="injectionType">The injection type to apply</param>
            /// <param name="methodName">The method name to look for</param>
            /// <returns>The method to which an injection is performed on</returns>
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
                    if (ModLibrary.ResolveType(typeName, out var t))
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

                var assignableTypes = ModLibrary.GetAllAssignableTypes(baseType);
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

            /// <summary>
            /// Extract all injection attributes (if custom attribute is present and else assume them by given context)
            /// </summary>
            /// <param name="method">The method to extract the attributes from</param>
            /// <param name="injectionAttribute">The found attribute</param>
            /// <param name="injectionType">The found injection type</param>
            /// <param name="methodName">The found method name</param>
            /// <param name="typeName">The found type name</param>
            /// <param name="fieldName">The found field name</param>
            /// <param name="propertyName">The found property name</param>
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

            /// <summary>
            /// Parses a type of the mod and its methods. Transpiles if necessary to make the methods applyable to the game later.
            /// </summary>
            /// <param name="type">The type to parse</param>
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
                    if (ModLibrary.ResolveType(baseTypeName, out baseType))
                    {
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
                    else if (typeName != null && ModLibrary.ResolveType(typeName, out var t))
                        methodBaseTypeReference = method.Module.ImportReference(t);

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
                            injectionAttribute = new CustomAttribute(module.ImportReference(ModLibrary.AttributeConstructors["ModAPI.Injection"]));
                            method.CustomAttributes.Add(injectionAttribute);
                        }

                        methodName = baseMethod.FullName;
                        if (fieldName != null)
                        {
                            // we need to rename the method name in accordance to ModApplier
                            methodName = methodName.Replace("::get_", "::get___ModAPI_").Replace("::set_", "::set__ModAPI_"); // for example: get_property to get___ModAPI_property
                        }

                        if (injectionAttribute.ConstructorArguments.Count == 0)
                            injectionAttribute.ConstructorArguments.Add(new CustomAttributeArgument(module.ImportReference(ModLibrary.InjectionTypeType), injectionType));
                        else
                            injectionAttribute.ConstructorArguments[0] = new CustomAttributeArgument(module.ImportReference(ModLibrary.InjectionTypeType), injectionType);

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
                            if (ModLibrary.Delegates.ContainsKey(baseMethod.FullName))
                            {
                                var @delegate = ModLibrary.Delegates[baseMethod.FullName];
                                
                                method.Body.SimplifyMacros();
                                var chainParam = new ParameterDefinition("__modapi_chain_methods", ParameterAttributes.None, method.Module.ImportReference(@delegate.Type.MakeArrayType()));
                                var numParam = new ParameterDefinition("__modapi_chain_num", ParameterAttributes.None, method.Module.TypeSystem.Int32);
                                method.Parameters.Add(chainParam);
                                method.Parameters.Add(numParam);

                                var routes = CallStack.FindCallsTo(method, baseMethod);
                                CallStack.Extend(method, ModLibrary, new Dictionary<string, TypeReference>() {
                                    { "num", module.TypeSystem.Int32 },
                                    { "chain", @delegate.Type.MakeArrayType() }
                                }, routes);
                                //ExtendRoutesToBaseCall(this, type, routes, method, chainParam, numParam, @delegate, highestDisplayClass);
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