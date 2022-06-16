 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ModAPI.Utils
{
    public static class MonoHelper
    {
        public class Delegate
        {
            public Delegate() { }

            public Delegate(TypeDefinition @delegate)
            {
                Type = @delegate;
                foreach (var m in @delegate.Methods)
                {
                    if (m.Name == "Invoke")
                        Invoke = m;
                    else if (m.Name == ".ctor")
                        Constructor = m;
                }
            }
            public TypeDefinition Type;
            public MethodDefinition Invoke;
            public MethodDefinition Constructor;
        }

        public class SignatureContext
        {
            public bool CheckParameterName = false;
            public bool IgnoreNewSlot = true;
            public bool IgnoreReturnType = false;
            public bool IgnoreCustomAttribute = false;
            public string ReturnType = null;
            public int SkipParameters = 0;
            public string MethodName = null;
            public List<ParameterDefinition> OptionalParameter = new List<ParameterDefinition>();
        }

        public static int GetStackChange(Instruction inst)
        {
            int change = 0;
            switch (inst.OpCode.StackBehaviourPop)
            {
                case StackBehaviour.Pop0:
                    change -= 0;
                    break;
                case StackBehaviour.Pop1:
                case StackBehaviour.Popi:
                case StackBehaviour.Popref:
                    change -= 1;
                    break;
                case StackBehaviour.Pop1_pop1:
                case StackBehaviour.Popi_pop1:
                case StackBehaviour.Popi_popi:
                case StackBehaviour.Popi_popi8:
                case StackBehaviour.Popi_popr4:
                case StackBehaviour.Popi_popr8:
                case StackBehaviour.Popref_pop1:
                case StackBehaviour.Popref_popi:
                    change -= 2;
                    break;
                case StackBehaviour.Popref_popi_popi:
                case StackBehaviour.Popref_popi_popi8:
                case StackBehaviour.Popref_popi_popr4:
                case StackBehaviour.Popref_popi_popr8:
                case StackBehaviour.Popref_popi_popref:
                    change -= 3;
                    break;
                case StackBehaviour.Varpop:
                    if (inst.Operand is MethodReference mref)
                        change -= mref.Parameters.Count;
                    break;
            }
            switch (inst.OpCode.StackBehaviourPush)
            {
                case StackBehaviour.Push0:
                    change += 0;
                    break;
                case StackBehaviour.Push1:
                case StackBehaviour.Pushi:
                case StackBehaviour.Pushref:
                case StackBehaviour.Pushi8:
                case StackBehaviour.Pushr4:
                case StackBehaviour.Pushr8:
                    change += 1;
                    break;
                case StackBehaviour.Varpush:
                    if (inst.Operand is MethodReference mref)
                        change += mref.ReturnType.FullName == "System.Void" ? 0 : 1;
                    break;
            }

            return change;
        }

        public static Delegate CreateDelegate(TypeDefinition parent, TypeReference returnType, string name, bool addSelfToParams, List<ParameterDefinition> parameters, TypeReference multicastDelegateType, TypeReference asyncCallbackType, TypeReference iAsyncResultType)
        {

            returnType = parent.Module.ImportReference(returnType);
            var @delegate = new TypeDefinition(parent.Namespace, name, TypeAttributes.Sealed | TypeAttributes.NestedPublic, parent.Module.ImportReference(multicastDelegateType));
            @delegate.DeclaringType = parent;
            //@delegate.IsAnsiClass = true;
            //@delegate.BaseType = parent.Module.ImportReference(multicastDelegateType);
            var constructor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, parent.Module.TypeSystem.Void);
            //constructor.IsRuntime = true;
            //constructor.IsManaged = true;
            constructor.Parameters.Add(new ParameterDefinition("obj", ParameterAttributes.None, parent.Module.TypeSystem.Object));
            constructor.Parameters.Add(new ParameterDefinition("methodPointer", ParameterAttributes.None, parent.Module.TypeSystem.IntPtr));
            constructor.ImplAttributes = MethodImplAttributes.Runtime;
            @delegate.Methods.Add(constructor);

            var invoke = new MethodDefinition("Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, returnType);
            invoke.IsRuntime = true;
            invoke.IsManaged = true;
            //invoke.Parameters.Add(new ParameterDefinition("self", ParameterAttributes.None, parent.Module.ImportReference(parent)));

            for (var i = 0; i < parameters.Count; i++)
                invoke.Parameters.Add(new ParameterDefinition(parameters[i].Name, parameters[i].Attributes, parent.Module.ImportReference(parameters[i].ParameterType)));
            if (addSelfToParams)
            {
                invoke.Parameters.Add(new ParameterDefinition("next", ParameterAttributes.None, parent.Module.ImportReference(@delegate.MakeArrayType())));
                invoke.Parameters.Add(new ParameterDefinition("num", ParameterAttributes.None, parent.Module.TypeSystem.Int32));
            }
            @delegate.Methods.Add(invoke);
            
            var beginInvoke = new MethodDefinition("BeginInvoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, parent.Module.ImportReference(iAsyncResultType));
            beginInvoke.ImplAttributes = MethodImplAttributes.Runtime;
            /*IsRuntime = true;
            beginInvoke.IsManaged = true;*/
            for (var i = 0; i < parameters.Count; i++)
                beginInvoke.Parameters.Add(new ParameterDefinition(parameters[i].Name, parameters[i].Attributes, parent.Module.ImportReference(parameters[i].ParameterType)));
            if (addSelfToParams)
            {
                beginInvoke.Parameters.Add(new ParameterDefinition("next", ParameterAttributes.None, parent.Module.ImportReference(@delegate.MakeArrayType())));
                beginInvoke.Parameters.Add(new ParameterDefinition("num", ParameterAttributes.None, parent.Module.TypeSystem.Int32));
            }
            beginInvoke.Parameters.Add(new ParameterDefinition("callback", ParameterAttributes.None, parent.Module.ImportReference(asyncCallbackType)));
            beginInvoke.Parameters.Add(new ParameterDefinition("obj", ParameterAttributes.None, parent.Module.TypeSystem.Object));
            @delegate.Methods.Add(beginInvoke);

            var endInvoke = new MethodDefinition("EndInvoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, returnType);
            endInvoke.ImplAttributes = MethodImplAttributes.Runtime;
            /*endInvoke.IsRuntime = true;
            endInvoke.IsManaged = true;*/
            for (var i = 0; i < parameters.Count; i++)
            {
                if (parameters[i].IsOut || parameters[i].IsIn || parameters[i].ParameterType is ByReferenceType)
                    endInvoke.Parameters.Add(new ParameterDefinition(parameters[i].Name, parameters[i].Attributes, parent.Module.ImportReference(parameters[i].ParameterType)));
            }
            endInvoke.Parameters.Add(new ParameterDefinition("result", ParameterAttributes.None, parent.Module.ImportReference(iAsyncResultType)));
            @delegate.Methods.Add(endInvoke);

            parent.NestedTypes.Add(@delegate);
            return new Delegate() { Type = @delegate, Constructor = constructor, Invoke = invoke };
        }

        public static MethodDefinition Copy(this MethodDefinition method, ModuleDefinition module = null)
        {
            if (module == null)
                module = method.Module;
            var newMethod = new MethodDefinition(method.Name, method.Attributes, module.ImportReference(method.ReturnType));
            for (var i = 0; i < method.CustomAttributes.Count; i++)
                newMethod.CustomAttributes.Add(new CustomAttribute(module.ImportReference(method.CustomAttributes[i].Constructor), method.CustomAttributes[i].GetBlob()));
            for (var i = 0; i < method.Parameters.Count; i++)
                newMethod.Parameters.Add(new ParameterDefinition(method.Parameters[i].Name, method.Parameters[i].Attributes, module.ImportReference(method.Parameters[i].ParameterType)));
            for (var i = 0; i < method.GenericParameters.Count; i++)
                newMethod.GenericParameters.Add(new GenericParameter(method.GenericParameters[i].Name, newMethod));

            if (newMethod.Body == null)
                newMethod.Body = new MethodBody(newMethod);
            method.Body.Copy(newMethod.Body, module);
            return newMethod;
        }

        public static void Copy(this MethodBody body, MethodBody into, ModuleDefinition module = null)
        {
            if (module == null)
                module = into.Method.DeclaringType.Module;
            into.InitLocals = body.InitLocals;
            for (var i = 0; i < body.Variables.Count; i++)
                into.Variables.Add(new VariableDefinition(module.ImportReference(body.Variables[i].VariableType)));

            Dictionary<int, Instruction> offsets = new Dictionary<int, Instruction>();

            var processor = into.GetILProcessor();
            for (var i = 0; i < body.Instructions.Count; i++)
            {
                var instr = body.Instructions[i];
                var operand = instr.Operand;
                Instruction newInstr = null;
                if (operand == null)
                    newInstr = Instruction.Create(instr.OpCode);
                else if (operand is MethodReference methodRef)
                    newInstr = Instruction.Create(instr.OpCode, module.ImportReference(methodRef));
                else if (operand is GenericInstanceMethod genericInstanceRef)
                    newInstr = Instruction.Create(instr.OpCode, module.ImportReference(genericInstanceRef));
                else if (operand is FieldReference fieldRef)
                    newInstr = Instruction.Create(instr.OpCode, module.ImportReference(fieldRef));
                else if (operand is TypeReference typeRef)
                    newInstr = Instruction.Create(instr.OpCode, module.ImportReference(typeRef));
                else if (operand is GenericInstanceType genericTypeInstance)
                    newInstr = Instruction.Create(instr.OpCode, module.ImportReference(genericTypeInstance));
                else if (operand is ParameterDefinition param)
                    newInstr = Instruction.Create(instr.OpCode, into.Method.Parameters[param.Index]);
                else if (operand is VariableDefinition variable)
                    newInstr = Instruction.Create(instr.OpCode, into.Variables[variable.Index]);
                else if (operand is Instruction _instr)
                    newInstr = Instruction.Create(instr.OpCode, body.Instructions[0]);
                else if (operand is Instruction[] _instrs)
                    newInstr = Instruction.Create(instr.OpCode, new Instruction[_instrs.Length]);
                else if (operand is string str)
                    newInstr = Instruction.Create(instr.OpCode, str);
                else if (operand is byte b)
                    newInstr = Instruction.Create(instr.OpCode, b);
                else if (operand is sbyte sb)
                    newInstr = Instruction.Create(instr.OpCode, sb);
                else if (operand is short s)
                    newInstr = Instruction.Create(instr.OpCode, s);
                else if (operand is ushort us)
                    newInstr = Instruction.Create(instr.OpCode, us);
                else if (operand is int @in)
                    newInstr = Instruction.Create(instr.OpCode, @in);
                else if (operand is uint uin)
                    newInstr = Instruction.Create(instr.OpCode, uin);
                else if (operand is long lo)
                    newInstr = Instruction.Create(instr.OpCode, lo);
                else if (operand is ulong ulo)
                    newInstr = Instruction.Create(instr.OpCode, ulo);
                else if (operand is float f)
                    newInstr = Instruction.Create(instr.OpCode, f);
                else if (operand is double d)
                    newInstr = Instruction.Create(instr.OpCode, d);
                else if (operand is GenericParameter p)
                    newInstr = Instruction.Create(instr.OpCode, p);
                else
                    newInstr = Instruction.Create(instr.OpCode);
                processor.Append(newInstr);
                //into.Instructions.Add(newInstr);
                offsets.Add(instr.Offset, newInstr);
            }
            // fix jumps
            for (var i = 0; i < body.Instructions.Count; i++)
            {
                var instr = body.Instructions[i];
                var operand = instr.Operand;
                if (operand is Instruction _instr)
                    into.Instructions[i].Operand = offsets[(body.Instructions[i].Operand as Instruction).Offset];
                else if (operand is Instruction[] _instrs)
                {
                    if (body.Instructions[i].Operand is Instruction[] instrs && into.Instructions[i].Operand is Instruction[] newInstrs)
                    {
                        for (var j = 0; j < newInstrs.Length; j++)
                        {
                            newInstrs[j] = offsets[instrs[j].Offset];
                        }
                    }
                }
            }

            for (var i = 0; i < body.ExceptionHandlers.Count; i++)
            {
                var newExceptionHandler = new ExceptionHandler(body.ExceptionHandlers[i].HandlerType);
                if (body.ExceptionHandlers[i].CatchType != null)
                    newExceptionHandler.CatchType = module.ImportReference(body.ExceptionHandlers[i].CatchType);
                if (body.ExceptionHandlers[i].HandlerStart != null)
                    newExceptionHandler.HandlerStart = offsets[body.ExceptionHandlers[i].HandlerStart.Offset];
                if (body.ExceptionHandlers[i].HandlerEnd != null)
                    newExceptionHandler.HandlerEnd = offsets[body.ExceptionHandlers[i].HandlerEnd.Offset];
                if (body.ExceptionHandlers[i].FilterStart != null)
                    newExceptionHandler.FilterStart = offsets[body.ExceptionHandlers[i].FilterStart.Offset];
                if (body.ExceptionHandlers[i].TryStart != null)
                    newExceptionHandler.TryStart = offsets[body.ExceptionHandlers[i].TryStart.Offset];
                if (body.ExceptionHandlers[i].TryEnd != null)
                    newExceptionHandler.TryEnd = offsets[body.ExceptionHandlers[i].TryEnd.Offset];
                into.ExceptionHandlers.Add(newExceptionHandler);
            }
        }

        public static MethodReference CloneGenericInstance(this MethodReference method, GenericInstanceMethod other, ModuleDefinition module = null)
        {
            if (module == null)
                module = method.Module;
            method = module.ImportReference(method);

            for (var i = 0; i < method.GenericParameters.Count; i++)
                method.GenericParameters[i].Name = "!!" + i;
            var genericParameters = other.GetGenericParameters(module);
            var declaringType = method.DeclaringType;
            if (declaringType is TypeReference type)
            {
                var genericType = new GenericInstanceType(declaringType);
                for (var i = 0; i < type.GenericParameters.Count; i++)
                    if (genericParameters.ContainsKey(type.GenericParameters[i].Name))
                        genericType.GenericArguments.Add(genericParameters[type.GenericParameters[i].Name]);
                var genericMethod = new MethodReference(method.Name, module.ImportReference(method.ReturnType), module.ImportReference(genericType));
                for (var i = 0; i < method.Parameters.Count; i++)
                    genericMethod.Parameters.Add(new ParameterDefinition(method.Parameters[i].Name, method.Parameters[i].Attributes, module.ImportReference(method.Parameters[i].ParameterType)));
                for (var i = 0; i < method.GenericParameters.Count; i++)
                    genericMethod.GenericParameters.Add(new GenericParameter(method.GenericParameters[i].Name, method.GenericParameters[i].Owner));
                method = genericMethod;
                declaringType = genericType;
            }

            var ret = new GenericInstanceMethod(module.ImportReference(method));

            for (var i = 0; i < method.GenericParameters.Count; i++)
                if (genericParameters.ContainsKey(method.GenericParameters[i].Name))
                    ret.GenericArguments.Add(genericParameters[method.GenericParameters[i].Name]);

            return ret;
        }

        public static Dictionary<string, TypeReference> GetGenericParameters(this GenericInstanceMethod method, ModuleDefinition module = null)
        {
            if (module == null)
                module = method.Module;
            var ret = new Dictionary<string, TypeReference>();
            if (method.DeclaringType is GenericInstanceType type)
            {
                var t = type.GetGenericParameters(module);
                foreach (var kv in t)
                    ret.Add(kv.Key, kv.Value);
            }
            for (var i = 0; i < method.ElementMethod.GenericParameters.Count; i++)
                ret.Add(method.ElementMethod.GenericParameters[i].Name, module.ImportReference(method.GenericArguments[i]));
            return ret;
        }

        public static Dictionary<string, TypeReference> GetGenericParameters(this GenericInstanceType type, ModuleDefinition module = null)
        {
            if (module == null)
                module = type.Module;
            var ret = new Dictionary<string, TypeReference>();
            for (var i = 0; i < type.ElementType.GenericParameters.Count; i++)
                ret.Add(type.ElementType.GenericParameters[i].Name, module.ImportReference(type.GenericArguments[i]));
            return ret;
        }

        public static string GetGenericFullName(this GenericInstanceMethod methodRef, List<string> genericParams = null)
        {
            if (genericParams == null) genericParams = new();
            foreach (var p in methodRef.GenericArguments)
            {
                if (p.IsGenericParameter)
                    genericParams.Add(p.Name);
            }
            return GetGenericFullName(methodRef.ElementMethod, genericParams);
        }

        public static string GetGenericFullName(this MethodReference methodRef, List<string> genericParams = null)
        {
            if (methodRef is GenericInstanceMethod generic)
                return generic.GetGenericFullName(genericParams);

            if (genericParams == null) genericParams = new();
            var className = methodRef.DeclaringType.GetGenericFullName(false, genericParams);

            foreach (var genericParam in methodRef.GenericParameters)
            {
                int ind = genericParams.IndexOf(genericParam.Name);
                if (ind == -1)
                    genericParams.Add(genericParam.Name);
            }

            string parameters = "";
            bool first = true;
            foreach (var param in methodRef.Parameters)
            {
                if (!first)
                    parameters += ",";
                first = false;
                parameters += param.ParameterType.GetGenericFullName(true, genericParams);
            }
            return methodRef.ReturnType.GetGenericFullName(true, genericParams) + " " + 
                className + "::" + 
                methodRef.Name + "(" + parameters + ")";
        }
        
        public static string GetGenericFullName(this GenericInstanceType typeRef, bool useGenericInstance = false, List<string> genericParams = null)
        {
            if (genericParams == null) genericParams = new();
            if (typeRef.ElementType != null)
            {
                if (useGenericInstance)
                {
                    var n = typeRef.ElementType.FullName + "<";
                    bool first = true;
                    for (var i = 0; i < typeRef.GenericArguments.Count; i++)
                    {
                        if (!first)
                            n += ",";
                        if (typeRef.GenericArguments[i].IsGenericParameter)
                        {
                            var ind = genericParams.IndexOf(typeRef.GenericArguments[i].Name);
                            if (ind == -1)
                            {
                                ind = genericParams.Count;
                                genericParams.Add(typeRef.GenericArguments[i].Name);
                            }
                            n += "!!" + ind;
                        }
                        else n += typeRef.GenericArguments[i].FullName;
                        first = false;
                    }
                    n += ">";
                    return n;
                }
                else return typeRef.ElementType.GetGenericFullName(useGenericInstance, genericParams);
            }
            else return typeRef.FullName;
        }

        public static string GetGenericFullName(this TypeReference typeRef, bool useGenericInstance = false, List<string> genericParams = null)
        {
            if (typeRef is ByReferenceType byRef)
                return byRef.GetGenericFullName(useGenericInstance, genericParams);
            if (typeRef is GenericInstanceType generic)
                return generic.GetGenericFullName(useGenericInstance, genericParams);

            if (typeRef.IsGenericParameter)
            {
                int ind = genericParams.IndexOf(typeRef.Name);
                if (ind == -1)
                {
                    ind = genericParams.Count;
                    genericParams.Add(typeRef.Name);
                }
                return "!!" + ind;
            }    
            if (genericParams == null) genericParams = new();
            foreach (var genericParam in typeRef.GenericParameters)
            {
                int ind = genericParams.IndexOf(genericParam.Name);
                if (ind == -1)
                    genericParams.Add(genericParam.Name);
            }
            return typeRef.FullName;
        }

        public static string GetGenericFullName(this ByReferenceType typeRef, bool useGenericInstance = false, List<string> genericParams = null)
        {
            return typeRef.ElementType.GetGenericFullName(useGenericInstance, genericParams) + "&";
        }

        public static bool MatchingSignature(this MethodDefinition p1, MethodDefinition p2, SignatureContext context = null)
        {
            if (p1.Name == p2.Name)
                System.Console.WriteLine("A");
            var optionalParameters = context.OptionalParameter?.Count ?? 0;

            if (context == null) context = new SignatureContext();
            if ((context.MethodName != null && p1.Name != context.MethodName) ||
                (context.MethodName == null && p1.Name != p2.Name))
                return false;
            if (p1.IsAbstract != p2.IsAbstract)
                return false;
            if (p1.IsStatic != p2.IsStatic)
                return false;
            if (p1.IsPublic != p2.IsPublic)
                return false;
            if (p1.IsAssembly != p2.IsAssembly)
                return false;
            if (p1.IsFamily != p2.IsFamily)
                return false;
            if (p1.IsFinal != p2.IsFinal)
                return false;
            if (p1.IsGenericInstance != p2.IsGenericInstance)
                return false;
            if ((!context.IgnoreNewSlot && p1.IsNewSlot != p2.IsNewSlot))
                return false;
            if (p1.IsGetter != p2.IsGetter)
                return false;
            if (p1.IsSetter != p2.IsSetter)
                return false;
            if (p1.IsPrivate != p2.IsPrivate)
                return false;
            if (p1.IsHideBySig != p2.IsHideBySig)
                return false;
            if (p1.IsSpecialName != p2.IsSpecialName)
                return false;
            if (p1.IsVirtual != p2.IsVirtual)
                return false;
            if ((!context.IgnoreCustomAttribute && p1.CustomAttributes.Count != p2.CustomAttributes.Count))
                return false;
            if (p1.GenericParameters.Count != p2.GenericParameters.Count)
                return false;
            if (p1.Parameters.Count < p2.Parameters.Count - context.SkipParameters &&
                p1.Parameters.Count > p2.Parameters.Count
                //(optionalParameters > 0 && p1.Parameters.Count <= p2.Parameters.Count + context.SkipParameters + optionalParameters && p1.Parameters.Count >= p2.Parameters.Count + context.SkipParameters + optionalParameters
                )
                return false;
            if ((!context.IgnoreReturnType && context.ReturnType == null && p1.ReturnType.FullName != p2.ReturnType.FullName) ||
                (!context.IgnoreReturnType && context.ReturnType != null && p1.ReturnType.FullName != context.ReturnType))
                return false;

            for (var i = 0; i < p1.GenericParameters.Count; i++)
                if (!p1.GenericParameters[i].MatchingSignature(p2.GenericParameters[i], context))
                    return false;

            if (!context.IgnoreCustomAttribute)
                for (var i = 0; i < p1.CustomAttributes.Count; i++)
                    if (!p1.CustomAttributes[i].MatchingSignature(p2.CustomAttributes[i], context))
                        return false;


            bool validParametersFound = false;
            var skip = p2.Parameters.Count - p1.Parameters.Count;
            if (skip > context.SkipParameters)
                return false;

            for (var i = 0; i < p1.Parameters.Count; i++)
            {
                if (!p1.Parameters[i].MatchingSignature(p2.Parameters[(i + skip)], context))
                {
                    return false;
                }
            }
            /*
            bool validParametersFound = false;
            for (var j = 0; j < optionalParameters; j++)
            {
                bool isValid = true;
                for (var i = 0; i < p1.Parameters.Count + j; i++)
                {
                    if (i >= j)
                    {
                        if (!p1.Parameters[i].MatchingSignature(p2.Parameters[(i + context.SkipParameters)], context))
                        {
                            isValid = false;
                            break;
                        }
                    }
                    else if(!p1.Parameters[i].MatchingSignature(context.OptionalParameter[i], context))
                    {
                        isValid = false;
                        break;
                    }
                }
                if (isValid)
                {
                    validParametersFound = true;
                    break;
                }
            }

            if (!validParametersFound)
                return false;
            */
            return true;
        }
        
        public static bool MatchingSignature(this GenericParameter p1, GenericParameter p2, SignatureContext context = null)
        {
            if (context == null) context = new SignatureContext();
            if (/*p1.IsFunctionPointer != p2.IsFunctionPointer ||
                p1.IsDefinition != p2.IsDefinition ||
                p1.IsContravariant != p2.IsContravariant ||
                p1.IsCovariant != p2.IsCovariant ||
                p1.IsArray != p2.IsArray ||
                p1.IsPointer != p2.IsPointer ||
                p1.IsPinned != p2.IsPinned ||
                p1.IsByReference != p2.IsByReference ||
                p1.IsNested != p2.IsNested ||
                p1.IsNonVariant != p2.IsNonVariant ||
                p1.IsOptionalModifier != p2.IsOptionalModifier ||
                p1.IsPrimitive != p2.IsPrimitive ||
                p1.IsRequiredModifier != p2.IsRequiredModifier ||
                p1.IsSentinel != p2.IsSentinel ||
                p1.IsValueType != p2.IsValueType ||
                p1.IsWindowsRuntimeProjection != p2.IsWindowsRuntimeProjection || 
                attributes might be enough? */
                p1.Attributes != p2.Attributes ||
                p1.Name != p2.Name ||
                p1.Position != p2.Position ||
                p1.Type != p2.Type ||
                p1.Constraints.Count != p2.Constraints.Count ||
                p1.GenericParameters.Count != p2.GenericParameters.Count ||
                p1.CustomAttributes.Count != p2.CustomAttributes.Count)
                return false;

            for (var i = 0; i < p1.Constraints.Count; i++)
            {
                if (!p1.Constraints[i].MatchingSignature(p2.Constraints[i], context))
                    return false;
            }

            for (var i = 0; i < p1.GenericParameters.Count; i++)
            {
                if (!p1.GenericParameters[i].MatchingSignature(p2.GenericParameters[i], context))
                    return false;
            }

            for (var i = 0; i < p1.CustomAttributes.Count; i++)
            {
                if (!p1.CustomAttributes[i].MatchingSignature(p2.CustomAttributes[i], context))
                    return false;
            }

            return true;
        }
        public static bool MatchingSignature(this ParameterDefinition p1, ParameterDefinition p2, SignatureContext context = null)
        {
            if (context == null) context = new SignatureContext();
            if (p1.ParameterType.FullName != p2.ParameterType.FullName ||
                p1.Constant != p2.Constant ||
                p1.Attributes != p2.Attributes ||
                p1.CustomAttributes.Count != p2.CustomAttributes.Count ||
                p1.Index != p2.Index ||
                (context.CheckParameterName && p1.Name != p2.Name))
                return false;

            for (var i = 0; i < p1.CustomAttributes.Count; i++)
            {
                if (!p1.CustomAttributes[i].MatchingSignature(p2.CustomAttributes[i], context))
                    return false;
            }

            return true;
        }

        public static bool MatchingSignature(this GenericParameterConstraint p1, GenericParameterConstraint p2, SignatureContext context = null)
        {
            if (context == null) context = new SignatureContext();
            if (p1.ConstraintType != p2.ConstraintType ||
                p1.CustomAttributes.Count != p2.CustomAttributes.Count)
                return false;

            for (var i = 0; i < p1.CustomAttributes.Count; i++)
            {
                if (!p1.CustomAttributes[i].MatchingSignature(p2.CustomAttributes[i], context))
                    return false;
            }

            return true;
        }
        public static bool MatchingSignature(this CustomAttribute p1, CustomAttribute p2, SignatureContext context = null)
        {
            if (context == null) context = new SignatureContext();
            if (p1.ConstructorArguments.Count != p2.ConstructorArguments.Count ||
                p1.Constructor.FullName != p2.Constructor.FullName ||
                p1.AttributeType.FullName != p2.AttributeType.FullName ||
                p1.Fields.Count != p2.Fields.Count ||
                p1.Properties.Count != p2.Properties.Count)
                return false;

            for (var i = 0; i < p1.ConstructorArguments.Count; i++)
            {
                if (!p1.ConstructorArguments[i].MatchingSignature(p2.ConstructorArguments[i], context))
                    return false;
            }

            for (var i = 0; i < p1.Fields.Count; i++)
            {
                if (!p1.Fields[i].MatchingSignature(p2.Fields[i], context))
                    return false;
            }

            for (var i = 0; i < p1.Properties.Count; i++)
            {
                if (!p1.Properties[i].MatchingSignature(p2.Properties[i], context))
                    return false;
            }

            return true;
        }

        public static bool MatchingSignature(this CustomAttributeArgument p1, CustomAttributeArgument p2, SignatureContext context = null)
        {
            if (context == null) context = new SignatureContext();
            if (p1.Type.FullName != p2.Type.FullName ||
                p1.Value != p2.Value)
                return false;
            return true;
        }

        public static bool MatchingSignature(this CustomAttributeNamedArgument p1, CustomAttributeNamedArgument p2, SignatureContext context = null)
        {
            if (context == null) context = new SignatureContext();
            if (!p1.Argument.MatchingSignature(p2.Argument, context) ||
                p1.Name != p2.Name)
                return false;
            return true;
        }

        public static GenericParameter GetGenericParameter(this TypeReference type)
        {
            var t = type;
            while (t.IsArray)
                t = type.GetElementType();
            if (t.IsGenericParameter)
            {
                foreach (var p in t.DeclaringType.GenericParameters)
                {
                    if (p.Name == t.Name)
                        return p;
                }
            }
            return null;
        }

        public static string GetIdentifier(this FieldReference field)
        {
            if (field.DeclaringType.Scope is AssemblyNameReference nameReference)
                return "[" + nameReference.Name + "]" + field.FullName;
            else
                return "[" + field.Module.Assembly.Name.Name + "]" + field.FullName;
        }

        public static string GetIdentifier(this MethodReference method)
        {
            if (method.DeclaringType.Scope is AssemblyNameReference nameReference)
                return "[" + nameReference.Name + "]" + method.FullName;
            else
                return "[" + method.Module.Assembly.Name.Name + "]" + method.FullName;
        }

        public static string GetIdentifier(this TypeReference type)
        {
            if (type.Scope is AssemblyNameReference nameReference)
                return "[" + nameReference.Name + "]" + type.FullName;
            else
                return "[" + type.Module.Assembly.Name.Name + "]" + type.FullName;
        }

        public static string GetIdentifier(this PropertyReference property)
        {
            if (property.DeclaringType.Scope is AssemblyNameReference nameReference)
                return "[" + nameReference.Name + "]" + property.FullName;
            else
                return "[" + property.Module.Assembly.Name.Name + "]" + property.FullName;
        }

        public static string GetIdentifier(this FieldDefinition field)
        {
            return "[" + field.Module.Assembly.Name.Name + "]" + field.FullName;
        }

        public static string GetIdentifier(this MethodDefinition method)
        {
            return "[" + method.Module.Assembly.Name.Name + "]" + method.FullName;
        }

        public static string GetIdentifier(this TypeDefinition type)
        {
            return "[" + type.Module.Assembly.Name.Name + "]" + type.FullName;
        }

        public static string GetIdentifier(this PropertyDefinition property)
        {
            return "[" + property.Module.Assembly.Name.Name + "]" + property.FullName;
        }

        public static void RedirectJumps(this MethodBody body, Instruction originalInstruction, Instruction newInstruction)
        {
            for (var i = 0; i < body.Instructions.Count; i++)
            {
                var instruction = body.Instructions[i];
                if ((instruction.OpCode.Code == Code.Br ||
                    instruction.OpCode.Code == Code.Brfalse ||
                    instruction.OpCode.Code == Code.Brtrue) && instruction.Operand is Instruction inst0)
                {
                    if (inst0 == originalInstruction)
                        instruction.Operand = newInstruction;
                }
            }
        }

        private class ReplaceAssemblyReferenceContext
        {
            public AssemblyNameReference Reference;
            public Dictionary<string, TypeDefinition> Types = new();
            public Dictionary<string, FieldDefinition> Fields = new();
            public Dictionary<string, MethodDefinition> Methods = new();
            public Dictionary<string, EventDefinition> Events = new();
            public Dictionary<string, PropertyDefinition> Properties = new();
        }
        private static MethodReference ReplaceAssemblyReference(MethodReference method, AssemblyNameReference reference, ReplaceAssemblyReferenceContext context)
        {
            if (method is GenericInstanceMethod generic)
            {
                if (generic.ElementMethod.IsSameScope(reference) && context.Methods.ContainsKey(generic.ElementMethod.FullName))
                {
                    var newMethod = new GenericInstanceMethod(generic.Module.ImportReference(context.Methods[generic.ElementMethod.FullName]));
                    foreach (var genericArgument in generic.GenericArguments)
                    {
                        if (context.Types.ContainsKey(genericArgument.FullName))
                            newMethod.GenericArguments.Add(method.Module.ImportReference(context.Types[genericArgument.FullName]));
                        else
                            newMethod.GenericArguments.Add(genericArgument);
                    }
                    return newMethod;
                }
            }
            else if (method.IsSameScope(reference))
            {
                if (context.Methods.ContainsKey(method.FullName))
                    return method.Module.ImportReference(context.Methods[method.FullName]);
                    //method.DeclaringType.Scope = context.Reference;
            }
            method.ReturnType = ReplaceAssemblyReference(method.ReturnType, reference, context);
            foreach (var param in method.Parameters)
            {
                foreach (var customAttribute in param.CustomAttributes)
                    ReplaceAssemblyReference(customAttribute, reference, context);
                param.ParameterType = ReplaceAssemblyReference(param.ParameterType, reference, context);
            }
            foreach (var genericParameter in method.GenericParameters)
                ReplaceAssemblyReference(genericParameter, reference, context);
            return method;
        }

        private static TypeReference ReplaceAssemblyReference(TypeReference type, AssemblyNameReference reference, ReplaceAssemblyReferenceContext context)
        {
            if (type is GenericInstanceType generic)
            {
                if (context.Types.ContainsKey(generic.ElementType.FullName))
                {
                    var newType = new GenericInstanceType(generic.Module.ImportReference(context.Types[generic.ElementType.FullName]));
                    foreach (var genericArgument in generic.GenericArguments)
                    {
                        if (context.Types.ContainsKey(genericArgument.FullName))
                            newType.GenericArguments.Add(type.Module.ImportReference(context.Types[genericArgument.FullName]));
                        else
                            newType.GenericArguments.Add(genericArgument);
                    }
                    return newType;
                }
            }
            else if (type.IsSameScope(reference))
            {
                if (context.Types.ContainsKey(type.FullName))
                    return type.Module.ImportReference(context.Types[type.FullName]);
            }
            return type;
        }


        private static void ReplaceAssemblyReference(FieldReference field, AssemblyNameReference reference, ReplaceAssemblyReferenceContext context)
        {
            field.FieldType = ReplaceAssemblyReference(field.FieldType, reference, context);
        }

        private static void ReplaceAssemblyReference(GenericParameter genericParameter, AssemblyNameReference reference, ReplaceAssemblyReferenceContext context)
        {
            foreach (var customAttribute in genericParameter.CustomAttributes)
                ReplaceAssemblyReference(customAttribute, reference, context);
            if (genericParameter.DeclaringType != null)
                genericParameter.DeclaringType = ReplaceAssemblyReference(genericParameter.DeclaringType, reference, context);
            /*if (genericParameter.DeclaringMethod != null)
                genericParameter.DeclaringMethod = ReplaceAssemblyReference(genericParameter.DeclaringMethod, reference, context);*/
            foreach (var constraint in genericParameter.Constraints)
            {
                foreach (var customAttribute in constraint.CustomAttributes)
                    ReplaceAssemblyReference(customAttribute, reference, context);
                constraint.ConstraintType = ReplaceAssemblyReference(constraint.ConstraintType, reference, context);
            }
            foreach (var g in genericParameter.GenericParameters)
                ReplaceAssemblyReference(g, reference, context);
        }

        private static void ReplaceAssemblyReference(FieldDefinition field, AssemblyNameReference reference, ReplaceAssemblyReferenceContext context)
        {
            foreach (var customAttribute in field.CustomAttributes)
                ReplaceAssemblyReference(customAttribute, reference, context);
            field.FieldType = ReplaceAssemblyReference(field.FieldType, reference, context);
        }

        private static void ReplaceAssemblyReference(PropertyDefinition property, AssemblyNameReference reference, ReplaceAssemblyReferenceContext context)
        {
            foreach (var customAttribute in property.CustomAttributes)
                ReplaceAssemblyReference(customAttribute, reference, context);
            property.PropertyType = ReplaceAssemblyReference(property.PropertyType, reference, context);
            foreach (var param in property.Parameters)
            {
                foreach (var customAttribute in param.CustomAttributes)
                    ReplaceAssemblyReference(customAttribute, reference, context);
                param.ParameterType = ReplaceAssemblyReference(param.ParameterType, reference, context);
            }
        }

        private static void ReplaceAssemblyReference(MethodDefinition method, AssemblyNameReference reference, ReplaceAssemblyReferenceContext context)
        {
            foreach (var customAttribute in method.CustomAttributes)
                ReplaceAssemblyReference(customAttribute, reference, context);
            method.ReturnType = ReplaceAssemblyReference(method.ReturnType, reference, context);
            foreach (var genericParameter in method.GenericParameters)
                ReplaceAssemblyReference(genericParameter, reference, context);
            foreach (var param in method.Parameters)
            {
                foreach (var customAttribute in param.CustomAttributes)
                    ReplaceAssemblyReference(customAttribute, reference, context);
                param.ParameterType = ReplaceAssemblyReference(param.ParameterType, reference, context);
            }
            foreach (var variable in method.Body.Variables)
                variable.VariableType = ReplaceAssemblyReference(variable.VariableType, reference, context);
            foreach (var exceptionHandler in method.Body.ExceptionHandlers)
                if (exceptionHandler.CatchType != null)
                    exceptionHandler.CatchType = ReplaceAssemblyReference(exceptionHandler.CatchType, reference, context);
            foreach (var instruction in method.Body.Instructions)
            {
                if (instruction.Operand is FieldReference fieldRef)
                    ReplaceAssemblyReference(fieldRef, reference, context);
                if (instruction.Operand is TypeReference typeRef)
                    instruction.Operand = ReplaceAssemblyReference(typeRef, reference, context);
                if (instruction.Operand is MethodReference methodRef)
                    instruction.Operand = ReplaceAssemblyReference(methodRef, reference, context);
            }
        }

        private static void ReplaceAssemblyReference(TypeDefinition type, AssemblyNameReference reference, ReplaceAssemblyReferenceContext context)
        {
            foreach (var nestedType in type.NestedTypes)
                ReplaceAssemblyReference(nestedType, reference, context);

            if (type.BaseType != null)
                type.BaseType = ReplaceAssemblyReference(type.BaseType, reference, context);
            for (var i = 0; i < type.Interfaces.Count; i++)
                type.Interfaces[i].InterfaceType = ReplaceAssemblyReference(type.Interfaces[i].InterfaceType, reference, context);
            foreach (var customAttribute in type.CustomAttributes)
                ReplaceAssemblyReference(customAttribute, reference, context);
            foreach (var genericParameter in type.GenericParameters)
                ReplaceAssemblyReference(genericParameter, reference, context);
            foreach (var field in type.Fields)
                ReplaceAssemblyReference(field, reference, context);
            foreach (var property in type.Properties)
                ReplaceAssemblyReference(property, reference, context);
            /*foreach (var @event in type.Events)
                ReplaceAssemblyReference(@event, reference, context);*/
            foreach (var method in type.Methods)
                ReplaceAssemblyReference(method, reference, context);
        }

        private static void ReplaceAssemblyReference(CustomAttribute attribute, AssemblyNameReference reference, ReplaceAssemblyReferenceContext context)
        {
//            attribute.AttributeType = ReplaceAssemblyReference(attribute.AttributeType, reference, context);
            attribute.Constructor = ReplaceAssemblyReference(attribute.Constructor, reference, context);
            foreach (var c in attribute.ConstructorArguments)
            {
                //c.Type = ReplaceAssemblyReference(c.Type, reference, context);
                /*if (c.Type is GenericInstanceType generic)
                {
                    if (generic.ElementType.IsSameScope(reference))
                        generic.ElementType.Scope = context.Reference;
                }
                else if (c.Type.IsSameScope(reference))
                    c.Type.Scope = context.Reference;*/
            }
        }

        public static bool IsSameScope(this TypeReference reference, AssemblyNameReference assemblyReference)
        {
            if (reference.Scope is AssemblyNameReference a)
                return a.Name == assemblyReference.Name && a.Version.Equals(assemblyReference.Version);
            return false;
        }

        public static bool IsSameScope(this MemberReference reference, AssemblyNameReference assemblyReference)
        {
            if (reference.DeclaringType.Scope is AssemblyNameReference a)
                return a.Name == assemblyReference.Name && a.Version.Equals(assemblyReference.Version);
            return false;
        }

        private static void BuildReplaceAssemblyReferenceContext(TypeDefinition type, ReplaceAssemblyReferenceContext context)
        {
            foreach (var nestedType in type.NestedTypes)
                BuildReplaceAssemblyReferenceContext(nestedType, context);

            if (!context.Types.ContainsKey(type.FullName))
                context.Types.Add(type.FullName, type);
            
            foreach (var method in type.Methods)
                if (!context.Methods.ContainsKey(method.FullName))
                    context.Methods.Add(method.FullName, method);
            foreach (var @event in type.Events)
                if (!context.Events.ContainsKey(@event.FullName))
                    context.Events.Add(@event.FullName, @event);
            foreach (var property in type.Properties)
                if (!context.Properties.ContainsKey(property.FullName))
                    context.Properties.Add(property.FullName, property);
            foreach (var field in type.Fields)
                if (!context.Fields.ContainsKey(field.FullName))
                    context.Fields.Add(field.FullName, field);
        }
        public static void ReplaceAssemblyReference(this AssemblyDefinition assembly, AssemblyNameReference reference, AssemblyDefinition newReference)
        {
            var context = new ReplaceAssemblyReferenceContext();
            context.Reference = newReference.Name;
            foreach (var module in newReference.Modules)
                foreach (var type in module.Types)
                    BuildReplaceAssemblyReferenceContext(type, context);
            foreach (var module in assembly.Modules)
            {
                foreach (var type in module.Types)
                    ReplaceAssemblyReference(type, reference, context);
                foreach (var attribute in module.CustomAttributes)
                    ReplaceAssemblyReference(attribute, reference, context);
            }
            foreach (var attribute in assembly.CustomAttributes)
                ReplaceAssemblyReference(attribute, reference, context);
            foreach (var module in assembly.Modules)
            {
                for (var i = 0; i < module.AssemblyReferences.Count; i++)
                {
                    if (module.AssemblyReferences[i].Name == reference.Name && module.AssemblyReferences[i].Version == reference.Version)
                    {
                        module.AssemblyReferences.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        public static void ReplaceDisplayMethodWithDisplayClass(MethodDefinition method, Instruction ldftnInstruction, List<(TypeReference, string, Instruction)> addParameters, MethodReference compilerGeneratedAttributeType, MethodReference objectConstructor)
        {
            if (ldftnInstruction.Previous.OpCode != OpCodes.Ldarg_0 && ldftnInstruction.Previous.OpCode != OpCodes.Ldsfld)
                return;

            var body = method.Body;
            if (ldftnInstruction.Operand is MethodReference mref)
            {
                var type = method.DeclaringType;
                MethodDefinition calledMethod = null;
                var searchType = type;
                if (mref.DeclaringType.Name == "<>c")
                {
                    foreach (var n in type.NestedTypes)
                    {
                        if (n.Name == "<>c")
                        {
                            searchType = n;
                            break;
                        }
                    }
                }
                foreach (var m in searchType.Methods)
                {
                    if (m.FullName == mref.FullName)
                    {
                        calledMethod = m;
                        break;
                    }
                }
                if (calledMethod != null)
                {
                    var displayClassName = "<>c__DisplayClass8_0";// GetNextDisplayClassName(type);
                    var newClass = new TypeDefinition("", displayClassName, TypeAttributes.AnsiClass | TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
                    
                    newClass.BaseType = method.Module.TypeSystem.Object;
                    newClass.CustomAttributes.Add(new CustomAttribute(compilerGeneratedAttributeType, new byte[] { 1, 0, 0, 0 }));

                    //var thisField = new FieldDefinition("<>4__this", FieldAttributes.Public, method.Module.ImportReference(type));
                    //newClass.Fields.Add(thisField);

                    var constructor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, type.Module.TypeSystem.Void);
                    var processor = constructor.Body.GetILProcessor();

                    //type.Module.TypeSystem.Object.
                    //processor.Append(processor.Create(OpCodes.Ldarg_0));
                    //processor.Append(processor.Create(OpCodes.Call));
                    processor.Append(processor.Create(OpCodes.Ldarg_0));
                    processor.Append(processor.Create(OpCodes.Call, objectConstructor));
                    processor.Append(processor.Create(OpCodes.Nop));
                    processor.Append(processor.Create(OpCodes.Ret));
                    newClass.Methods.Add(constructor);

                    Dictionary<string, FieldDefinition> fields = new Dictionary<string, FieldDefinition>();
                    foreach (var pa in addParameters)
                    {
                        var field = new FieldDefinition(pa.Item2, FieldAttributes.Public, method.Module.ImportReference(pa.Item1));
                        newClass.Fields.Add(field);
                        fields.Add(pa.Item2, field);
                    }

                    searchType.Methods.Remove(calledMethod);
                    var ind = calledMethod.Name.IndexOf(">");
                    calledMethod.Name = calledMethod.Name.Substring(0, ind + 1) + "b__0";
                    //calledMethod.Body.Instructions[1] = Instruction.Create(OpCodes.Ldfld, fields["test"]);
                    newClass.Methods.Add(calledMethod);

                    type.NestedTypes.Add(newClass);
                    var callBody = calledMethod.Body;
                    var ill = callBody.GetILProcessor();
                    ill.Replace(callBody.Instructions[1], Instruction.Create(OpCodes.Ldfld, fields["b"]));
                    ill.InsertBefore(callBody.Instructions[1], Instruction.Create(OpCodes.Ldarg_0));

                    method.Body.SimplifyMacros();
                    var localVar = new VariableDefinition(method.Module.ImportReference(newClass));
                    body.Variables.Insert(0, localVar);
                    
                    if (ldftnInstruction.Previous.OpCode == OpCodes.Ldarg_0)
                    {
                        // class method
                    }
                    else if (ldftnInstruction.Previous.OpCode == OpCodes.Ldsfld)
                    {
                        method.Body.Instructions.RemoveAt(0);
                        var removeInstructions = new List<Instruction>();
                        removeInstructions.Add(ldftnInstruction.Previous);
                        removeInstructions.Add(ldftnInstruction.Next.Next);
                        removeInstructions.Add(ldftnInstruction.Next.Next.Next);
                        var prev = ldftnInstruction.Previous.Previous;
                        while (true)
                        {
                            removeInstructions.Add(prev);
                            if (prev.OpCode == OpCodes.Ldsfld)
                                break;
                            prev = prev.Previous;
                        }
                        foreach (var r in removeInstructions)
                            body.Instructions.Remove(r);

                        var localIndex = ldftnInstruction.Next;
                        var mProcessor = body.GetILProcessor();

                        mProcessor.InsertBefore(ldftnInstruction, mProcessor.Create(OpCodes.Newobj, method.Module.ImportReference(constructor)));
                        mProcessor.InsertBefore(ldftnInstruction, mProcessor.Create(OpCodes.Stloc, localVar));

                        /*mProcessor.InsertBefore(ldftnInstruction, mProcessor.Create(OpCodes.Ldloc, localVar));
                        mProcessor.InsertBefore(ldftnInstruction, mProcessor.Create(OpCodes.Ldarg_0));
                        mProcessor.InsertBefore(ldftnInstruction, mProcessor.Create(OpCodes.Stfld, method.Module.ImportReference(thisField)));*/

                        foreach (var pa in addParameters)
                        {
                            mProcessor.InsertBefore(ldftnInstruction, mProcessor.Create(OpCodes.Ldloc, localVar));
                            mProcessor.InsertBefore(ldftnInstruction, pa.Item3);
                            mProcessor.InsertBefore(ldftnInstruction, mProcessor.Create(OpCodes.Stfld, method.Module.ImportReference(fields[pa.Item2])));
                        }
                        mProcessor.InsertBefore(ldftnInstruction, mProcessor.Create(OpCodes.Nop));
                        mProcessor.InsertBefore(ldftnInstruction, mProcessor.Create(OpCodes.Ldloc, localVar));
                        // method in compiler generated class <>c
                    }

                }
            }
        }

        private static string GetNextDisplayClassName(TypeDefinition type)
        {
            var highestNum = -1;
            foreach (var nestedType in type.NestedTypes)
            {
                if (nestedType.Name.StartsWith("<>c__DisplayClass0_"))
                {
                    var num = int.Parse(nestedType.Name.Substring("<>c__DisplayClass0_".Length));
                    if (num > highestNum)
                        highestNum = num;
                }
            }
            return "<>c__DisplayClass7_0";// + (highestNum + 1);
        }

        public static int GetHighestDisplayClassGroup(TypeDefinition type)
        {
            var highestDisplayClass = -1;
            foreach (var nestedType in type.NestedTypes)
            {
                //ParseModType(context, nestedType);
                if (nestedType.Name == "<>c")
                {
                    foreach (var method in nestedType.Methods)
                    {
                        var match = Regex.Match(method.Name, @"\<([^\>]+)\>b__([0-9]+)_([0-9]+)");
                        if (match.Success)
                        {
                            var methodName = int.Parse(match.Groups[1].Value);
                            var group = int.Parse(match.Groups[2].Value);
                            var sub = int.Parse(match.Groups[3].Value);
                            if (group > highestDisplayClass)
                                highestDisplayClass = group;
                        }
                    }
                }
                else if (nestedType.Name.StartsWith("<>c__DisplayClass"))
                {
                    var match = Regex.Match(nestedType.Name, @"\<\>c__DisplayClass([0-9]+)_([0-9]+)");
                    if (match.Success)
                    {
                        var group = int.Parse(match.Groups[1].Value);
                        var sub = int.Parse(match.Groups[2].Value);
                        if (group > highestDisplayClass)
                            highestDisplayClass = group;
                    }
                }
            }
            foreach (var method in type.Methods)
            {
                if (method.Name.StartsWith("<"))
                {
                    var match = Regex.Match(method.Name, @"\<([^\>]+)\>b__([0-9]+)_([0-9]+)");
                    if (match.Success)
                    {
                        var methodName = match.Groups[1].Value;
                        var group = int.Parse(match.Groups[2].Value);
                        var sub = int.Parse(match.Groups[3].Value);
                        if (group > highestDisplayClass)
                            highestDisplayClass = group;
                    }
                }
            }
            return highestDisplayClass;
        }
    }
}
