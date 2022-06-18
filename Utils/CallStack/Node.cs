using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ModAPI.Utils
{
    internal partial class CallStack
    {
        internal class Node
        {
            public Node Parent;
            public List<Node> Children = new();
            public MethodDefinition CalledMethod;
            public MethodDefinition Method;
            public Instruction Instruction;
            public NodeType Type;
            public enum NodeType
            {
                Method,
                Call
            }

            public void Clean()
            {
                for (var i = 0; i < Children.Count; i++)
                {
                    if (!Children[i].ContainsCall())
                        Children.RemoveAt(i);
                    else Children[i].Clean();
                }
            }

            private List<Node> Reroute(MethodDefinition newMethod, Instruction[] instructions)
            {
                var children = this.Children.ToList();


                for (var j = 0; j < children.Count; j++)
                {
                    for (var i = 0; i < CalledMethod.Body.Instructions.Count; i++)
                    {
                        if (children[j].Instruction == CalledMethod.Body.Instructions[i])
                        {
                            children[j] = new Node()
                            {
                                CalledMethod = children[j].CalledMethod,
                                Method = newMethod,
                                Instruction = instructions[i],
                                Children = children[j].Children,
                                Parent = this,
                                Type = children[j].Type,
                            };
                            continue;
                        }
                    }
                }
                return children;
            }
            public bool ContainsCall()
            {
                if (Type == NodeType.Call)
                    return true;
                for (var i = 0; i < Children.Count; i++)
                {
                    if (Children[i].ContainsCall())
                        return true;
                }
                return false;
            }

            public void Extend(CallStackCopyContext context, CallStackCopyScope scope)
            {
                if (Type == CallStack.Node.NodeType.Call)
                {
                    AddParametersToCall(context, scope);
                }
                // method called is in a display class or is a compiler generated method
                else if (CalledMethod.Name.StartsWith("<") && !CalledMethod.Name.StartsWith("<>n__"))
                {
                    if (CalledMethod.DeclaringType.FullName == context.Type.FullName) // method
                    {
                        if (scope.Type == CallStackCopyScope.TypeEnum.METHOD)
                            ExtendDisplayMethodFromMethod(context, scope);
                        else if (scope.Type == CallStackCopyScope.TypeEnum.DISPLAY_CLASS)
                            ExtendDisplayMethodFromDisplayClass(context, scope);
                    }
                    else if (CalledMethod.DeclaringType.Name.StartsWith("<>c__DisplayClass")) // display class
                    {
                        ExtendDisplayClass(context, scope);
                    }
                    else
                    {
                        // something weird happened (e.g. method of nested type <>c was called even tho I deemed it impossible as of lacking reference to this)
                    }
                }
                else
                {
                    ExtendMethod(context, scope);
                }
            }

            private void ExtendDisplayMethodFromMethod(CallStackCopyContext context, CallStackCopyScope scope)
            {
                (MethodDefinition, Instruction[]) newMethod = (null, null);
                DisplayClass displayClass = null;
                if (context.MethodMappings.ContainsKey(Method.FullName))
                {
                    newMethod = context.Methods[context.MethodMappings[Method.FullName]];
                    displayClass = context.DisplayClasses[newMethod.Item1.DeclaringType.FullName];
                }
                else
                {
                    // create a new display class
                    var objectType = context.ModLibrary.AllTypes["System.Object"];
                    var compilerGeneratedAttributeConstructor = context.ModLibrary.AllTypes["System.Runtime.CompilerServices.CompilerGeneratedAttribute"].Methods.First(m => m.IsConstructor);

                    context.HighestDisplayClassNum++;
                    context.HighestDisplayClassSub = 0;
                    var newName = "<>c__DisplayClass" + context.HighestDisplayClassNum + "_" + context.HighestDisplayClassSub;
                    var newClass = new TypeDefinition(context.Type.Namespace, newName, TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Sealed | TypeAttributes.NestedPrivate, context.Module.TypeSystem.Object);
                    newClass.CustomAttributes.Add(new CustomAttribute(context.Module.ImportReference(compilerGeneratedAttributeConstructor), new byte[] { 1, 0, 0, 0 }));

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
                    var addedFields = new Dictionary<string, FieldDefinition>();
                    var newFields = new Dictionary<string, FieldDefinition>();
                    var thisField = new FieldDefinition("self", FieldAttributes.Public, context.Module.ImportReference(context.Type));
                    newClass.Fields.Add(thisField);
                    newFields.Add("self", thisField);

                    foreach (var param in context.AddParameters)
                    {
                        var newField = new FieldDefinition(param.Key, FieldAttributes.Public, context.Module.ImportReference(param.Value));
                        newClass.Fields.Add(newField);
                        newFields.Add(param.Key, newField);
                        addedFields.Add(param.Key, newField);
                    }

                    var newMethods = new Dictionary<string, MethodDefinition>();

                    // create constructor
                    MethodDefinition constructor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, context.Module.TypeSystem.Void);
                    var constructorProcessor = constructor.Body.GetILProcessor();
                    constructorProcessor.Append(constructorProcessor.Create(OpCodes.Ldarg_0));
                    constructorProcessor.Append(constructorProcessor.Create(OpCodes.Call, context.Module.ImportReference(objectConstructor)));
                    constructorProcessor.Append(constructorProcessor.Create(OpCodes.Nop));
                    constructorProcessor.Append(constructorProcessor.Create(OpCodes.Ret));
                    newClass.Methods.Add(constructor);

                    context.Type.NestedTypes.Add(newClass);

                    displayClass = new DisplayClass()
                    {
                        Constructor = constructor,
                        AddedFields = addedFields,
                        Fields = newFields,
                        ThisField = thisField,
                        Methods = newMethods,
                        Type = newClass
                    };

                    var match = Regex.Match(CalledMethod.Name, @"\<([^\>]+)\>b__([0-9]+)_([0-9]+)");
                    var n = match.Groups[1].Value;

                    context.HighestDisplayClassSub++;
                    var newMethodName = "<" + n + ">b__" + context.HighestDisplayClassSub;
                    displayClass.HighestSub = context.HighestDisplayClassSub;

                    // copy method
                    var method = new MethodDefinition(newMethodName, CalledMethod.Attributes, CalledMethod.ReturnType);
                    method.IsPublic = true;
                    for (var i = 0; i < CalledMethod.Parameters.Count; i++)
                        method.Parameters.Add(new ParameterDefinition(CalledMethod.Parameters[i].Name, CalledMethod.Parameters[i].Attributes, CalledMethod.Parameters[i].ParameterType));
                    var newBody = method.Body;
                    CalledMethod.Body.Copy(newBody, context.Module);
                    displayClass.Type.Methods.Add(method);
                    newMethod = AddCopiedMethod(context, CalledMethod, method);

                    // modify method
                    var newMethodProcessor = newBody.GetILProcessor();
                    for (var i = 0; i < newBody.Instructions.Count; i++)
                    {
                        if (newBody.Instructions[i].OpCode == OpCodes.Ldarg_0)
                        {
                            // this will also be applied to ldftn statements. This has to be removed later on for ldftn statements in the route
                            newMethodProcessor.InsertAfter(newBody.Instructions[i], newMethodProcessor.Create(OpCodes.Ldfld, displayClass.ThisField));
                            i++;
                        }
                    }
                }

                //re-route childs
                var children = Reroute(newMethod.Item1, newMethod.Item2);

                var newScope = new CallStackCopyScope()
                {
                    Type = CallStackCopyScope.TypeEnum.DISPLAY_CLASS,
                    DisplayClass = displayClass
                };

                foreach (var child in children)
                {
                    // we keep being in the same display class for now
                    child.Method.Body.SimplifyMacros();
                    child.Extend(context, newScope);
                    child.Method.Body.Optimize();
                }

                var body = Method.Body;
                var displayClassVar = new VariableDefinition(context.Module.ImportReference(displayClass.Type));
                body.Variables.Insert(0, displayClassVar);
                var processor = body.GetILProcessor();

                var first = body.Instructions[0];
                processor.InsertBefore(first, processor.Create(OpCodes.Newobj, context.Module.ImportReference(displayClass.Constructor)));
                processor.InsertBefore(first, processor.Create(OpCodes.Stloc, displayClassVar));
                processor.InsertBefore(first, processor.Create(OpCodes.Ldloc, displayClassVar));
                processor.InsertBefore(first, processor.Create(OpCodes.Ldarg_0));
                processor.InsertBefore(first, processor.Create(OpCodes.Stfld, context.Module.ImportReference(displayClass.ThisField)));

                foreach (var param in context.AddParameters)
                {
                    processor.InsertBefore(first, processor.Create(OpCodes.Ldloc, displayClassVar));
                    processor.InsertBefore(first, processor.Create(OpCodes.Ldarg, scope.Method.AddedParameters[param.Key]));
                    processor.InsertBefore(first, processor.Create(OpCodes.Stfld, context.Module.ImportReference(displayClass.AddedFields[param.Key])));
                }

                var inst = Instruction;
                inst.Previous.OpCode = OpCodes.Ldloc;
                inst.Previous.Operand = displayClassVar;
                inst.Operand = context.Module.ImportReference(newMethod.Item1);
            }

            private void ExtendDisplayMethodFromDisplayClass(CallStackCopyContext context, CallStackCopyScope scope)
            {
                (MethodDefinition, Instruction[]) newMethod = (null, null);
                if (context.MethodMappings.ContainsKey(CalledMethod.FullName))
                {
                    // it may be necessary to check for the actual type of the method (as the compiler sometimes likes to
                    // create some methods like n__0 for duplicated code and stuff.
                    newMethod = context.Methods[context.MethodMappings[CalledMethod.FullName]];
                }
                else
                {
                    var match = Regex.Match(CalledMethod.Name, @"\<([^\>]+)\>b__([0-9]+)_([0-9]+)");
                    var n = match.Groups[1].Value;
                    scope.DisplayClass.HighestSub++;

                    // copy method
                    var newMethodName = "<" + n + ">b__" + scope.DisplayClass.HighestSub;
                    var method = new MethodDefinition(newMethodName, CalledMethod.Attributes, CalledMethod.ReturnType);
                    method.IsPublic = true;
                    var newBody = method.Body;
                    CalledMethod.Body.Copy(newBody);
                    scope.DisplayClass.Type.Methods.Add(method);
                    newMethod = AddCopiedMethod(context, CalledMethod, method);

                    // modify method
                    var newMethodProcessor = newBody.GetILProcessor();
                    for (var i = 0; i < newBody.Instructions.Count; i++)
                    {
                        if (newBody.Instructions[i].OpCode == OpCodes.Ldarg_0)
                        {
                            // this will also be applied to ldftn statements. This has to be removed later on for ldftn statements in the route
                            newMethodProcessor.InsertAfter(newBody.Instructions[i], newMethodProcessor.Create(OpCodes.Ldfld, scope.DisplayClass.ThisField));
                            i++;
                        }
                    }
                }

                var children = Reroute(newMethod.Item1, newMethod.Item2);

                foreach (var child in children)
                {
                    // we keep being in the same scope for now
                    child.Method.Body.SimplifyMacros();
                    child.Extend(context, scope);
                    child.Method.Body.Optimize();

                }

                var body = Method.Body;
                var processor = body.GetILProcessor();

                var first = processor.WalkBack(Instruction);
                // remove ldfld self
                if (first.Next.OpCode == OpCodes.Ldfld)
                    processor.Remove(first.Next);
                Instruction.Operand = context.Module.ImportReference(newMethod.Item1);
            }

            private void ExtendDisplayClass(CallStackCopyContext context, CallStackCopyScope scope)
            {
                var displayClass = new DisplayClass(CalledMethod.DeclaringType);
                DisplayClass newDisplayClass = null;
                if (context.ClassMappings.ContainsKey(CalledMethod.DeclaringType.Name))
                {
                    newDisplayClass = context.DisplayClasses[context.ClassMappings[CalledMethod.DeclaringType.Name]];
                }
                else
                {
                    if (scope.Type == CallStackCopyScope.TypeEnum.METHOD)
                    {
                        context.HighestDisplayClassNum++;
                        context.HighestDisplayClassSub = 0;
                    }
                    else
                    {
                        context.HighestDisplayClassNum++;
                    }

                    newDisplayClass = displayClass.Copy(context);
                    context.ClassMappings.Add(displayClass.Type.FullName, newDisplayClass.Type.FullName);
                    context.DisplayClasses.Add(newDisplayClass.Type.FullName, newDisplayClass);
                }

                bool sameDisplayClass = scope.Type == CallStackCopyScope.TypeEnum.DISPLAY_CLASS && newDisplayClass.Type.FullName == scope.DisplayClass.Type.FullName;
                //var newClasses = CopyDisplayClasses(type, classTypes[group], baseMethod, @delegate, ref highestDisplayClass);

                var method = Method;

                if (!sameDisplayClass)
                {
                    var body = method.Body;
                    var processor = Method.Body.GetILProcessor();
                    Instruction.Operand = context.Module.ImportReference(newDisplayClass.Methods[CalledMethod.Name]);

                    // add params
                    var previous = Instruction.Previous;
                    var variable = (VariableDefinition)previous.Operand;
                    variable.VariableType = context.Module.ImportReference(newDisplayClass.Type);
                    // find new obj
                    var inst = Instruction;

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
                        if (scope.Type == CallStackCopyScope.TypeEnum.METHOD)
                        {
                            bool alreadyChanged = false;
                            if (inst.OpCode == OpCodes.Ldloc && inst.Operand is VariableDefinition v && v == variable &&
                                inst.Next != null && inst.Next.OpCode == OpCodes.Ldarg && inst.Next.Operand is ParameterDefinition p1 && p1 == scope.Method.AddedParameters.First().Value)
                                alreadyChanged = true;

                            if (!alreadyChanged)
                            {
                                foreach (var param in context.AddParameters)
                                {
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Ldloc, variable));
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Ldarg, scope.Method.AddedParameters[param.Key]));
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Stfld, newDisplayClass.AddedFields[param.Key]));
                                }
                            }
                        }
                        else if (scope.Type == CallStackCopyScope.TypeEnum.DISPLAY_CLASS && context.HighestDisplayClassSub == 0) // will most likely never be true?
                        {
                            bool alreadyChanged = false;
                            if (inst.OpCode == OpCodes.Ldloc && inst.Operand is VariableDefinition v && v == variable &&
                                inst.Next != null && inst.Next.OpCode == OpCodes.Ldarg_0 &&
                                inst.Next.Next != null && inst.Next.Next.OpCode == OpCodes.Ldfld && inst.Next.Next.Operand is FieldDefinition f1 && f1.FullName == scope.DisplayClass.AddedFields.First().Value.FullName)
                                alreadyChanged = true;

                            if (!alreadyChanged)
                            {
                                foreach (var param in context.AddParameters)
                                {
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Ldloc, variable));
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Ldarg_0));
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Ldarg, scope.DisplayClass.AddedFields[param.Key]));
                                    processor.InsertBefore(inst, processor.Create(OpCodes.Stfld, newDisplayClass.AddedFields[param.Key]));
                                }
                            }
                        }
                    }
                }

                var newScope = new CallStackCopyScope()
                {
                    Type = CallStackCopyScope.TypeEnum.DISPLAY_CLASS,
                    DisplayClass = newDisplayClass,
                    OriginalName = displayClass.Type.FullName
                };

                foreach (var child in Children)
                {
                    // we keep being in the same display class for now
                    child.Method.Body.SimplifyMacros();
                    child.Extend(context, newScope);
                    child.Method.Body.Optimize();
                }
            }

            private void ExtendMethod(CallStackCopyContext context, CallStackCopyScope scope)
            {
                var newMethod = CalledMethod.Copy();

                //re-route childs
                Reroute(newMethod, newMethod.Body.Instructions.ToArray());

                var addedParams = new Dictionary<string, ParameterDefinition>();
                foreach (var param in context.AddParameters)
                {
                    var newParam = new ParameterDefinition(param.Key, ParameterAttributes.None, context.Module.ImportReference(param.Value));
                    newMethod.Parameters.Add(newParam);
                    addedParams.Add(param.Key, newParam);
                }
                newMethod.Name += "_" + context.HighestDisplayClassNum + "_" + context.HighestDisplayClassSub;
                CalledMethod.DeclaringType.Methods.Add(newMethod);

                var body = Method.Body;
                var processor = body.GetILProcessor();
                if (scope.Type == CallStackCopyScope.TypeEnum.DISPLAY_CLASS)
                {
                    foreach (var param in context.AddParameters)
                    {
                        processor.InsertBefore(Instruction, processor.Create(OpCodes.Ldarg_0));
                        processor.InsertBefore(Instruction, processor.Create(OpCodes.Ldfld, scope.DisplayClass.AddedFields[param.Key]));
                    }
                }
                Instruction.Operand = context.Module.ImportReference(newMethod);

                var newScope = new CallStackCopyScope()
                {
                    Type = CallStackCopyScope.TypeEnum.METHOD,
                    Method = new Utils.CallStack.Method()
                    {
                        MethodDefinition = newMethod,
                        AddedParameters = addedParams
                    }
                };

                foreach (var child in Children)
                {
                    // we keep being in the same display class for now
                    child.Method.Body.SimplifyMacros();
                    child.Extend(context, newScope);
                    child.Method.Body.Optimize();
                }
                // ordinary method
            }

            private void AddParametersToCall(CallStackCopyContext context, CallStackCopyScope scope)
            {
                if (context.Replacer == null)
                {
                    var body = Method.Body;
                    var processor = body.GetILProcessor();

                    if (scope.Type == CallStackCopyScope.TypeEnum.METHOD)
                    {                        
                        foreach (var param in context.AddParameters)
                        {
                            processor.InsertBefore(Instruction, processor.Create(OpCodes.Ldarg, scope.Method.AddedParameters[param.Key]));
                        }
                    }
                    else if (scope.Type == CallStackCopyScope.TypeEnum.DISPLAY_CLASS)
                    {
                        foreach (var param in context.AddParameters)
                        {
                            processor.InsertBefore(Instruction, processor.Create(OpCodes.Ldarg_0));
                            processor.InsertBefore(Instruction, processor.Create(OpCodes.Ldfld, scope.DisplayClass.AddedFields[param.Key]));
                        }
                    }
                }
                else
                {
                    context.Replacer(this, context, scope);
                }
            }

            private (MethodDefinition, Instruction[]) AddCopiedMethod(CallStackCopyContext context, MethodDefinition original, MethodDefinition copy)
            {
                var newInstructions = new Instruction[original.Body.Instructions.Count];
                var m = (copy, copy.Body.Instructions.ToArray());
                context.Methods.Add(copy.FullName, m);
                context.MethodMappings.Add(original.FullName, copy.FullName);
                return m;
            }
        }
    }
}