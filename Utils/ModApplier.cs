using ModAPI.ViewModels;
using ModAPI.ViewModels.ModProject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using System.IO;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Cecil;

namespace ModAPI.Utils
{
    internal class ModApplier
    {
        private static NLog.ILogger Logger = NLog.LogManager.GetLogger("ModApplier");

        public class Context
        {
            public Game Game;
            public List<Mod> Mods;
            public ProgressHandler ProgressHandler;
        }


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

            /*
            public MethodDefinition FindBaseMethod(TypeReference currentBaseType, MethodDefinition method, MonoHelper.SignatureContext context)
            {
                //var currentBaseType = method.DeclaringType.BaseType;
                while (currentBaseType != null)
                {
                    var baseTypeName = GetFullyQualifiedName(currentBaseType);
                    if (AllTypes.ContainsKey(baseTypeName))
                    {
                        var baseType = AllTypes[baseTypeName];
                        foreach (var m in baseType.Methods)
                        {
                            if (m.MatchingSignature(method, context))
                            {
                                return m;
                            }
                        }
                        currentBaseType = baseType.BaseType;
                    }
                    else break;
                }
                return null;
            }*/
        }

        public static void Execute(Context ctxt)
        {
            /*try
            {*/
            var context = new PrivateContext(ctxt);
            var libraries = context.Game.ModLibrary.Libraries;
            var baseModLibPath = Path.Combine(context.Game.ModLibrary.LibraryDirectory, "BaseModLib.dll");
            if (!File.Exists(baseModLibPath))
                throw new Exception("BaseModLib wasn't found");

            var c = 0;
            context.ProgressHandler.ChangeProgress("Loading BaseModLib.dll...", 0f);
            context.BaseModLib = AssemblyDefinition.ReadAssembly(baseModLibPath);
            context.InitializeBaseModLib();
            c++;

            var moddedDirectory = Path.Combine(context.Game.GameDirectory, "ModAPI", "Modded");
            if (!Directory.Exists(moddedDirectory))
                Directory.CreateDirectory(moddedDirectory);

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(context.Game.Backup.BackupDirectory);
            resolver.AddSearchDirectory(moddedDirectory);

            var readerParameters = new ReaderParameters()
            {
                AssemblyResolver = resolver
            };

            context.ProgressHandler.ChangeProgress("Loading mod library assemblies (1/" + (libraries.Count) + ")...", 0.01f);
            foreach (var library in libraries)
            {
                var modLibraryAssembly = AssemblyDefinition.ReadAssembly(library.File, readerParameters);
                context.ModLibraryAssemblies[modLibraryAssembly.FullName] = modLibraryAssembly;
                foreach (var type in modLibraryAssembly.MainModule.Types)
                {
                    ParseModLibraryType(context, type);
                }
                c++;
                context.ProgressHandler.ChangeProgress("Loading mod library assemblies (" + (c + 1) + "/" + (libraries.Count) + ")...", 0.01f + 0.19f * ((float)c / (float)(libraries.Count)));
            }

            c = 0;
            context.ProgressHandler.ChangeProgress("Loading game library assemblies (1/" + (libraries.Count) + ")...", 0.01f);
            foreach (var library in libraries)
            {
                var assembly = AssemblyDefinition.ReadAssembly(Path.Combine(context.Game.Backup.BackupDirectory, Path.GetFileName(library.File)), readerParameters);
                context.Assemblies[assembly.FullName] = assembly;
                foreach (var type in assembly.MainModule.Types)
                {
                    ParseType(context, type);
                }
                c++;
                context.ProgressHandler.ChangeProgress("Loading game library assemblies (" + (c + 1) + "/" + (libraries.Count) + ")...", 0.2f + 0.2f * ((float)c / (float)(libraries.Count)));
            }
            
            context.ProgressHandler.ChangeProgress("Analyizing mods (1/" + (context.Mods.Count) + ")...", 0.4f);
            c = 0;
            foreach (var mod in context.Mods)
            {
                var modAssembly = AssemblyDefinition.ReadAssembly(Path.Combine(mod.File), readerParameters);
                context.ModAssemblies.Add(Path.GetFileNameWithoutExtension(mod.File), modAssembly);
                foreach (var type in modAssembly.MainModule.Types)
                {
                    ParseModType(context, type);
                }
                c++;
                context.ProgressHandler.ChangeProgress("Analyzing mods (" + (c + 1) + "/" + (context.Mods.Count) + ")...", 0.4f + 0.2f * ((float)c / (float)(context.Mods.Count)));
            }

            var internalsVisibleToAttribute = context.AllTypes["System.Runtime.CompilerServices.InternalsVisibleToAttribute"];
            var internalsVisibleToConstructor = internalsVisibleToAttribute.Methods.First(m => m.Name == ".ctor");

            foreach (var mAssembly in context.ModAssemblies)
            {
                var str_bytes = System.Text.Encoding.UTF8.GetBytes(mAssembly.Value.Name.Name);
                var bytes = new byte[str_bytes.Length + 4];
                Array.Copy(str_bytes, 0, bytes, 2, str_bytes.Length);
                bytes[0] = 0x01;
                foreach (var assembly in context.Assemblies)
                {
                    assembly.Value.CustomAttributes.Add(new CustomAttribute(assembly.Value.MainModule.ImportReference(internalsVisibleToConstructor), bytes));
                }
            }

            var totalCount = context.FieldToProperty.Count;
            var fieldsToProperty = context.FieldToProperty.Keys.ToList();
            c = 1;
            context.ProgressHandler.ChangeProgress("Converting fields to properties... (" + c + "/" + totalCount + ")", 0.6f);
            foreach (var fieldName in fieldsToProperty)
            {
                if (context.AllFields.ContainsKey(fieldName))
                {
                    var field = context.AllFields[fieldName];

                    Logger.Debug("Found field \"" + field.Name + "\". Changing it to a property...");

                    var newProperty = new PropertyDefinition("__ModAPI_" + field.Name, PropertyAttributes.None, field.FieldType);
                    newProperty.HasThis = !field.IsStatic;

                    var getMethod = new MethodDefinition("get___ModAPI_" + field.Name, MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, field.FieldType);
                    getMethod.SemanticsAttributes = MethodSemanticsAttributes.Getter;
                    var setMethod = new MethodDefinition("set___ModAPI_" + field.Name, MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, field.Module.TypeSystem.Void);
                    setMethod.SemanticsAttributes = MethodSemanticsAttributes.Setter;

                    getMethod.ReturnType = field.FieldType;
                    ParameterDefinition valueParameter = null;
                    setMethod.Parameters.Add(valueParameter = new ParameterDefinition("value", ParameterAttributes.None, field.FieldType));
                    setMethod.ReturnType = field.Module.TypeSystem.Void;

                    if (field.IsStatic)
                    {
                        var getIL = getMethod.Body.GetILProcessor();
                        getIL.Emit(OpCodes.Nop);
                        getIL.Emit(OpCodes.Ldsfld, field);
                        getIL.Emit(OpCodes.Ret);

                        var setIL = setMethod.Body.GetILProcessor();
                        setIL.Emit(OpCodes.Nop);
                        setIL.Emit(OpCodes.Ldarg_0);
                        setIL.Emit(OpCodes.Stsfld, field);
                        setIL.Emit(OpCodes.Ret);
                    }
                    else
                    {
                        var getIL = getMethod.Body.GetILProcessor();
                        getIL.Emit(OpCodes.Nop);
                        getIL.Emit(OpCodes.Ldarg_0);
                        getIL.Emit(OpCodes.Ldfld, field);
                        getIL.Emit(OpCodes.Ret);

                        var setIL = setMethod.Body.GetILProcessor();
                        setIL.Emit(OpCodes.Nop);
                        setIL.Emit(OpCodes.Ldarg_0);
                        setIL.Emit(OpCodes.Ldarg, valueParameter);
                        setIL.Emit(OpCodes.Stfld, field);
                        setIL.Emit(OpCodes.Ret);
                    }
                    field.DeclaringType.Methods.Add(getMethod);
                    field.DeclaringType.Methods.Add(setMethod);

                    newProperty.SetMethod = setMethod;
                    newProperty.GetMethod = getMethod;

                    getMethod.Body.Optimize();
                    setMethod.Body.Optimize();

                    field.DeclaringType.Properties.Add(newProperty);

                    context.FieldToProperty[fieldName] = newProperty;
                    
                    context.AllMethods.Add(getMethod.FullName, getMethod);
                    context.AllMethods.Add(setMethod.FullName, setMethod);
                    context.AllProperties.Add(newProperty.FullName, newProperty);
                    Logger.Debug("Property \"" + newProperty.Name + "\" created with getter and setter!");
                }
                c++;
                context.ProgressHandler.ChangeProgress("Converting fields to properties... (" + c + "/" + totalCount + ")", 0.6f);
            }

            foreach (var type in context.AllTypes)
            {
                RewriteMethodBodies(context, type.Value);
            }

            totalCount = context.AccessedFields.Count;
            c = 1;
            context.ProgressHandler.ChangeProgress("Making accessed non-public fields internal... (" + c + "/" + totalCount + ")", 0.6f);
            foreach (var accessedField in context.AccessedFields)
            {
                if (context.AllFields.ContainsKey(accessedField))
                {
                    Logger.Info("Making field " + accessedField + " public...");
                    var field = context.AllFields[accessedField];
                    if (!field.IsPublic)
                        field.IsAssembly = true;
                    //field.IsPublic = true;
                }
                c++;
                context.ProgressHandler.ChangeProgress("Making accessed non-public fields internal... (" + c + "/" + totalCount + ")", 0.6f);
            }

            totalCount = context.AccessedMethods.Count;
            c = 1;
            context.ProgressHandler.ChangeProgress("Making accessed non-public methods internal... (" + c + "/" + totalCount + ")", 0.6f);
            foreach (var accessedMethod in context.AccessedMethods)
            {
                if (context.AllMethods.ContainsKey(accessedMethod))
                {
                    Logger.Info("Making field " + accessedMethod + " public...");
                    var method = context.AllMethods[accessedMethod];
                    if (!method.IsPublic)
                        method.IsAssembly = true;
                }
                c++;
                context.ProgressHandler.ChangeProgress("Making accessed non-public methods internal... (" + c + "/" + totalCount + ")", 0.6f);
            }

            Dictionary<string, HashSet<string>> delegateNames = new Dictionary<string, HashSet<string>>();

            foreach (var kv in context.Replacements)
            {
                if (context.AllTypes.ContainsKey(kv.Value.BaseType) && context.AllMethods.ContainsKey(kv.Value.FullMethodName))
                {
                    var baseType = context.AllTypes[kv.Value.BaseType];
                    var baseMethod = context.AllMethods[kv.Value.FullMethodName];
                    
                    MethodDefinition originalMethod = null;
                    MethodDefinition baseTypeMethod = null;
                    foreach (var method in baseType.Methods)
                    {
                        if (method == baseMethod || method.GetOriginalBaseMethod() == baseMethod)
                            baseTypeMethod = method;
                    }
                    if (baseTypeMethod == null)
                    {
                        var newMethod = new MethodDefinition(baseMethod.Name, baseMethod.Attributes, baseMethod.ReturnType);
                        newMethod.IsAbstract = false;
                        baseTypeMethod = newMethod;
                        baseType.Methods.Add(baseTypeMethod);
                    }
                    else
                    {
                        var copy = baseTypeMethod.Copy();
                        copy.Name = "__ModAPI_" + baseTypeMethod.Name + "_Original";
                        if (!copy.IsStatic)
                        {
                            copy.IsStatic = true;
                            copy.HasThis = false;
                            copy.IsVirtual = false;
                            copy.IsNewSlot = false;
                            copy.Parameters.Insert(0, new ParameterDefinition("self", ParameterAttributes.None, baseMethod.DeclaringType));
                        }
                        baseType.Methods.Add(copy);
                        originalMethod = copy;
                    }


                    baseTypeMethod.Body.Variables.Clear();
                    baseTypeMethod.Body.Instructions.Clear();

                    var baseTypeMethodBody = baseTypeMethod.Body;
                    var processor = baseTypeMethodBody.GetILProcessor();

                    VariableDefinition returnVariable = null;
                    if (originalMethod.ReturnType.FullName != "System.Void")
                    {
                        returnVariable = new VariableDefinition(originalMethod.ReturnType);
                        baseTypeMethodBody.Variables.Add(returnVariable);
                        processor.Append(processor.Create(OpCodes.Nop));
                        processor.AppendAssignStandardValue(returnVariable, baseType.Module.ImportReference(originalMethod.ReturnType));
                    }

                    /*
                    if (kv.Value.Injections.ContainsKey(Injection.Type.HookBefore) && kv.Value.Injections[Injection.Type.HookBefore].Count > 0)
                    {
                        for (var i = 0; i < kv.Value.Injections[Injection.Type.HookBefore].Count; i++)
                        {
                            var exceptionVariable = new VariableDefinition(originalMethod.Module.ImportReference(context.AllTypes["System.Exception"]));
                            var hookBefore = kv.Value.Injections[Injection.Type.HookBefore][i];
                            var args = (originalMethod.IsStatic ? 0 : 1) + originalMethod.Parameters.Count;
                            Instruction tryStart = processor.Create(OpCodes.Nop);
                            processor.Append(tryStart);
                            for (var j = 0; j < args; j++)
                            {
                                if (j == 0)
                                    processor.Append(processor.Create(OpCodes.Ldarg_0));
                                else if (j == 1)
                                    processor.Append(processor.Create(OpCodes.Ldarg_1));
                                else if (j == 2)
                                    processor.Append(processor.Create(OpCodes.Ldarg_2));
                                else if (j == 3)
                                    processor.Append(processor.Create(OpCodes.Ldarg_3));
                                else
                                    processor.Append(processor.Create(OpCodes.Ldarg, j));
                            }
                            processor.Append(processor.Create(OpCodes.Call, originalMethod.Module.ImportReference(hookBefore)));
                            var leave = processor.Create(OpCodes.Leave);
                            processor.Append(leave);

                            var catchStart = processor.Create(OpCodes.Stloc, exceptionVariable);
                            processor.Append(catchStart);
                            var catchEnd = processor.Create(OpCodes.Rethrow);
                            processor.Append(catchEnd);
                            var last = processor.Create(OpCodes.Nop, exceptionVariable);
                            processor.Append(last);
                            leave.Operand = last;

                            originalMethod.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
                            {
                                TryStart = tryStart,
                                TryEnd = leave,
                                CatchType = originalMethod.Module.ImportReference(context.AllTypes["System.Exception"]),
                                HandlerStart = catchStart,
                                HandlerEnd = catchEnd
                            });
                        }
                    }*/

                    if (kv.Value.Injections.ContainsKey(Injection.Type.HookBefore) && kv.Value.Injections[Injection.Type.HookBefore].Count > 0)
                    {
                        var hookBeforeMethods = kv.Value.Injections[Injection.Type.HookBefore];
                        foreach (var method in hookBeforeMethods)
                        {
                            if (!baseTypeMethod.IsStatic)
                                processor.Append(processor.Create(OpCodes.Ldarg_0));
                            for (var i = 0; i < baseTypeMethod.Parameters.Count; i++)
                                processor.Append(processor.Create(OpCodes.Ldarg, baseTypeMethod.Parameters[i]));
                            processor.Append(processor.Create(OpCodes.Call, baseType.Module.ImportReference(method.Method)));
                            processor.Append(processor.Create(OpCodes.Nop));
                        }
                    }

                    if (kv.Value.Injections.ContainsKey(Injection.Type.Chain) && kv.Value.Injections[Injection.Type.Chain].Count > 0)
                    {
                        var chainMethods = kv.Value.Injections[Injection.Type.Chain];

                        if (!delegateNames.ContainsKey(baseType.FullName))
                            delegateNames.Add(baseType.FullName, new());
                        var typeDelegateNames = delegateNames[baseType.FullName];
                        if (originalMethod.IsConstructor)
                            continue;
                        var originalMethodName = baseTypeMethod.Name;
                        var delegateName = "__ModAPI_Delegate_" + originalMethodName.Replace("__ModAPI_", "");
                        int d = 0;
                        while (typeDelegateNames.Contains(delegateName))
                        {
                            delegateName = "__ModAPI_Delegate_" + originalMethodName.Replace("__ModAPI_", "") + d;
                            d++;
                        }
                        typeDelegateNames.Add(delegateName);

                        var @params = baseTypeMethod.Parameters.ToList();
                        if (!baseTypeMethod.IsStatic)
                            @params.Insert(0, new ParameterDefinition("self", ParameterAttributes.None, baseType));
                        
                        //@params.Add(new ParameterDefinition("self", ParameterAttributes.None, baseType.Module.TypeSystem.Int32));
                        var @delegate = MonoHelper.CreateDelegate(baseType, baseTypeMethod.ReturnType, delegateName, true, @params, context.AllTypes["System.MulticastDelegate"], context.AllTypes["System.AsyncCallback"], context.AllTypes["System.IAsyncResult"]);
                        var delegateType = @delegate.Type;

                        context.Delegates.Add(@delegate.Type.FullName, @delegate);

                        var delegateArrayType = delegateType.MakeArrayType();
                        var chainField = new FieldDefinition("__ModAPI_" + baseTypeMethod.Name + "_Chain", FieldAttributes.Static | FieldAttributes.Private, baseTypeMethod.Module.ImportReference(delegateArrayType));
                        baseType.Fields.Add(chainField);

                        if (originalMethod != null)
                        {
                            originalMethod.Parameters.Add(new ParameterDefinition("__modapi_chain_methods", ParameterAttributes.None, originalMethod.Module.ImportReference(delegateType.MakeArrayType())));
                            originalMethod.Parameters.Add(new ParameterDefinition("__modapi_chain_num", ParameterAttributes.None, originalMethod.Module.TypeSystem.Int32));
                        }
                        MethodDefinition invokeMethod = null;
                        MethodDefinition delegateConstructor = null;

                        for (var i = 0; i < delegateType.Methods.Count; i++)
                        {
                            if (delegateType.Methods[i].Name == "Invoke")
                                invokeMethod = delegateType.Methods[i];
                            if (delegateType.Methods[i].IsConstructor)
                                delegateConstructor = delegateType.Methods[i];
                        }

                        var comparisionBool = new VariableDefinition(baseTypeMethod.Module.TypeSystem.Boolean);
                        baseTypeMethod.Body.Variables.Add(comparisionBool);

                        processor.Append(processor.Create(OpCodes.Ldsfld, baseTypeMethod.Module.ImportReference(chainField)));
                        processor.Append(processor.Create(OpCodes.Ldnull));
                        processor.Append(processor.Create(OpCodes.Ceq));
                        processor.Append(processor.Create(OpCodes.Stloc, comparisionBool));
                        processor.Append(processor.Create(OpCodes.Ldloc, comparisionBool));
                        var breakInstruction = processor.Create(OpCodes.Brfalse, baseTypeMethod.Body.Instructions[0]);
                        processor.Append(breakInstruction);
                        processor.Append(processor.Create(OpCodes.Nop));
                        processor.Append(processor.Create(OpCodes.Ldc_I4, chainMethods.Count + (originalMethod != null ? 1 : 0)));
                        processor.Append(processor.Create(OpCodes.Newarr, baseTypeMethod.Module.ImportReference(delegateType)));
                        processor.Append(processor.Create(OpCodes.Stsfld, baseTypeMethod.Module.ImportReference(chainField)));
                        for (var i = 0; i < chainMethods.Count; i++)
                        {
                            processor.Append(processor.Create(OpCodes.Ldsfld, baseTypeMethod.Module.ImportReference(chainField)));
                            processor.Append(processor.Create(OpCodes.Ldc_I4, i));
                            processor.Append(processor.Create(OpCodes.Ldnull));
                            processor.Append(processor.Create(OpCodes.Ldftn, baseTypeMethod.Module.ImportReference(chainMethods[i].Method)));
                            processor.Append(processor.Create(OpCodes.Newobj, baseTypeMethod.Module.ImportReference(delegateConstructor)));
                            processor.Append(processor.Create(OpCodes.Stelem_Ref));
                        }

                        if (originalMethod != null)
                        {
                            processor.Append(processor.Create(OpCodes.Ldsfld, baseTypeMethod.Module.ImportReference(chainField)));
                            processor.Append(processor.Create(OpCodes.Ldc_I4, chainMethods.Count));
                            processor.Append(processor.Create(OpCodes.Ldnull));
                            processor.Append(processor.Create(OpCodes.Ldftn, baseTypeMethod.Module.ImportReference(originalMethod)));
                            processor.Append(processor.Create(OpCodes.Newobj, baseTypeMethod.Module.ImportReference(delegateConstructor)));
                            processor.Append(processor.Create(OpCodes.Stelem_Ref));
                        }

                        var nop = processor.Create(OpCodes.Nop);
                        processor.Append(nop);
                        breakInstruction.Operand = nop;

                        processor.Append(processor.Create(OpCodes.Ldsfld, baseTypeMethod.Module.ImportReference(chainField)));
                        processor.Append(processor.Create(OpCodes.Ldc_I4_0));
                        processor.Append(processor.Create(OpCodes.Ldelem_Ref));
                        
                        if (!baseTypeMethod.IsStatic)
                            processor.Append(processor.Create(OpCodes.Ldarg_0));

                        for (var i = 0; i < baseTypeMethod.Parameters.Count; i++)
                            processor.Append(processor.Create(OpCodes.Ldarg, baseTypeMethod.Parameters[i]));

                        processor.Append(processor.Create(OpCodes.Ldsfld, baseTypeMethod.Module.ImportReference(chainField)));
                        processor.Append(processor.Create(OpCodes.Ldc_I4_1));
                        processor.Append(processor.Create(OpCodes.Callvirt, baseTypeMethod.Module.ImportReference(invokeMethod)));
                        if (returnVariable != null)
                            processor.Append(processor.Create(OpCodes.Stloc, returnVariable));

                        processor.Append(processor.Create(OpCodes.Nop));

                        /*
                        for (var i = 0; i < kv.Value[Injection.Type.Chain].Count; i++)
                        {
                            var chain = kv.Value[Injection.Type.Chain][i];
                            var exceptionVariable = new VariableDefinition(chain.Module.ImportReference(context.AllTypes["System.Exception"]));
                            chain.Body.Variables.Add(exceptionVariable);

                            var chainProcessor = chain.Body.GetILProcessor();
                            for (var j = 0; j < chain.Body.Instructions.Count; j++)
                            {
                                var instruction = chain.Body.Instructions[j];
                                ReplacePropertyAccessor(context, instruction, chain.Module);
                                if (instruction.OpCode.Code == Code.Callvirt && instruction.Operand is MethodReference methodRef && methodRef.FullName == invokeMethod.FullName)
                                {
                                    if (i < kv.Value[Injection.Type.Chain].Count - 2)
                                    {
                                        chainProcessor.InsertBefore(instruction, chainProcessor.Create(OpCodes.Ldftn, chain.Module.ImportReference(kv.Value[Injection.Type.Chain][i + 2])));
                                        chainProcessor.InsertBefore(instruction, chainProcessor.Create(OpCodes.Newobj, chain.Module.ImportReference(delegateConstructor)));
                                        j += 2;
                                    }
                                    if (i < kv.Value[Injection.Type.Chain].Count - 1)
                                    {
                                        chainProcessor.InsertBefore(instruction, chainProcessor.Create(OpCodes.Ldftn, chain.Module.ImportReference(copy)));
                                        chainProcessor.InsertBefore(instruction, chainProcessor.Create(OpCodes.Newobj, chain.Module.ImportReference(delegateConstructor)));
                                        j += 2;
                                    }
                                    else
                                    {
                                        //chainProcessor.InsertBefore(instruction, chainProcessor.Create(OpCodes.Ldnull));
                                        j++;
                                    }
                                }
                            }

                            Instruction tryStart = chain.Body.Instructions[0];
                            //var leave = chainProcessor.Create(OpCodes.Leave, chain.Body.Instructions[0]);
                            //chainProcessor.Append(leave);


                            var catchStart = chainProcessor.Create(OpCodes.Stloc, exceptionVariable);
                            chainProcessor.Append(catchStart);
                            var catchEnd = processor.Create(OpCodes.Rethrow);
                            chainProcessor.Append(catchEnd);
                            //var last = processor.Create(OpCodes.Nop);
                            //chainProcessor.Append(last);
                            //leave.Operand = last;

                            chain.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Catch)
                            {
                                TryStart = tryStart,
                                TryEnd = catchStart,
                                CatchType = chain.Module.ImportReference(context.AllTypes["System.Exception"]),
                                HandlerStart = catchStart,
                                HandlerEnd = catchEnd.Next
                            });
                        }*/
                    }

                    if (kv.Value.Injections.ContainsKey(Injection.Type.HookAfter) && kv.Value.Injections[Injection.Type.HookAfter].Count > 0)
                    {
                        var hookAfterMethods = kv.Value.Injections[Injection.Type.HookAfter];
                        foreach (var method in hookAfterMethods)
                        {
                            if (!baseTypeMethod.IsStatic)
                                processor.Append(processor.Create(OpCodes.Ldarg_0));
                            for (var i = 0; i < baseTypeMethod.Parameters.Count; i++)
                                processor.Append(processor.Create(OpCodes.Ldarg, baseTypeMethod.Parameters[i]));
                            if (returnVariable != null)
                                processor.Append(processor.Create(OpCodes.Ldloca_S, returnVariable));
                            processor.Append(processor.Create(OpCodes.Call, baseType.Module.ImportReference(method.Method)));
                            processor.Append(processor.Create(OpCodes.Nop));
                        }
                    }
                    if (returnVariable != null)
                        processor.Append(processor.Create(OpCodes.Ldloc, returnVariable));
                    processor.Append(processor.Create(OpCodes.Ret));
                }
            }

            foreach (var assembly in context.Assemblies)
            {
                if (context.ModLibraryAssemblies.ContainsKey(assembly.Key))
                {
                    var modLibraryAssembly = context.ModLibraryAssemblies[assembly.Key];
                    foreach (var r in modLibraryAssembly.MainModule.Resources)
                    {
                        if (r is EmbeddedResource embedded && embedded.Name == "ModInformation")
                        {
                            assembly.Value.MainModule.Resources.Add(new EmbeddedResource("ModInformation", ManifestResourceAttributes.Public, embedded.GetResourceData()));
                            break;
                        }
                    }
                }
                assembly.Value.MainModule.AssemblyReferences.Add(new AssemblyNameReference("BaseModLib", Data.ModAPI.BaseModLib.Name.Version));
                assembly.Value.Write(Path.Combine(moddedDirectory, assembly.Value.Name.Name + ".dll"));
                //System.IO.File.Copy(Path.Combine(moddedDirectory, assembly.Value.Name.Name + ".dll"), Path.Combine(context.Game.ManagedDirectory, assembly.Value.Name.Name + ".dll"));
                assembly.Value.Dispose();
            }

            foreach (var assembly in context.ModAssemblies)
            {
                foreach (var type in assembly.Value.MainModule.Types)
                    RewriteModType(context, type);

                assembly.Value.Write(Path.Combine(moddedDirectory, assembly.Value.Name.Name + ".dll"));
                
                assembly.Value.Dispose();
            }

            foreach (var assembly in context.ModLibraryAssemblies)
            {
                assembly.Value.Dispose();
            }
        }

