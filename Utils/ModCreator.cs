using ModAPI.ViewModels;
using ModAPI.ViewModels.ModProject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using System.IO;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System.Text.RegularExpressions;

namespace ModAPI.Utils
{
    internal partial class ModCreator
    {
        private static NLog.ILogger Logger = NLog.LogManager.GetLogger("ModCreator");

        public class Context
        {
            public ModProject Project;
            public ProgressHandler ProgressHandler;
        }

        public static void Execute(Context ctxt)
        {
            /*try
            {*/
            var context = new PrivateContext(ctxt);
            context.Project.Configuration.MethodReplaces.Clear();
            context.Project.Configuration.MethodHookAfter.Clear();
            context.Project.Configuration.MethodHookBefore.Clear();
            context.Project.Configuration.MethodChain.Clear();

            var libraries = context.Project.Game.ModLibrary.Libraries;
            var baseModLibPath = Path.Combine(context.Project.Game.ModLibrary.LibraryDirectory, "BaseModLib.dll");
            if (!File.Exists(baseModLibPath))
                throw new Exception("BaseModLib wasn't found");

            var c = 0;
            context.ProgressHandler.ChangeProgress("Loading mod library assemblies (1/" + (libraries.Count + 1) + ")...", 0f);
            context.BaseModLib = AssemblyDefinition.ReadAssembly(baseModLibPath);
            context.InitializeBaseModLib();
            c++;

            context.ProgressHandler.ChangeProgress("Loading mod library assemblies (2/" + (libraries.Count + 1) + ")...", 0f);
            foreach (var library in libraries)
            {
                var assembly = library.LoadAssembly();
             
                context.Assemblies[assembly.FullName] = assembly;
                foreach (var type in assembly.MainModule.Types)
                {
                    ParseType(context, type);
                }
                c++;
                context.ProgressHandler.ChangeProgress("Loading mod library assemblies (" + (c + 2) + "/" + (libraries.Count + 1) + ")...", 0.5f * ((float)c / (float)(libraries.Count + 1)));
            }

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(context.Project.Game.ModLibrary.LibraryDirectory);
            var readerParameters = new ReaderParameters()
            {
                AssemblyResolver = resolver,
                ReadWrite = true
            };
            var modAssembly = AssemblyDefinition.ReadAssembly(Path.Combine(context.Project.Directory, context.Project.Configuration.Name + ".dll"), readerParameters);
            foreach (var type in modAssembly.MainModule.Types)
            {
                ParseModType(context, type);
            }

            context.ProgressHandler.ChangeProgress("Loading mod library assemblies...", 0.02f);
            
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
            /*
            for (var i = 0; i < modAssembly.MainModule.AssemblyReferences.Count; i++)
            {
                if (modAssembly.MainModule.AssemblyReferences[i].Name == "netstandard")
                {
                    modAssembly.MainModule.AssemblyReferences.RemoveAt(i);
                    i--;
                }
            }*/
            modAssembly.MainModule.Resources.Add(new EmbeddedResource("ModConfiguration", ManifestResourceAttributes.Public, System.Text.Encoding.UTF8.GetBytes(context.Project.Configuration.ToJSON(true).ToString())));
            //modAssembly.MainModule.Kind = ModuleKind.NetModule;
            //modAssembly.Name.Attributes.HasFlag(AssemblyAttributes.WindowsRuntime)
            modAssembly.Write(Path.Combine(context.Project.Directory, "Output.dll"));
            modAssembly.Dispose();

            foreach (var assembly in context.Assemblies)
            {
                assembly.Value.Dispose();
            }
            /*}
            catch (Exception e)
            {

            }*/
        }

