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

        public class Input
        {
            public ModProject Project;
            public ProgressHandler ProgressHandler;
        }

        public static void Execute(Input ctxt)
        {
            var context = new Context(ctxt);
            try
            {
                context.LoadModLibrary();
                context.CreateMod();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while creating mod.");
                context.ProgressHandler.Error(ex.Message);
            }
        }


        private static void ExtendRoutesToBaseCall(Context context, TypeDefinition type, List<CallStack.Node> routes, MethodDefinition method, ParameterDefinition chainParam, ParameterDefinition numParam, MonoHelper.Delegate @delegate, int highestDisplayClass)
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

        private static void __ExtendRouteToBaseCall(Context context, TypeDefinition type, CallStack.Node node, CallStackCopyContext routeContext, CallStackCopyScopeContext scope)
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
            // method called is in a display class or is a compiler generated method
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
                            var objectType = context.AllTypes["System.Object"];
                            var compilerGeneratedAttributeConstructor = context.AllTypes["System.Runtime.CompilerServices.CompilerGeneratedAttribute"].Methods.First(m => m.IsConstructor);

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
    }
}