        private static void ReplacePropertyAccessor(PrivateContext context, Instruction instruction, ModuleDefinition module)
        {
            string methodName = null;
            if (instruction.Operand is MethodReference _methodRef && !context.AccessedMethods.Contains(_methodRef.FullName))
            {
                methodName = _methodRef.FullName;
                if (methodName != null)
                {
                    if (context.ModLibraryMethods.ContainsKey(methodName))
                    {
                        var modLibraryMethod = context.ModLibraryMethods[methodName];
                        foreach (var attr in modLibraryMethod.CustomAttributes)
                        {
                            if (attr.Constructor.DeclaringType.FullName == "ModAPI.FormerField")
                            {
                                var fieldName = (string)attr.ConstructorArguments[0].Value;

                                if (context.FieldToProperty.ContainsKey(fieldName))
                                {
                                    if (_methodRef.Parameters.Count == 1)
                                        instruction.Operand = module.ImportReference(context.FieldToProperty[fieldName].SetMethod);
                                    else
                                        instruction.Operand = module.ImportReference(context.FieldToProperty[fieldName].GetMethod);
                                }
                            }
                            /*else if (attr.Constructor.DeclaringType.FullName == "ModAPI.PropertyName")
                            {
                                var propertyName = (string)attr.ConstructorArguments[0].Value;
                                if (!context.CompleteProperties.Contains(propertyName))
                                    context.CompleteProperties.Add(propertyName);
                            }*/
                        }
                    }
                }
            }
        }
        private void CopyMethod(MethodDefinition original, TypeDefinition newType)
        {
            var newMethod = new MethodDefinition(original.Name, original.Attributes, newType.Module.ImportReference(original.ReturnType));
            /*foreach (var genericParameter in original.GenericParameters)
            {
                newMethod.GenericParameters.Add(CopyGenericParameter(genericParameter, newType.Module));
            }*/
            foreach (var param in original.Parameters)
            {
                var newParam = new ParameterDefinition(param.Name, param.Attributes, newType.Module.ImportReference(param.ParameterType));
            }
            foreach (var attr in original.CustomAttributes)
            {
                var newAttr = new CustomAttribute(newType.Module.ImportReference(attr.Constructor), attr.GetBlob());
                newMethod.CustomAttributes.Add(newAttr);
            }
        }