        private static void ParseModType(PrivateContext context, TypeDefinition type)
        {
            TypeDefinition baseType = null;
            TypeReference baseTypeReference = null;
            if (type.BaseType != null)
            {
                var baseTypeName = type.BaseType.FullName;
                if (context.AllTypes.ContainsKey(baseTypeName))
                {
                    baseType = context.AllTypes[baseTypeName];
                    baseTypeReference = type.Module.ImportReference(baseType);
                    Logger.Info($"Found base type \"{type.BaseType.FullName}\" of \"{type.FullName}\" in mod library.");
                }
            }

            if (baseType != null)
            {
                int highestDisplayClass = MonoHelper.GetHighestDisplayClassGroup(type);
                
                List<MethodDefinition> foundMethods = new List<MethodDefinition>();
                bool foundMethod = false;
                for (var m = 0; m < type.Methods.Count; m++)
                {
                    var method = type.Methods[m];
                    if (method.IsConstructor)
                        continue;
                    Injection.Type injectionType = Injection.Type.Chain;
                    string methodName = null;
                    string typeName = null;
                    string fieldName = null;
                    string propertyName = null;
                    CustomAttribute injectionAttribute = null;
                    foreach (var attribute in method.CustomAttributes)
                    {
                        if (attribute.AttributeType.FullName == "ModAPI.Injection")
                        {
                            injectionAttribute = attribute;
                            injectionType = (Injection.Type)attribute.ConstructorArguments[0].Value;
                            for (var i = 0; i < attribute.Properties.Count; i++)
                            {
                                var property = attribute.Properties[i];
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
                                    attribute.Properties.RemoveAt(i);
                                    i--;
                                    continue;
                                }
                            }
                        }
                    }
                    TypeReference methodBaseTypeReference = null;
                    if (typeName == null && baseType != null)
                        methodBaseTypeReference = baseTypeReference;
                    else if (typeName != null && context.AllTypes.ContainsKey(typeName))
                        methodBaseTypeReference = method.Module.ImportReference(context.AllTypes[typeName]);

                    if (methodBaseTypeReference == null)
                    {
                        // type needs to exist
                        continue;
                    }
                
                    string returnType = null;
                    if (methodName == null)
                        methodName = method.Name;

                    var candidates = context.FindBaseMethods(method.DeclaringType.BaseType.FullName, methodName);

                    Logger.Info("Checking candidates for " + method.FullName);
                    var assignableTypes = context.GetAllAssignableTypes(baseType);
                    MethodDefinition baseMethod = null;
                
                    foreach (var candidate in candidates)
                    {
                        if (injectionType != Injection.Type.HookBefore && candidate.ReturnType.FullName != method.ReturnType.FullName)
                        {
                            Logger.Trace("Return type of candidate " + candidate.FullName + " is not a match.");
                            continue;
                        }
                        if (candidate.GenericParameters.Count != method.GenericParameters.Count)
                        {
                            Logger.Trace("Generic parameters of candidate " + candidate.FullName + " are not a match.");
                            continue;
                        }
                        for (var i = 0; i < candidate.GenericParameters.Count; i++)
                        {
                            if (!candidate.GenericParameters[i].MatchingSignature(method.GenericParameters[i]))
                            {
                                Logger.Trace("Generic parameters of candidate " + candidate.FullName + " are not a match.");
                                continue;
                            }
                        }
                        var paramsCount = method.Parameters.Count;
                        var paramsOffset = 0;
                        if (!candidate.IsStatic && method.IsStatic)
                        {
                            if (method.Parameters.Count == 0)
                            {
                                Logger.Trace("Candidate " + candidate.FullName + " wasn't suitable as method is static and has no parameters (so most likely injecting into static method is the goal).");
                                continue;
                            }
                            if (!assignableTypes.Contains(method.Parameters[0].ParameterType.FullName))
                            {
                                Logger.Trace("Candidate " + candidate.FullName + " wasn't suitable for first parameter of type " + method.Parameters[0].ParameterType.FullName);
                                continue;
                            }
                            paramsOffset++;
                        }
                        if (injectionType == Injection.Type.HookAfter && candidate.ReturnType.FullName != "System.Void")
                        {
                            if (method.Parameters.Count == 0)
                            {
                                Logger.Trace("Method seems broken? Method is of type hook after and doesn't have parameters.");
                                continue;
                            }
                            if (method.Parameters[0].ParameterType.FullName != candidate.ReturnType.FullName)
                            {
                                Logger.Trace("Candidate " + candidate.FullName + " wasn't suitable as the method is of type hook after and the return type of the candidate isn't matching the first parameter of method.");
                                continue;
                            }
                            paramsOffset++;
                        }
                        if (candidate.Parameters.Count != paramsCount - paramsOffset)
                        {
                            Logger.Trace("Parameters of candidate " + candidate.FullName + " are not a match.");
                            continue;
                        }
                        for (var i = 0; i < candidate.Parameters.Count; i++)
                        {
                            if (!candidate.Parameters[i].MatchingSignature(method.Parameters[i + paramsOffset]))
                            {
                                Logger.Trace("Parameters of candidate " + candidate.FullName + " are not a match.");
                                continue;
                            }
                        }
                        // we got him. It's a match! :)
                        baseMethod = candidate;
                        break;
                    }
                    if (baseMethod != null)
                    {
                        Logger.Info("Found base method " + baseMethod.FullName + " for " + method.FullName);
                        foundMethod = true;
                        if (method.IsGetter || method.IsSetter)
                        {
                            PropertyDefinition foundProperty = null;
                            foreach (var property in baseMethod.DeclaringType.Properties)
                            {
                                if (property.GetMethod == baseMethod || property.SetMethod == baseMethod)
                                {
                                    foundProperty = property;
                                    break;
                                }
                            }

                            if (foundProperty != null)
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
                                    fieldName = formerFieldAttribute.ConstructorArguments[0].Value.ToString();//.Replace("::", "::__ModAPI_");
                                else
                                    propertyName = foundProperty.FullName;
                            }
                        }
                        if (injectionAttribute == null)
                        {
                            injectionAttribute = new CustomAttribute(method.Module.ImportReference(context.AttributeConstructors["ModAPI.Injection"]));
                            method.CustomAttributes.Add(injectionAttribute);
                        }
                        if (injectionAttribute.ConstructorArguments.Count == 0)
                            injectionAttribute.ConstructorArguments.Add(new CustomAttributeArgument(method.Module.ImportReference(context.InjectionTypeType), injectionType));
                        else
                            injectionAttribute.ConstructorArguments[0] = new CustomAttributeArgument(method.Module.ImportReference(context.InjectionTypeType), injectionType);

                        //injectionAttribute.Properties.Add(new CustomAttributeNamedArgument("MethodName", new CustomAttributeArgument(method.Module.TypeSystem.String, methodName)));
                        injectionAttribute.Properties.Add(new CustomAttributeNamedArgument("TypeName", new CustomAttributeArgument(method.Module.TypeSystem.String, methodBaseTypeReference.FullName)));// baseMethod.DeclaringType.FullName)));
                        if (fieldName != null)
                            injectionAttribute.Properties.Add(new CustomAttributeNamedArgument("FullFieldName", new CustomAttributeArgument(method.Module.TypeSystem.String, fieldName)));
                        if (propertyName != null)
                            injectionAttribute.Properties.Add(new CustomAttributeNamedArgument("FullPropertyName", new CustomAttributeArgument(method.Module.TypeSystem.String, propertyName)));

                        methodName = baseMethod.FullName;
                        if (fieldName != null)
                        {
                            // we need to rename the method name in accordance to ModApplier
                            methodName = methodName.Replace("::get_", "::get___ModAPI_").Replace("::set_", "::set__ModAPI_"); // for example: get_property to get___ModAPI_property
                        }

                        injectionAttribute.Properties.Add(new CustomAttributeNamedArgument("FullMethodName", new CustomAttributeArgument(method.Module.TypeSystem.String, methodName)));
                        method.IsGetter = false;
                        method.IsSetter = false;
                        method.IsVirtual = false;
                        method.IsHideBySig = true;
                        method.IsSpecialName = false;
                        if (!method.IsStatic && !baseMethod.IsStatic)
                        {
                            method.IsStatic = true;
                            method.HasThis = false;
                        
                            method.Parameters.Insert(0, new ParameterDefinition("self", ParameterAttributes.None, method.Module.ImportReference(baseMethod.DeclaringType)));
                        }

                        if (injectionType == Injection.Type.Chain)
                        {
                            if (context.Delegates.ContainsKey(baseMethod.FullName))
                            {
                                method.Body.SimplifyMacros();
                                var @delegate = context.Delegates[baseMethod.FullName];
                            
                                var processor = method.Body.GetILProcessor();
                                var delegateArray = @delegate.Type.MakeArrayType();
                                var chainParam = new ParameterDefinition("__modapi_chain_methods", ParameterAttributes.None, method.Module.ImportReference(delegateArray));
                                var numParam = new ParameterDefinition("__modapi_chain_num", ParameterAttributes.None, method.Module.TypeSystem.Int32);
                                method.Parameters.Add(chainParam);
                                method.Parameters.Add(numParam);

                                var routes = CallStack.FindCallsTo(method, baseMethod);
                                ExtendRoutesToBaseCall(context, type, routes, method, chainParam, numParam, @delegate, highestDisplayClass);

                                method.Body.Optimize();
                            }
                            if (method.ReturnType.FullName != "System.Void")
                            {
                                //var delegateName = 
                                /*var func = context.AllTypes["System.Func´" + (method.Parameters.Count + 1)];
                                var g = new TypeReference[method.Parameters.Count + 1];
                                for (var i = 0; i < method.Parameters.Count; i++)
                                {
                                    method.Parameters[i].ParameterType
                                }
                                func.MakeGenericInstanceType();
                                method.Parameters.Add(new ParameterDefinition("__modapi_next_method", ParameterAttributes.None, method.Module.ImportReference(funcType)));*/
                            }
                        
                        }
                        if (injectionType == Injection.Type.Replace)
                            context.Project.Configuration.MethodReplaces.Add(methodName);
                        if (injectionType == Injection.Type.HookAfter)
                            context.Project.Configuration.MethodHookAfter.Add(methodName);
                        if (injectionType == Injection.Type.HookBefore)
                            context.Project.Configuration.MethodHookBefore.Add(methodName);
                        if (injectionType == Injection.Type.Chain)
                            context.Project.Configuration.MethodChain.Add(methodName);

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

        private static void ExtendRoutesToBaseCall(PrivateContext context, TypeDefinition type, List<CallStack.Node> routes, MethodDefinition method, ParameterDefinition chainParam, ParameterDefinition numParam, MonoHelper.Delegate @delegate, int highestDisplayClass)
        {
            foreach (var route in routes)
            {
                __ExtendRouteToBaseCall(context, type, route, new CallStackCopyContext()
                {
                    Delegate = @delegate,
                    HighestDisplayClassNum = highestDisplayClass
                }, 
                new CallStackCopyScopeContext()
                {
                    ChainParam = chainParam,
                    NumParam = numParam,
                    Method = method,
                    Type = CallStackCopyScopeContext.TypeEnum.METHOD
                });
            }
        }

        private static Dictionary<string, TypeDefinition> GetCorlibClasses(PrivateContext context, ModuleDefinition module, List<string> names)
        {
            var ret = new Dictionary<string, TypeDefinition>();
            foreach (var n in names)
            {
                if (context.AllTypes.ContainsKey(n))
                    ret.Add(n, context.AllTypes[n]);
            }
            return ret;
        }

        private static void __ExtendRouteToBaseCall(PrivateContext context, TypeDefinition type, CallStack.Node node, CallStackCopyContext routeContext, CallStackCopyScopeContext scope)
        {
            var module = type.Module;
            if (node.Type == CallStack.Node.NodeType.Call)
            {
                if (scope.Type == CallStackCopyScopeContext.TypeEnum.METHOD)
                {
                    ReplaceMethodCallWithNextCall(node.Method, routeContext.Delegate, scope.ChainParam, scope.NumParam, node.Instruction);
                }
                else if (scope.Type == CallStackCopyScopeContext.TypeEnum.DISPLAY_CLASS)
                {
                    ReplaceMethodCallWithNextCall(node.Method, routeContext.Delegate, scope.DisplayClass.ChainMethodsField, scope.DisplayClass.ChainNumField, node.Instruction);
                }
            }
            else if (node.CalledMethod.Name.StartsWith("<") && !node.CalledMethod.Name.StartsWith("<>n__"))
            {
                if (node.CalledMethod.DeclaringType.FullName == type.FullName) // method
                {
                    if (scope.Type == CallStackCopyScopeContext.TypeEnum.METHOD)
                    {
                        MethodDefinition newMethod = null;
                        DisplayClass displayClass = null;
                        if (routeContext.MethodMappings.ContainsKey(node.Method.FullName))
                        {
                            // it may be necessary to check for the actual type of the method (as the compiler sometimes likes to
                            // create some methods like n__0 for duplicated code and stuff.
                            
                            newMethod = routeContext.Methods[routeContext.MethodMappings[node.Method.FullName]];
                            displayClass = routeContext.DisplayClasses[newMethod.DeclaringType.FullName];
                        }
                        else
                        {
                            // create a new display class

                            var mscorlibTypes = GetCorlibClasses(context, module, new List<string>() { "System.Runtime.CompilerServices.CompilerGeneratedAttribute", "System.Object" });
                            var objectType = mscorlibTypes["System.Object"];
                            var compilerGeneratedAttributeConstructor = mscorlibTypes["System.Runtime.CompilerServices.CompilerGeneratedAttribute"].Methods.First(m => m.IsConstructor);

                            routeContext.HighestDisplayClassNum++;
                            routeContext.HighestDisplayClassSub = 0;
                            var newName = "<>c__DisplayClass" + routeContext.HighestDisplayClassNum + "_" + routeContext.HighestDisplayClassSub;
                            var newClass = new TypeDefinition(type.Namespace, newName, TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Sealed | TypeAttributes.NestedPrivate, module.TypeSystem.Object);
                            newClass.CustomAttributes.Add(new CustomAttribute(module.ImportReference(compilerGeneratedAttributeConstructor), new byte[] { 1, 0, 0, 0 }));

                            //var objectType = module.TypeSystem.Object.Resolve();
                            MethodDefinition objectConstructor = null;
                            for (var i = 0; i < objectType.Methods.Count; i++)
                            {
                                if (objectType.Methods[i].IsConstructor && objectType.Methods[i].Parameters.Count == 0)
                                {
                                    objectConstructor = objectType.Methods[i];
                                    break;
                                }
                            }
                            var newFields = new Dictionary<string, FieldDefinition>();
                            var thisField = new FieldDefinition("self", FieldAttributes.Public, module.ImportReference(type));
                            var numField = new FieldDefinition("__ModAPI_chain_num", FieldAttributes.Public, module.TypeSystem.Int32);
                            var chainField = new FieldDefinition("__ModAPI_chain_methods", FieldAttributes.Public, module.ImportReference(routeContext.Delegate.Type.MakeArrayType()));
                            newFields.Add("self", thisField);
                            newFields.Add("__ModAPI_chain_methods", chainField);
                            newFields.Add("__ModAPI_chain_num", numField);
                            newClass.Fields.Add(thisField);
                            newClass.Fields.Add(chainField);
                            newClass.Fields.Add(numField);

                            var newMethods = new Dictionary<string, MethodDefinition>();
                            MethodDefinition constructor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, module.TypeSystem.Void);
                            var constructorProcessor = constructor.Body.GetILProcessor();
                            constructorProcessor.Append(constructorProcessor.Create(OpCodes.Ldarg_0));
                            constructorProcessor.Append(constructorProcessor.Create(OpCodes.Call, module.ImportReference(objectConstructor)));
                            constructorProcessor.Append(constructorProcessor.Create(OpCodes.Nop));
                            constructorProcessor.Append(constructorProcessor.Create(OpCodes.Ret));
                            newClass.Methods.Add(constructor);

                            type.NestedTypes.Add(newClass);

                            displayClass = new DisplayClass()
                            {
                                Constructor = constructor,
                                ChainMethodsField = chainField,
                                ChainNumField = numField,
                                Fields = newFields,
                                ThisField = thisField,
                                Methods = newMethods,
                                Type = newClass
                            };

                            var match = Regex.Match(node.CalledMethod.Name, @"\<([^\>]+)\>b__([0-9]+)_([0-9]+)");
                            var n = match.Groups[1].Value;

                            displayClass.HighestSub++;
                            var newMethodName = "<" + n + ">b__" + displayClass.HighestSub;

                            newMethod = new MethodDefinition(newMethodName, node.CalledMethod.Attributes, node.CalledMethod.ReturnType);
                            newMethod.IsPublic = true;
                            for (var i = 0; i < node.CalledMethod.Parameters.Count; i++)
                                newMethod.Parameters.Add(new ParameterDefinition(node.CalledMethod.Parameters[i].ParameterType));
                            var newBody = newMethod.Body;

                            node.CalledMethod.Body.Copy(newBody, module);
                            routeContext.Methods.Add(newMethod.FullName, newMethod);
                            routeContext.MethodMappings.Add(node.Method.FullName, newMethod.FullName);


                            var newMethodProcessor = newBody.GetILProcessor();

                            //re-route childs
                            for (var i = 0; i < node.CalledMethod.Body.Instructions.Count; i++)
                            {
                                for (var j = 0; j < node.Children.Count; j++)
                                {
                                    if (node.Children[j].Instruction == node.CalledMethod.Body.Instructions[i])
                                    {
                                        node.Children[j].Method = newMethod;
                                        node.Children[j].Instruction = newBody.Instructions[i];
                                    }
                                }
                            }
                            
                            for (var i = 0; i < newBody.Instructions.Count; i++)
                            {
                                if (newBody.Instructions[i].OpCode == OpCodes.Ldarg_0)
                                {
                                    // this will also be applied to ldftn statements. This has to be removed later on for ldftn statements in the route
                                    newMethodProcessor.InsertAfter(newBody.Instructions[i], newMethodProcessor.Create(OpCodes.Ldfld, displayClass.ThisField));
                                    i++;
                                }
                            }

                            displayClass.Type.Methods.Add(newMethod);
                        }

                        var newScope = new CallStackCopyScopeContext()
                        {
                            Type = CallStackCopyScopeContext.TypeEnum.DISPLAY_CLASS,
                            DisplayClass = displayClass
                        };

                        foreach (var n in node.Children)
                        {
                            // we keep being in the same display class for now
                            __ExtendRouteToBaseCall(context, type, n, routeContext, newScope);
                        }

                        var body = node.Method.Body;
                        body.SimplifyMacros();

                        var displayClassVar = new VariableDefinition(module.ImportReference(displayClass.Type));
                        body.Variables.Insert(0, displayClassVar);
                        var processor = body.GetILProcessor();

                        var first = body.Instructions[0];
                        processor.InsertBefore(first, processor.Create(OpCodes.Newobj, module.ImportReference(displayClass.Constructor)));
                        processor.InsertBefore(first, processor.Create(OpCodes.Stloc, displayClassVar));
                        processor.InsertBefore(first, processor.Create(OpCodes.Ldloc, displayClassVar));
                        processor.InsertBefore(first, processor.Create(OpCodes.Ldarg_0));
                        processor.InsertBefore(first, processor.Create(OpCodes.Stfld, module.ImportReference(displayClass.ThisField)));

                        processor.InsertBefore(first, processor.Create(OpCodes.Ldloc, displayClassVar));
                        processor.InsertBefore(first, processor.Create(OpCodes.Ldarg, scope.NumParam));
                        processor.InsertBefore(first, processor.Create(OpCodes.Stfld, module.ImportReference(displayClass.ChainNumField)));

                        processor.InsertBefore(first, processor.Create(OpCodes.Ldloc, displayClassVar));
                        processor.InsertBefore(first, processor.Create(OpCodes.Ldarg, scope.ChainParam));
                        processor.InsertBefore(first, processor.Create(OpCodes.Stfld, module.ImportReference(displayClass.ChainMethodsField)));

                        var inst = node.Instruction;
                        inst.Previous.OpCode = OpCodes.Ldloc;
                        inst.Previous.Operand = displayClassVar;
                        inst.Operand = module.ImportReference(newMethod);

                        body.Optimize();
                    }
                    else if (scope.Type == CallStackCopyScopeContext.TypeEnum.DISPLAY_CLASS)
                    {
                        MethodDefinition newMethod = null;
                        /*if (routeContext.MethodMappings.ContainsKey(node.CalledMethod.FullName))
                        {
                            // it may be necessary to check for the actual type of the method (as the compiler sometimes likes to
                            // create some methods like n__0 for duplicated code and stuff.
                            newMethod = routeContext.Methods[routeContext.MethodMappings[node.CalledMethod.FullName]];
                        }
                        else
                        {*/
                            var match = Regex.Match(node.CalledMethod.Name, @"\<([^\>]+)\>b__([0-9]+)_([0-9]+)");
                            var n = match.Groups[1].Value;
                            scope.DisplayClass.HighestSub++;

                            var newMethodName = "<" + n + ">b__" + scope.DisplayClass.HighestSub;
                            newMethod = new MethodDefinition(newMethodName, node.CalledMethod.Attributes, node.CalledMethod.ReturnType);
                            newMethod.IsPublic = true;

                            scope.DisplayClass.Type.Methods.Add(newMethod);

                            var newBody = newMethod.Body;

                            node.CalledMethod.Body.Copy(newBody);
                            routeContext.Methods.Add(newMethod.FullName, newMethod);
                            routeContext.MethodMappings.Add(node.CalledMethod.FullName, newMethod.FullName);

                            var newMethodProcessor = newBody.GetILProcessor();

                            //re-route childs
                            for (var i = 0; i < node.CalledMethod.Body.Instructions.Count; i++)
                            {
                                for (var j = 0; j < node.Children.Count; j++)
                                {
                                    if (node.Children[j].Instruction == node.CalledMethod.Body.Instructions[i])
                                    {
                                        node.Children[j].Method = newMethod;
                                        node.Children[j].Instruction = newMethod.Body.Instructions[i];
                                    }
                                }
                            }

                            for (var i = 0; i < newBody.Instructions.Count; i++)
                            {
                                if (newBody.Instructions[i].OpCode == OpCodes.Ldarg_0)
                                {
                                    // this will also be applied to ldftn statements. This has to be removed later on for ldftn statements in the route
                                    newMethodProcessor.InsertAfter(newBody.Instructions[i], newMethodProcessor.Create(OpCodes.Ldfld, scope.DisplayClass.ThisField));
                                    i++;
                                }
                            }
                        //}


                        var body = node.Method.Body;
                        var processor = body.GetILProcessor();
                        
                        var first = processor.WalkBack(node.Instruction);
                        // remove ldfld self
                        if (first.Next.OpCode == OpCodes.Ldfld)
                            processor.Remove(first.Next);
                        node.Instruction.Operand = module.ImportReference(newMethod);

                        foreach (var subNode in node.Children)
                        {
                            // we keep being in the same scope for now
                            __ExtendRouteToBaseCall(context, type, subNode, routeContext, scope);
                        }
                    }
                }
                else if (node.CalledMethod.DeclaringType.Name.StartsWith("<>c__DisplayClass")) // display class
                {
                    var displayClass = new DisplayClass(node.CalledMethod.DeclaringType);
                    DisplayClass newDisplayClass = null;
                    if (routeContext.ClassMappings.ContainsKey(node.CalledMethod.DeclaringType.Name))
                    {
                        newDisplayClass = routeContext.DisplayClasses[routeContext.ClassMappings[node.CalledMethod.DeclaringType.Name]];
                    }
                    else 
                    {
                        if (scope.Type == CallStackCopyScopeContext.TypeEnum.METHOD)
                        {
                            routeContext.HighestDisplayClassNum++;
                            routeContext.HighestDisplayClassSub = 0;
                        }
                        else
                            routeContext.HighestDisplayClassSub++;

                        newDisplayClass = displayClass.Copy(type, routeContext);
                        routeContext.ClassMappings.Add(displayClass.Type.FullName, newDisplayClass.Type.FullName);
                        routeContext.DisplayClasses.Add(newDisplayClass.Type.FullName, newDisplayClass);
                    }

                    bool sameDisplayClass = scope.Type == CallStackCopyScopeContext.TypeEnum.DISPLAY_CLASS && newDisplayClass.Type.FullName == scope.DisplayClass.Type.FullName;
                    //var newClasses = CopyDisplayClasses(type, classTypes[group], baseMethod, @delegate, ref highestDisplayClass);

                    var method = node.Method;
                    
                    if (!sameDisplayClass)
                    {
                        var body = method.Body;
                        body.SimplifyMacros();

                        var processor = node.Method.Body.GetILProcessor();
                        node.Instruction.Operand = type.Module.ImportReference(newDisplayClass.Methods[node.CalledMethod.Name]);

                        // add params
                        var previous = node.Instruction.Previous;
                        var variable = (VariableDefinition)previous.Operand;
                        variable.VariableType = module.ImportReference(newDisplayClass.Type);
                        // find new obj
                        var inst = node.Instruction;
                        
                        while (inst != null)
                        {
                            if (inst.OpCode == OpCodes.Stloc && inst.Operand is VariableDefinition v && v == variable && 
                                inst.Previous != null && inst.Previous.OpCode == OpCodes.Newobj && inst.Previous.Operand is MethodReference mref && 
                                mref.Name == ".ctor" && mref.DeclaringType.FullName == displayClass.Type.FullName)
                            {
                                inst = inst.Next;
                                break;
                            }
                            inst = inst.Previous;
                        }
                        if (inst != null)
                        {
                            bool alreadyChanged = false;
                            if (inst.OpCode == OpCodes.Ldloc && inst.Operand is VariableDefinition v && v == variable &&
                                inst.Next != null && inst.Next.OpCode == OpCodes.Ldarg && inst.Next.Operand is ParameterDefinition p1 && p1 == scope.ChainParam)
                                alreadyChanged = true;

                            if (!alreadyChanged)
                            {
                                if (scope.Type == CallStackCopyScopeContext.TypeEnum.METHOD)
                                {
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Ldloc, variable));
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Ldarg, scope.ChainParam));
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Stfld, newDisplayClass.ChainMethodsField));
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Ldloc, variable));
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Ldarg, scope.NumParam));
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Stfld, newDisplayClass.ChainNumField));
                                }
                                else if (scope.Type == CallStackCopyScopeContext.TypeEnum.DISPLAY_CLASS && routeContext.HighestDisplayClassSub == 0) // will most likely never be true?
                                {
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Ldloc, variable));
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Ldarg_0));
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Ldfld, scope.DisplayClass.ChainMethodsField));
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Stfld, newDisplayClass.ChainMethodsField));
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Ldloc, variable));
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Ldarg_0));
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Ldfld, scope.DisplayClass.ChainNumField));
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Stfld, newDisplayClass.ChainNumField));
                                }
                            }
                        }
                        
                        body.Optimize();
                    }

                    var newScope = new CallStackCopyScopeContext()
                    {
                        Type = CallStackCopyScopeContext.TypeEnum.DISPLAY_CLASS,
                        DisplayClass = newDisplayClass,
                        OriginalName = displayClass.Type.FullName
                    };

                    foreach (var n in node.Children)
                    {
                        // we keep being in the same display class for now
                        __ExtendRouteToBaseCall(context, type, n, routeContext, newScope);
                    }
                }
                else
                {
                    // something weird happened (e.g. method of nested type <>c was called even tho I deemed it impossible as of lacking reference to this)
                }
            }
            else
            {
                var newMethod = node.CalledMethod.Copy();

                //re-route childs
                for (var i = 0; i < node.CalledMethod.Body.Instructions.Count; i++)
                {
                    for (var j = 0; j < node.Children.Count; j++)
                    {
                        if (node.Children[j].Instruction == node.CalledMethod.Body.Instructions[i])
                        {
                            node.Children[j].Method = newMethod;
                            node.Children[j].Instruction = newMethod.Body.Instructions[i];
                        }
                    }
                }

                var chainParam = new ParameterDefinition("__modapi_chain_methods", ParameterAttributes.None, module.ImportReference(routeContext.Delegate.Type.MakeArrayType()));
                var numParam = new ParameterDefinition("__modapi_chain_num", ParameterAttributes.None, module.TypeSystem.Int32);
                newMethod.Parameters.Add(chainParam);
                newMethod.Parameters.Add(numParam);
                newMethod.Name += "_" + routeContext.HighestDisplayClassNum;
                node.CalledMethod.DeclaringType.Methods.Add(newMethod);

                var body = node.Method.Body;
                var processor = body.GetILProcessor();
                if (scope.Type == CallStackCopyScopeContext.TypeEnum.DISPLAY_CLASS)
                {
                    processor.InsertBefore(node.Instruction, processor.Create(OpCodes.Ldarg_0));
                    processor.InsertBefore(node.Instruction, processor.Create(OpCodes.Ldfld, scope.DisplayClass.ChainMethodsField));
                    processor.InsertBefore(node.Instruction, processor.Create(OpCodes.Ldarg_0));
                    processor.InsertBefore(node.Instruction, processor.Create(OpCodes.Ldfld, scope.DisplayClass.ChainNumField));
                }
                node.Instruction.Operand = module.ImportReference(newMethod);

                var newScope = new CallStackCopyScopeContext()
                {
                    Type = CallStackCopyScopeContext.TypeEnum.METHOD,
                    ChainParam = chainParam,
                    NumParam = numParam,
                    Method = newMethod
                };

                foreach (var n in node.Children)
                {
                    // we keep being in the same display class for now
                    __ExtendRouteToBaseCall(context, type, n, routeContext, newScope);
                }
                // ordinary method
            }
        }


        public class CallStackCopyContext
        {
            public Dictionary<string, string> ClassMappings = new();
            public Dictionary<string, string> MethodMappings = new();
            public Dictionary<string, DisplayClass> DisplayClasses = new();
            public Dictionary<string, MethodDefinition> Methods = new();
            public MonoHelper.Delegate Delegate;
            public int HighestDisplayClassNum;
            public int HighestDisplayClassSub;
        }

        public class CallStackCopyScopeContext
        {
            public enum TypeEnum
            {
                DISPLAY_CLASS,
                METHOD
            }
            public TypeEnum Type;
            public DisplayClass DisplayClass;
            public MethodDefinition Method;
            public ParameterDefinition NumParam;
            public ParameterDefinition ChainParam;
            public CallStackCopyScopeContext Parent;
            public string OriginalName;
        }

        public class DisplayClass
        {
            public TypeDefinition Type;
            public MethodDefinition Constructor;
            public Dictionary<string, MethodDefinition> Methods = new();
            public Dictionary<string, FieldDefinition> Fields = new();
            public FieldDefinition ThisField;
            public FieldDefinition ChainNumField;
            public FieldDefinition ChainMethodsField;
            public int HighestSub = -1;

            public DisplayClass()
            {

            }
            public DisplayClass(TypeDefinition type)
            {
                Type = type;
                for (var i = 0; i < type.Methods.Count; i++)
                {
                    if (type.Methods[i].IsConstructor)
                        Constructor = type.Methods[i];
                    else
                        Methods.Add(type.Methods[i].Name, type.Methods[i]);
                }
                for (var i = 0; i < type.Fields.Count; i++)
                {
                    Fields.Add(type.Fields[i].Name, type.Fields[i]);
                    if (type.Fields[i].Name == "<>4__this")
                    {
                        ThisField = type.Fields[i];
                        ThisField.Name = "self";
                    }
                    else if (type.Fields[i].Name == "self")
                        ThisField = type.Fields[i];
                    else if (type.Fields[i].Name == "__ModAPI_chain_methods")
                        ChainMethodsField = type.Fields[i];
                    else if (type.Fields[i].Name == "__ModAPI_chain_num")
                        ChainNumField = type.Fields[i];
                }
            }

            public DisplayClass Copy(TypeDefinition parent, CallStackCopyContext context)
            {
                var module = Type.Module;

                var @delegate = context.Delegate;
                //var match = Regex.Match(Type.Name, @"\<\>c__DisplayClass([0-9]+)_([0-9]+)");
                //var sub = int.Parse(match.Groups[2].Value);
                var newName = "<>c__DisplayClass" + context.HighestDisplayClassNum + "_" + context.HighestDisplayClassSub;
                var newClass = new TypeDefinition(Type.Namespace, newName, Type.Attributes);
                parent.NestedTypes.Add(newClass);

                var newFields = new Dictionary<string, FieldDefinition>();
                FieldDefinition thisField = null;
                for (var j = 0; j < Type.Fields.Count; j++)
                {
                    var field = Type.Fields[j];
                    var newField = new FieldDefinition(field.Name, field.Attributes, field.FieldType);
                    if (newField.Name == "<>4__this" || newField.Name == "self")
                    {
                        newField.Name = "self";
                        thisField = newField;
                    }
                    //if (classMapping.ContainsKey(original[i].Fields[j].FieldType.Name))
                    //    newField.FieldType = module.ImportReference(newClasses[classMapping[original[i].Fields[j].FieldType.Name]].Type);
                    newClass.Fields.Add(newField);
                    newFields.Add(newField.Name, newField);
                }
                    
                var numField = new FieldDefinition("__ModAPI_chain_num", FieldAttributes.Public, module.TypeSystem.Int32);    
                var chainField = new FieldDefinition("__ModAPI_chain_methods", FieldAttributes.Public, module.ImportReference(@delegate.Type.MakeArrayType()));
                newFields.Add("__ModAPI_chain_num", numField);
                newFields.Add("__ModAPI_chain_methods", chainField);
                newClass.Fields.Add(numField);
                newClass.Fields.Add(chainField);

                if (thisField == null)
                {
                    thisField = new FieldDefinition("self", FieldAttributes.Public, module.ImportReference(parent));
                    newFields.Add("self", thisField);
                    newClass.Fields.Add(thisField);
                }

                var newMethods = new Dictionary<string, MethodDefinition>();
                MethodDefinition constructor = null;
                var highestSub = 0;
                for (var j = 0; j < Type.Methods.Count; j++)
                {
                    var method = Type.Methods[j];
                    var match = Regex.Match(method.Name, @"\<([^\>]+)\>b__([0-9]+)");
                    if (match.Success)
                    {
                        var s = int.Parse(match.Groups[2].Value);
                        if (s > highestSub)
                            s = highestSub;
                        var newMethod = new MethodDefinition(method.Name, method.Attributes, method.ReturnType);
                        method.Body.Copy(newMethod.Body);
                        newMethods.Add(newMethod.Name, newMethod);
                        newClass.Methods.Add(newMethod);

                        if (context != null)
                        {
                            context.MethodMappings.Add(method.FullName, newMethod.FullName);
                            context.Methods.Add(newMethod.FullName, newMethod);
                        }
                    }
                    if (method.Name == ".ctor")
                    {
                        var newMethod = new MethodDefinition(method.Name, method.Attributes, method.ReturnType);
                        method.Body.Copy(newMethod.Body);
                        constructor = newMethod;
                        newClass.Methods.Add(newMethod);
                    }
                }

                foreach (var m in newMethods)
                {
                    var body = m.Value.Body;
                    for (var i = 0; i < body.Instructions.Count; i++)
                    {
                        var inst = body.Instructions[i];
                        if (inst.Operand is FieldReference fieldRef && newFields.ContainsKey(fieldRef.Name))
                            inst.Operand = module.ImportReference(newFields[fieldRef.Name]);
                        if (inst.Operand is MethodReference methodRef && newMethods.ContainsKey(methodRef.Name))
                            inst.Operand = module.ImportReference(newMethods[methodRef.Name]);
                    }
                }
                var displayClass = new DisplayClass()
                {
                    Type = newClass,
                    HighestSub = highestSub,
                    Constructor = constructor,
                    Fields = newFields,
                    Methods = newMethods,
                    ChainMethodsField = chainField,
                    ChainNumField = numField,
                    ThisField = thisField
                };

                return displayClass;
            }

            public void Resolve(CallStackCopyContext context)
            {
                foreach (var field in Fields)
                {
                    if (context.ClassMappings.ContainsKey(field.Value.FieldType.Name))
                        field.Value.FieldType = Type.Module.ImportReference(context.DisplayClasses[context.ClassMappings[field.Value.FieldType.Name]].Type);
                }
                foreach (var method in Methods)
                {
                    var body = method.Value.Body;
                    for (var j = 0; j < body.Instructions.Count; j++)
                    {
                        var instruction = body.Instructions[j];
                        if (instruction.Operand is MethodReference mref)
                        {
                            if (context.MethodMappings.ContainsKey(mref.FullName))
                                instruction.Operand = method.Value.Module.ImportReference(context.Methods[context.MethodMappings[mref.FullName]]);
                            else if (mref.Name == ".ctor" && context.ClassMappings.ContainsKey(mref.DeclaringType.Name))
                                instruction.Operand = method.Value.Module.ImportReference(context.DisplayClasses[context.ClassMappings[mref.DeclaringType.Name]].Constructor);
                        }
                    }
                }
            }

        }

        private static void ReplaceMethodCallWithNextCall(MethodDefinition method, MonoHelper.Delegate @delegate, FieldDefinition chain, FieldDefinition num, Instruction instruction)
        {
            var processor = method.Body.GetILProcessor();

            var firstInstruction = processor.WalkBack(instruction);

            processor.InsertBefore(firstInstruction, processor.Create(OpCodes.Ldarg_0));
            processor.InsertBefore(firstInstruction, processor.Create(OpCodes.Ldfld, chain));
            processor.InsertBefore(firstInstruction, processor.Create(OpCodes.Ldarg_0));
            processor.InsertBefore(firstInstruction, processor.Create(OpCodes.Ldfld, num));
            processor.InsertBefore(firstInstruction, processor.Create(OpCodes.Ldelem_Ref));

            processor.InsertBefore(instruction, processor.Create(OpCodes.Ldarg_0));
            processor.InsertBefore(instruction, processor.Create(OpCodes.Ldfld, chain));
            processor.InsertBefore(instruction, processor.Create(OpCodes.Ldarg_0));
            processor.InsertBefore(instruction, processor.Create(OpCodes.Ldfld, num));
            processor.InsertBefore(instruction, processor.Create(OpCodes.Ldc_I4_1));
            processor.InsertBefore(instruction, processor.Create(OpCodes.Add));

            instruction.Operand = method.DeclaringType.Module.ImportReference(@delegate.Invoke);
        }

        private static void ReplaceMethodCallWithNextCall(MethodDefinition method, MonoHelper.Delegate @delegate, ParameterDefinition chain, ParameterDefinition num, Instruction instruction)
        {
            var processor = method.Body.GetILProcessor();

            var firstInstruction = processor.WalkBack(instruction);

            processor.InsertBefore(firstInstruction, processor.Create(OpCodes.Ldarg, chain));
            processor.InsertBefore(firstInstruction, processor.Create(OpCodes.Ldarg, num));
            processor.InsertBefore(firstInstruction, processor.Create(OpCodes.Ldelem_Ref));

            processor.InsertBefore(instruction, processor.Create(OpCodes.Ldarg, chain));
            processor.InsertBefore(instruction, processor.Create(OpCodes.Ldarg, num));
            processor.InsertBefore(instruction, processor.Create(OpCodes.Ldc_I4_1));
            processor.InsertBefore(instruction, processor.Create(OpCodes.Add));

            instruction.Operand = method.DeclaringType.Module.ImportReference(@delegate.Invoke);
        }

        private static void ParseType(PrivateContext context, TypeDefinition type)
        {
            if (type.BaseType != null && type.BaseType.FullName == "System.MulticastDelegate" && type.HasCustomAttributes && type.CustomAttributes[0].Constructor.DeclaringType.FullName == "ModAPI.MethodName" && !context.Delegates.ContainsKey((string)type.CustomAttributes[0].ConstructorArguments[0].Value))
            {
                context.Delegates.Add((string)type.CustomAttributes[0].ConstructorArguments[0].Value, new MonoHelper.Delegate(type));
            }
            var typeName = type.FullName;
            if (!context.AllTypes.ContainsKey(typeName))
                context.AllTypes.Add(typeName, type);
            /*else
                Logger.Warn("Type \"" + typeName + "\" was already added.");*/

            foreach (var nestedType in type.NestedTypes)
                ParseType(context, nestedType);

            foreach (var method in type.Methods)
            {
                var name = method.FullName;
                if (context.AllMethods.ContainsKey(name))
                    context.AllMethods.Add(name, method);
                /*else
                    Logger.Warn("Method with name \"" + name + "\" was already added.");*/

/*                if (context.AllMethods.ContainsKey(method.FullName))
                    context.AllMethods.Add(method.FullName, method);
                else
                    Logger.Warn("Method with name \"" + method.FullName + "\" was already added.");*/
            }
            foreach (var property in type.Properties)
            {
                var name = property.FullName;
                if (context.AllProperties.ContainsKey(name))
                    context.AllProperties.Add(name, property);
                /*else
                    Logger.Warn("Property with name \"" + name + "\" was already added.");*/
            }
        }
    }
}
