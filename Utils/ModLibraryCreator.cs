using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModAPI.ViewModels;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ModAPI.Utils
{
    class ModLibraryCreator
    {
        private static NLog.ILogger Logger = NLog.LogManager.GetLogger("ModLibraryCreator");

        public class Context
        {
            public List<Library> AssemblyFiles = new List<Library>();
            public string AssemblyResolverPath;
            public string SaveTo;
            public ProgressHandler ProgressHandler;
        }

        private class PrivateContext : Context
        {
            public Dictionary<string, AssemblyDefinition> SystemAssemblies = new Dictionary<string, AssemblyDefinition>();
            public Dictionary<string, AssemblyDefinition> Assemblies = new Dictionary<string, AssemblyDefinition>();
            public Dictionary<string, string> Checksums = new Dictionary<string, string>();
            public Dictionary<string, PropertyDefinition> NewProperties = new Dictionary<string, PropertyDefinition>();
            public Dictionary<string, PropertyDefinition> FieldToProperty = new Dictionary<string, PropertyDefinition>();
            public Dictionary<string, TypeDefinition> AllTypes = new Dictionary<string, TypeDefinition>();
            public HashSet<string> NewPropertyNames = new HashSet<string>();
            public HashSet<string> UnchangeableFields = new HashSet<string>();
            public MethodDefinition NonSerializedConstructor = null;
            public MethodDefinition FormerFieldConstructor = null;
            public MethodDefinition PropertyNameConstructor = null;
            public MethodDefinition MethodNameConstructor = null;
            public AssemblyDefinition BaseModLib = null;
            public PrivateContext(Context context)
            {
                ProgressHandler = context.ProgressHandler;
                AssemblyFiles = context.AssemblyFiles;
                SaveTo = context.SaveTo;
                AssemblyResolverPath = context.AssemblyResolverPath;
            }

            public void LoadBaseModLib()
            {
                BaseModLib = AssemblyDefinition.ReadAssembly(Path.GetFullPath(Configuration.DataDirectory + "/BaseModLib.dll"), new ReaderParameters() { ReadWrite = true });
                var type = BaseModLib.MainModule.GetType("ModAPI.FormerField");
                if (type == null)
                    throw new Exception("BaseModLib.dll is corrupted. Reinstall ModAPI?");
                foreach (var m in type.Methods)
                {
                    if (m.IsConstructor)
                    {
                        FormerFieldConstructor = m;
                        break;
                    }
                }
                if (FormerFieldConstructor == null)
                    throw new Exception("BaseModLib.dll is corrupted. Reinstall ModAPI?");

                type = BaseModLib.MainModule.GetType("ModAPI.PropertyName");
                if (type == null)
                    throw new Exception("BaseModLib.dll is corrupted. Reinstall ModAPI?");
                foreach (var m in type.Methods)
                {
                    if (m.IsConstructor)
                    {
                        PropertyNameConstructor = m;
                        break;
                    }
                }
                if (PropertyNameConstructor == null)
                    throw new Exception("BaseModLib.dll is corrupted. Reinstall ModAPI?");

                type = BaseModLib.MainModule.GetType("ModAPI.MethodName");
                if (type == null)
                    throw new Exception("BaseModLib.dll is corrupted. Reinstall ModAPI?");
                foreach (var m in type.Methods)
                {
                    if (m.IsConstructor)
                    {
                        MethodNameConstructor = m;
                        break;
                    }
                }
                if (MethodNameConstructor == null)
                    throw new Exception("BaseModLib.dll is corrupted. Reinstall ModAPI?");
            }
        }

        public static void Execute(Context ctxt)
        {
            var context = new PrivateContext(ctxt);
            try
            {
                var resolver = new DefaultAssemblyResolver();
                resolver.AddSearchDirectory(Path.GetFullPath(context.AssemblyResolverPath));

                if (!Directory.Exists(context.SaveTo))
                    Directory.CreateDirectory(context.SaveTo);
                
                Logger.Debug("Extracting BaseModLib.dll...");
                Embedded.Extract("ModAPI.BaseModLib.dll", Path.GetFullPath(Configuration.DataDirectory + "/BaseModLib.dll"));

                context.LoadBaseModLib();

                Logger.Debug("Loading assemblies...");
                var readerParameters = new ReaderParameters() { AssemblyResolver = resolver };
                foreach (var library in context.AssemblyFiles)
                {
                    if (library.IsSystem)
                    {
                        var definition = AssemblyDefinition.ReadAssembly(library.File, readerParameters);
                        Logger.Debug("Loaded assembly \"" + library.File + "\"!");
                        context.SystemAssemblies.Add(definition.Name.Name, definition);
                        context.Checksums.Add(definition.Name.Name, library.GetOriginalChecksum());
                    }
                    else
                    {
                        var definition = AssemblyDefinition.ReadAssembly(library.File, readerParameters);
                        Logger.Debug("Loaded assembly \"" + library.File + "\"!");
                        context.Assemblies.Add(definition.Name.Name, definition);
                        context.Checksums.Add(definition.Name.Name, library.GetOriginalChecksum());
                    }
                }
                if (context.SystemAssemblies.ContainsKey("mscorlib"))
                {
                    context.BaseModLib.ReplaceAssemblyReference(new AssemblyNameReference("netstandard", new System.Version("2.0.0.0")), context.SystemAssemblies["mscorlib"]);
                    var nonserialized = context.SystemAssemblies["mscorlib"].MainModule.GetType("System.NonSerializedAttribute");
                    foreach (var m in nonserialized.Methods)
                    {
                        if (m.IsConstructor)
                        {
                            context.NonSerializedConstructor = m;
                            break;
                        }
                    }
                }
                if (context.NonSerializedConstructor == null)
                    throw new InvalidOperationException("mscorlib.dll is needed for creation of the mod library.");
                context.BaseModLib.Write(Path.Combine(context.SaveTo, "BaseModLib.dll"));

                // first pass
                // includes making every every method public virtual and changing fields to properties as public virtual if possible.
                // if not possible fields will just be changed to public instead.
                var total = 0f;
                var done = 0f;
                context.ProgressHandler.ChangeProgress("First pass: Analyzing fields...", 0f);
                Logger.Debug("First pass: Analyzing fields...");

                foreach (var assembly in context.Assemblies)
                    foreach (var type in assembly.Value.MainModule.Types)
                        total++;
                foreach (var assembly in context.SystemAssemblies)
                    foreach (var type in assembly.Value.MainModule.Types)
                        total++;

                foreach (var assembly in context.Assemblies)
                {
                    foreach (var type in assembly.Value.MainModule.Types)
                    {
                        FetchTypes(context, type);
                        FirstPassType(context, type);
                        done++;
                        context.ProgressHandler.ChangeProgress((.2f * done / total));
                    }
                }
                foreach (var assembly in context.SystemAssemblies)
                {
                    foreach (var type in assembly.Value.MainModule.Types)
                    {
                        FetchTypes(context, type);
                        done++;
                        context.ProgressHandler.ChangeProgress((.2f * done / total));
                    }
                }

                total = 0f;
                done = 0f;
                context.ProgressHandler.ChangeProgress("Second pass: Changing fields to properties and opening up types...", .2f);
                Logger.Debug("Second pass: Changing fields to properties and opening up types...");
                foreach (var assembly in context.Assemblies)
                {
                    foreach (var type in assembly.Value.MainModule.Types)
                        total++;
                }

                foreach (var assembly in context.Assemblies)
                {
                    foreach (var type in assembly.Value.MainModule.Types)
                    {
                        SecondPassType(context, type);
                        done++;
                        context.ProgressHandler.ChangeProgress(.2f + (.6f * done / total));
                    }
                }

                total = 0f;
                done = 0f;
                context.ProgressHandler.ChangeProgress("Third pass: Removing method bodies and adding delegates...", .8f);
                Logger.Debug("Third pass: Removing method bodies and adding delegates...");
                foreach (var assembly in context.Assemblies)
                {
                    foreach (var type in assembly.Value.MainModule.Types)
                        total++;
                }

                foreach (var assembly in context.Assemblies)
                {
                    foreach (var type in assembly.Value.MainModule.Types)
                    {
                        ThirdPassType(context, type);
                        done++;
                        context.ProgressHandler.ChangeProgress(.8f + (.15f * done / total));
                    }
                }

                context.ProgressHandler.ChangeProgress("Saving assemblies...", 0.95f);

                total = context.Assemblies.Count + context.SystemAssemblies.Count;
                done = 0f;
                foreach (var assembly in context.Assemblies)
                {
                    var modInformation = new ModInformation();
                    modInformation.OriginalChecksum = context.Checksums[assembly.Value.Name.Name];
                    assembly.Value.MainModule.Resources.Add(new EmbeddedResource("ModInformation", ManifestResourceAttributes.Public, System.Text.Encoding.UTF8.GetBytes(modInformation.ToJSON().ToString())));
                    assembly.Value.MainModule.AssemblyReferences.Add(new AssemblyNameReference("BaseModLib", Data.ModAPI.BaseModLib.Name.Version));
                    assembly.Value.Write(Path.Combine(Path.GetFullPath(context.SaveTo), assembly.Value.Name.Name + ".dll"));
                    done++;
                    context.ProgressHandler.ChangeProgress(.95f + (.05f * done / (total)));
                }

                foreach (var assembly in context.SystemAssemblies)
                {
                    var modInformation = new ModInformation();
                    modInformation.OriginalChecksum = context.Checksums[assembly.Value.Name.Name];
                    assembly.Value.MainModule.Resources.Add(new EmbeddedResource("ModInformation", ManifestResourceAttributes.Public, System.Text.Encoding.UTF8.GetBytes(modInformation.ToJSON().ToString())));
                    assembly.Value.MainModule.AssemblyReferences.Add(new AssemblyNameReference("BaseModLib", Data.ModAPI.BaseModLib.Name.Version));
                    assembly.Value.Write(Path.Combine(Path.GetFullPath(context.SaveTo), assembly.Value.Name.Name + ".dll"));
                    done++;
                    context.ProgressHandler.ChangeProgress(.95f + (.05f * done / (total)));
                }
                //var fileName = Path.GetFileName(library.File);
                //System.IO.File.Copy(library.File, Path.Combine(context.SaveTo, fileName), true);

                foreach (var assembly in context.Assemblies)
                    assembly.Value.Dispose();
                foreach (var assembly in context.SystemAssemblies)
                    assembly.Value.Dispose();

                context.ProgressHandler.Finish();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while creating mod library");
                context.ProgressHandler.Error(e.Message);
            }
        }

        private static void FetchTypes(PrivateContext context, TypeDefinition type)
        {
            if (!context.AllTypes.ContainsKey(type.FullName))
                context.AllTypes.Add(type.FullName, type);
            foreach (var nested in type.NestedTypes)
                FetchTypes(context, nested);
        }

        private static void FirstPassType(PrivateContext context, TypeDefinition type)
        {
            foreach (var nested in type.NestedTypes)
                FirstPassType(context, nested);

            var isCompilerGenerated = false;
            foreach (var attr in type.CustomAttributes)
                if (attr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute")
                    isCompilerGenerated = true;

            if (!isCompilerGenerated && !type.IsRuntimeSpecialName && !type.IsSpecialName)
            {
                Logger.Debug("Analyzing \"" + type.FullName + "\"");

                foreach (var field in type.Fields)
                {
                    var identifier = field.GetIdentifier();
                    try
                    {
                        var genericParameter = field.FieldType.GetGenericParameter();
                        if (genericParameter != null)
                        {
                            if (genericParameter.IsValueType)
                            {
                                Logger.Info("Field \"" + identifier + "\" is not changeable as it is holding a generic parameter which is of a valuetype.");
                                context.UnchangeableFields.Add(identifier);
                            }
                        }
                        else
                        {
                            var fieldType = field.FieldType.Resolve();
                            if (!fieldType.IsEnum && !fieldType.IsPrimitive && field.FieldType.IsValueType)
                            {
                                if (!context.UnchangeableFields.Contains(identifier))
                                {
                                    Logger.Info("Field \"" + identifier + "\" is not changeable as it is holding a struct of type \"" + field.FieldType.FullName + "\".");
                                    context.UnchangeableFields.Add(identifier);
                                }
                            }
                        }
                    }
                    catch (AssemblyResolutionException e)
                    {
                        Logger.Info("Field \"" + identifier + "\" is not changeable as the type \"" + field.FieldType.FullName + "\" was not resolveable.");
                    }
                }

                foreach (var method in type.Methods)
                {
                    var body = method.Body;
                    if (body != null)
                    {
                        for (var i = 0; i < body.Instructions.Count; i++)
                        {
                            var instruction = body.Instructions[i];
                            if (instruction.OpCode == OpCodes.Ldflda)
                            {
                                var fieldReference = (FieldReference)instruction.Operand;
                                var identifier = fieldReference.GetIdentifier();
                                if (!context.UnchangeableFields.Contains(identifier))
                                {
                                    Logger.Info("Field \"" + identifier + "\" is not changeable to a property as it is accessed by address in code.");
                                    context.UnchangeableFields.Add(identifier);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void SecondPassType(PrivateContext context, TypeDefinition type)
        {
            foreach (var nested in type.NestedTypes)
                SecondPassType(context, nested);
            var isCompilerGenerated = false;
            foreach (var attr in type.CustomAttributes)
                if (attr.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute")
                    isCompilerGenerated = true;
            if (!isCompilerGenerated && !type.IsRuntimeSpecialName && !type.IsSpecialName) // coroutines are off limit for now
            {
                Logger.Debug("Opening up \"" + type.FullName + "\"");
                /*if (type.IsValueType)
                {
                    type.BaseType = null;
                    type.IsClass = true;
                }*/

                if (!type.IsPublic)
                {
                    type.IsPublic = true;
                }

                foreach (var method in type.Methods)
                {
                    if (!method.IsSpecialName && !method.IsConstructor)
                    {
                        if (!method.IsPublic)
                        {
                            method.IsPublic = true;
                        }
                        if (!method.IsStatic && !method.IsVirtual)
                        {
                            method.IsVirtual = true;
                            method.IsNewSlot = true;
                        }
                    }
                }

                foreach (var property in type.Properties)
                {
                    if (property.GetMethod != null)
                    {
                        var getMethod = property.GetMethod;
                        if (!getMethod.IsPublic)
                            getMethod.IsPublic = true;
                        if (!getMethod.IsStatic && !getMethod.IsVirtual)
                        {
                            getMethod.IsVirtual = true;
                            getMethod.IsNewSlot = true;
                        }
                    }
                    else
                    {
                        var getMethod = new MethodDefinition("get_" + property.Name, MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, type);
                        getMethod.SemanticsAttributes = MethodSemanticsAttributes.Getter;
                        getMethod.ReturnType = property.PropertyType;
                        
                        var propertyNameAttribute = new CustomAttribute(property.Module.ImportReference(context.PropertyNameConstructor));
                        propertyNameAttribute.ConstructorArguments.Add(new CustomAttributeArgument(property.Module.TypeSystem.String, property.FullName));
                        getMethod.CustomAttributes.Add(propertyNameAttribute);
                        
                        property.GetMethod = getMethod;
                        type.Methods.Add(getMethod);
                    }
                    if (property.SetMethod != null)
                    {
                        var setMethod = property.SetMethod;
                        if (!setMethod.IsPublic)
                            setMethod.IsPublic = true;
                        if (!setMethod.IsStatic && !setMethod.IsVirtual)
                        {
                            setMethod.IsVirtual = true;
                            setMethod.IsNewSlot = true;
                        }
                    }
                    else
                    {
                        var setMethod = new MethodDefinition("set_" + property.Name, MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, type);
                        setMethod.SemanticsAttributes = MethodSemanticsAttributes.Setter;
                        setMethod.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, property.PropertyType));
                        setMethod.ReturnType = type.Module.TypeSystem.Void;
                        
                        var propertyNameAttribute = new CustomAttribute(property.Module.ImportReference(context.PropertyNameConstructor));
                        propertyNameAttribute.ConstructorArguments.Add(new CustomAttributeArgument(property.Module.TypeSystem.String, property.FullName));
                        setMethod.CustomAttributes.Add(propertyNameAttribute);
                        
                        property.SetMethod = setMethod;
                        type.Methods.Add(setMethod);
                    }
                }

                if (!type.IsValueType)
                { 
                    var removeFields = new List<FieldDefinition>();
                    foreach (var field in type.Fields)
                    {
                        var fullFieldName = field.FullName;
                        var fieldName = field.Name;
                        var identifier = field.GetIdentifier();
                        if (!context.UnchangeableFields.Contains(identifier) && !field.IsCompilerControlled)
                        {
                            removeFields.Add(field);

                            Logger.Debug("Found field \"" + field.Name + "\". Changing it to a property...");

                            var newProperty = new PropertyDefinition(fieldName, PropertyAttributes.None, field.FieldType);
                            newProperty.HasThis = !field.IsStatic;

                            var getMethod = new MethodDefinition("get_" + fieldName, MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, type);
                            getMethod.SemanticsAttributes = MethodSemanticsAttributes.Getter;
                            var setMethod = new MethodDefinition("set_" + fieldName, MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, type);
                            setMethod.SemanticsAttributes = MethodSemanticsAttributes.Setter;

                            getMethod.ReturnType = field.FieldType;
                            setMethod.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, field.FieldType));
                            setMethod.ReturnType = type.Module.TypeSystem.Void;

                            var formerFieldAttribute0 = new CustomAttribute(field.Module.ImportReference(context.FormerFieldConstructor));
                            formerFieldAttribute0.ConstructorArguments.Add(new CustomAttributeArgument(field.Module.TypeSystem.String, field.FullName));
                            newProperty.CustomAttributes.Add(formerFieldAttribute0);
                            /*
                            var formerFieldAttribute1 = new CustomAttribute(field.Module.ImportReference(context.FormerFieldConstructor));
                            formerFieldAttribute1.ConstructorArguments.Add(new CustomAttributeArgument(field.Module.TypeSystem.String, field.FullName));
                            setMethod.CustomAttributes.Add(formerFieldAttribute1);
                            */
                            type.Methods.Add(getMethod);
                            type.Methods.Add(setMethod);
                            
                            newProperty.SetMethod = setMethod;
                            newProperty.GetMethod = getMethod;

                            getMethod.Body.Optimize();
                            setMethod.Body.Optimize();

                            type.Properties.Add(newProperty);

                            context.NewProperties.Add(newProperty.GetIdentifier(), newProperty);
                            context.NewPropertyNames.Add(newProperty.GetIdentifier());
                            context.FieldToProperty.Add(field.GetIdentifier(), newProperty);

                            Logger.Debug("Property \"" + newProperty.Name + "\" created with getter and setter!");

                        }
                        else
                        {
                            field.IsPublic = true;
                        }
                    }

                    foreach (var f in removeFields)
                    {
                        type.Fields.Remove(f);
                    }
                }
            }
        }

        private static void ThirdPassType(PrivateContext context, TypeDefinition type)
        {
            foreach (var nested in type.NestedTypes)
                ThirdPassType(context, nested);

            HashSet<string> delegateNames = new HashSet<string>();
            foreach (var method in type.Methods)
            {
                if (!method.IsConstructor)
                {
                    var methodName = method.Name;
                    var delegateName = "__ModAPI_Delegate_" + methodName;
                    int i = 0;
                    while (delegateNames.Contains(delegateName))
                    {
                        delegateName = "__ModAPI_Delegate_" + methodName + i;
                        i++;
                    }
                    delegateNames.Add(delegateName);
                    var @params = method.Parameters.ToList();
                    if (!method.IsStatic)
                        @params.Insert(0, new ParameterDefinition("self", ParameterAttributes.None, method.DeclaringType));
                    var @delegate = MonoHelper.CreateDelegate(type, method.ReturnType, delegateName, true, @params, context.AllTypes["System.MulticastDelegate"], context.AllTypes["System.AsyncCallback"], context.AllTypes["System.IAsyncResult"]);
                    var delegateType = @delegate.Type;

                    var methodNameAttribute = new CustomAttribute(type.Module.ImportReference(context.MethodNameConstructor));
                    methodNameAttribute.ConstructorArguments.Add(new CustomAttributeArgument(method.Module.TypeSystem.String, method.FullName));
                    delegateType.CustomAttributes.Add(methodNameAttribute);
                }

                if (method.Body != null)
                {
                    Logger.Debug("Removing method body of method \"" + method.FullName + "\"");
                    method.Body.Instructions.Clear();
                }
            }
        }

        private static void ReplaceFieldToProperty(PrivateContext context, MethodDefinition method)
        {
            var body = method.Body;
            if (body != null)
            {
                body.SimplifyMacros();

                var processor = body.GetILProcessor();

                for (var i = 0; i < body.Instructions.Count; i++)
                {
                    var instruction = body.Instructions[i];
                    if (instruction.OpCode == OpCodes.Ldfld)
                    {
                        var field = (FieldReference)instruction.Operand;
                        var identifier = field.GetIdentifier();
                        if (context.FieldToProperty.ContainsKey(identifier))
                        {
                            var property = context.FieldToProperty[identifier];
                            var newInstruction = processor.Create(OpCodes.Callvirt, method.Module.ImportReference(property.GetMethod));
                            body.RedirectJumps(instruction, newInstruction);
                            processor.Replace(instruction, newInstruction);
                            Logger.Info("Ldfld replaced with callvirt to getter of new property in \"" + method.FullName + "\"");
                        }
                    }
                    if (instruction.OpCode == OpCodes.Stfld)
                    {
                        var field = (FieldReference)instruction.Operand;
                        var identifier = field.GetIdentifier();
                        if (context.FieldToProperty.ContainsKey(identifier))
                        {
                            var property = context.FieldToProperty[identifier];
                            var reference = method.Module.ImportReference(property.SetMethod);
                            var newInstruction = processor.Create(OpCodes.Callvirt, reference);
                            body.RedirectJumps(instruction, newInstruction);
                            processor.Replace(instruction, newInstruction);
                            Logger.Info("Stfld replaced with callvirt to setter of new property in \"" + method.FullName + "\"");
                        }
                    }
                }

                body.Optimize();
            }
        }
        /*
        private static void ThirdPassType(PrivateContext context, TypeDefinition type)
        {
            foreach (var nested in type.NestedTypes)
                ThirdPassType(context, nested);

            if (!type.IsRuntimeSpecialName && !type.IsSpecialName) // coroutines are off limit for now
            {
                Logger.Debug("Opening up \"" + type.FullName + "\"");

                var already = new HashSet<MethodDefinition>();
                foreach (var property in type.Properties)
                {
                    if (property.GetMethod != null)
                        already.Add(property.GetMethod);
                    if (property.SetMethod != null)
                        already.Add(property.SetMethod);
                    if (!context.NewProperties.ContainsKey(property.GetIdentifier()))
                    {
                        if (property.GetMethod != null)
                        {
                            ReplaceFieldToProperty(context, property.GetMethod);
                        }
                        if (property.SetMethod != null)
                        {
                            ReplaceFieldToProperty(context, property.SetMethod);
                        }
                    }
                }

                foreach (var method in type.Methods)
                {
                    if (!already.Contains(method))
                        ReplaceFieldToProperty(context, method);
                }
            }
        }*/
    }
}