        private void CopyGenericParameter(GenericParameter parameter, ModuleDefinition module)
        {
        }

        private static void RewriteMethodBodies(PrivateContext context, TypeDefinition type)
        {
            foreach (var nestedType in type.NestedTypes)
            {
                RewriteMethodBodies(context, nestedType);
            }
            foreach (var method in type.Methods)
            {
                RewriteMethodBody(context, method);
            }
        }

        private static void RewriteModType(PrivateContext context, TypeDefinition type)
        {
            foreach (var nestedType in type.NestedTypes)
            {
                RewriteModType(context, nestedType);
            }
            foreach (var field in type.Fields)
            {
                if (field.FieldType.IsArray && context.Delegates.ContainsKey(field.FieldType.GetElementType().FullName))
                    field.FieldType = field.Module.ImportReference(context.Delegates[field.FieldType.GetElementType().FullName].Type.MakeArrayType());
            }
            foreach (var method in type.Methods)
            {
                RewriteModMethod(context, method);
            }
        }
        private static void RewriteMethodBody(PrivateContext context, MethodDefinition method)
        {
            if (method.Body == null)
                return;
            foreach (var instruction in method.Body.Instructions)
            {
                /*if (instruction.Operand is MethodReference methodRef && context.NewMethods.ContainsKey(methodRef.FullName))
                    instruction.Operand = method.Module.ImportReference(context.NewMethods[methodRef.FullName]);*/
                if (instruction.Operand is FieldReference fieldRef && context.FieldToProperty.ContainsKey(fieldRef.FullName))
                {
                    var prop = context.FieldToProperty[fieldRef.FullName];
                    if ((instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Ldsfld) && method.FullName != prop.GetMethod.FullName)
                    {
                        instruction.OpCode = OpCodes.Callvirt;
                        instruction.Operand = method.Module.ImportReference(prop.GetMethod);
                    }
                    else if ((instruction.OpCode == OpCodes.Stfld || instruction.OpCode == OpCodes.Stsfld) && method.FullName != prop.SetMethod.FullName)
                    {
                        instruction.OpCode = OpCodes.Callvirt;
                        instruction.Operand = method.Module.ImportReference(prop.SetMethod);
                    }
                }

            }
        }

        private static void RewriteModMethod(PrivateContext context, MethodDefinition method)
        {
            var module = method.DeclaringType.Module;
            for (var i = 0; i < method.Parameters.Count; i++)
            {
                if (method.Parameters[i].ParameterType.IsArray && context.Delegates.ContainsKey(method.Parameters[i].ParameterType.GetElementType().FullName))
                    method.Parameters[i].ParameterType = module.ImportReference(context.Delegates[method.Parameters[i].ParameterType.GetElementType().FullName].Type.MakeArrayType());
            }
            if (method.Body == null)
                return;
            foreach (var instruction in method.Body.Instructions)
            {
                /*if (instruction.Operand is MethodReference methodRef && context.NewMethods.ContainsKey(methodRef.FullName))
                    instruction.Operand = method.Module.ImportReference(context.NewMethods[methodRef.FullName]);*/
                if (instruction.Operand is MethodReference methodRef && context.Delegates.ContainsKey(methodRef.DeclaringType.FullName))
                {
                    var @delegate = context.Delegates[methodRef.DeclaringType.FullName];
                    if (methodRef.Name == "Invoke")
                        instruction.Operand = module.ImportReference(@delegate.Invoke);
                    if (methodRef.Name == ".ctor")
                        instruction.Operand = module.ImportReference(@delegate.Constructor);
                }

            }
        }

        private static void ParseModType(PrivateContext context, TypeDefinition type)
        {
            foreach (var nestedType in type.NestedTypes)
                ParseModType(context, nestedType);

            List<MethodDefinition> foundMethods = new List<MethodDefinition>();
            int c = type.Methods.Count;
            for (var k = 0; k < c; k++)
            {
                var method = type.Methods[k];
                var methodRef = type.Module.ImportReference(method);
                var genericFullName = methodRef.GetGenericFullName();
                context.Test.Add(genericFullName, methodRef);

                /*var newMethod = method.Copy();
                newMethod.Name = "Clone_" + newMethod.Name;
                type.Methods.Add(newMethod);*/
                foreach (var instruction in method.Body.Instructions)
                {
                    if (instruction.Operand is MethodReference @ref1)
                    {
                        var genericMethodFullName = @ref1.GetGenericFullName();
                    }
                    if (instruction.Operand is GenericInstanceMethod @ref2)
                    {
                        var genericMethodFullName = @ref2.GetGenericFullName();
                        if (context.Test.ContainsKey(genericMethodFullName))
                        {
                            var clone = context.Test[genericMethodFullName].CloneGenericInstance(@ref2);
                            instruction.Operand = clone;
                        }
                    }
                }
                Injection.Type injectionType = Injection.Type.Chain;
                string typeName = null;
                string fullFieldName = null;
                string fullPropertyName = null;
                string fullMethodName = null;
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
                            if (property.Name == "TypeName")
                                typeName = (string)property.Argument.Value;
                            if (property.Name == "FullFieldName")
                                fullFieldName = (string)property.Argument.Value;
                            if (property.Name == "FullPropertyName")
                                fullPropertyName = (string)property.Argument.Value;
                            if (property.Name == "FullMethodName")
                                fullMethodName = (string)property.Argument.Value;
                        }
                    }
                }

                foreach (var instruction in method.Body.Instructions)
                {
                    string methodName = null;
                    if (instruction.Operand is MethodReference _methodRef && !context.AccessedMethods.Contains(_methodRef.FullName))
                        methodName = _methodRef.FullName;
                    if (instruction.Operand is MethodDefinition _methodDef && !context.AccessedMethods.Contains(_methodDef.FullName))
                        methodName = _methodDef.FullName;
                    if (methodName != null)
                    {
                        if (context.ModLibraryMethods.ContainsKey(methodName))
                        {
                            var modLibraryMethod = context.ModLibraryMethods[methodName];
                            foreach (var attr in modLibraryMethod.CustomAttributes)
                            {
                                if (attr.Constructor.DeclaringType.FullName == "ModAPI.FormerField")
                                {
                                    var fieldName = (string)attr.ConstructorArguments[0].Value;
                                    if (!context.FieldToProperty.ContainsKey(fieldName))
                                        context.FieldToProperty.Add(fieldName, null);
                                }
                                else if (attr.Constructor.DeclaringType.FullName == "ModAPI.PropertyName")
                                {
                                    var propertyName = (string)attr.ConstructorArguments[0].Value;
                                    if (!context.CompleteProperties.Contains(propertyName))
                                        context.CompleteProperties.Add(propertyName);
                                }
                            }
                        }
                    }

                    if (instruction.Operand is FieldReference _fieldRef && !context.AccessedFields.Contains(_fieldRef.FullName))
                        context.AccessedFields.Add(_fieldRef.FullName);
                    if (instruction.Operand is FieldDefinition _fieldDef && !context.AccessedFields.Contains(_fieldDef.FullName))
                        context.AccessedFields.Add(_fieldDef.FullName);
                }

                if (injectionAttribute != null)
                {
                    method.CustomAttributes.Remove(injectionAttribute);
                    if (fullFieldName != null && !context.FieldToProperty.ContainsKey(fullFieldName))
                        context.FieldToProperty.Add(fullFieldName, null);

                    var id = typeName + "::" + fullMethodName;
                    if (!context.Replacements.ContainsKey(id))
                        context.Replacements.Add(id, new Replacement() { BaseType = typeName, FullMethodName = fullMethodName, Injections = new() });
                    if (!context.Replacements[id].Injections.ContainsKey(injectionType))
                        context.Replacements[id].Injections.Add(injectionType, new());
                    context.Replacements[id].Injections[injectionType].Add(new ReplacementMethod() { Method = method });
                    
                    if (!context.MethodReplaces.ContainsKey(fullMethodName))
                        context.MethodReplaces.Add(fullMethodName, new());
                    if (!context.MethodReplaces[fullMethodName].ContainsKey(injectionType))
                        context.MethodReplaces[fullMethodName].Add(injectionType, new());
                    context.MethodReplaces[fullMethodName][injectionType].Add(method);
                }
            }
        }

        private static void ParseModLibraryType(PrivateContext context, TypeDefinition type)
        {
            var typeName = type.FullName;
            if (!context.ModLibraryTypes.ContainsKey(typeName))
                context.ModLibraryTypes.Add(typeName, type);

            foreach (var nestedType in type.NestedTypes)
                ParseModLibraryType(context, nestedType);

            foreach (var method in type.Methods)
            {
                var name = method.FullName;
                if (!context.ModLibraryMethods.ContainsKey(name))
                    context.ModLibraryMethods.Add(name, method);
            }
            foreach (var property in type.Properties)
            {
                var name = property.FullName;
                if (!context.ModLibraryProperties.ContainsKey(name))
                    context.ModLibraryProperties.Add(name, property);
            }
            foreach (var field in type.Fields)
            {
                var name = field.FullName;
                if (!context.ModLibraryFields.ContainsKey(name))
                    context.ModLibraryFields.Add(name, field);
            }
        }
        private static void ParseType(PrivateContext context, TypeDefinition type)
        {
            var typeName = type.FullName;
            if (!context.AllTypes.ContainsKey(typeName))
                context.AllTypes.Add(typeName, type);

            foreach (var nestedType in type.NestedTypes)
                ParseType(context, nestedType);

            foreach (var method in type.Methods)
            {
                var name = method.FullName;
                if (!context.AllMethods.ContainsKey(name))
                    context.AllMethods.Add(name, method);
            }
            foreach (var property in type.Properties)
            {
                var name = property.FullName;
                if (!context.AllProperties.ContainsKey(name))
                    context.AllProperties.Add(name, property);
            }
            foreach (var field in type.Fields)
            {
                var name = field.FullName;
                if (!context.AllFields.ContainsKey(name))
                    context.AllFields.Add(name, field);
            }
        }

        public class Replacement
        {
            public string BaseType;
            public string FullMethodName;
            public Dictionary<Injection.Type, List<ReplacementMethod>> Injections;
        }

        public class ReplacementMethod
        {
            public int Priority;
            public MethodDefinition Method;
        }
    }
}
