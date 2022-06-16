using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI.Utils
{
    public static class ILProcessorExtension
    {
        public static Instruction WalkBack(this ILProcessor processor, Instruction instruction)
        {
            if (instruction.Operand is MethodReference methodRef)
            {
                var stackSize = methodRef.Parameters.Count + (methodRef.HasThis ? 1 : 0);
                var curr = instruction;
                while (stackSize > 0)
                {
                    curr = curr.Previous;
                    if (curr == null)
                        throw new Exception("Something went wrong in method stack.");

                    stackSize -= MonoHelper.GetStackChange(curr);
                    if (stackSize == 0) // we found the beginning of the call
                    {
                        return curr;
                    }
                }
            }
            return null;
        }
        public static void AppendAssignStandardValue(this ILProcessor processor, VariableDefinition variable, TypeReference type)
        {
            if (type.FullName == "System.Boolean" || type.FullName == "System.UInt8" || type.FullName == "System.Int8" || type.FullName == "System.UInt16" || type.FullName == "System.Int16" || type.FullName == "System.UInt32" || type.FullName == "System.Int32")
            {
                processor.Append(processor.Create(OpCodes.Ldc_I4_0));
                processor.Append(processor.Create(OpCodes.Stloc, variable));
            }
            else if (type.FullName == "System.UInt64" || type.FullName == "System.Int64")
            {
                processor.Append(processor.Create(OpCodes.Ldc_I4_0));
                processor.Append(processor.Create(OpCodes.Conv_I8));
                processor.Append(processor.Create(OpCodes.Stloc, variable));
            }
            else if (type.FullName == "System.Single")
            {
                processor.Append(processor.Create(OpCodes.Ldc_R4, 0.0f));
                processor.Append(processor.Create(OpCodes.Stloc, variable));
            }
            else if (type.FullName == "System.Double")
            {
                processor.Append(processor.Create(OpCodes.Ldc_R8, 0));
                processor.Append(processor.Create(OpCodes.Stloc, variable));
            }
            else if (type.IsValueType || type.IsPrimitive)
            {
                processor.Append(processor.Create(OpCodes.Ldloca_S, variable));
                processor.Append(processor.Create(OpCodes.Initobj, type));
            }
            else
            {
                processor.Append(processor.Create(OpCodes.Ldnull));
                processor.Append(processor.Create(OpCodes.Stloc, variable));
            }
        }
        public static void InsertBeforeAssignStandardValue(this ILProcessor processor, Instruction before, VariableDefinition variable, TypeReference type)
        {
            if (type.FullName == "System.Boolean" || type.FullName == "System.UInt8" || type.FullName == "System.Int8" || type.FullName == "System.UInt16" || type.FullName == "System.Int16" || type.FullName == "System.UInt32" || type.FullName == "System.Int32")
            {
                processor.InsertBefore(before, processor.Create(OpCodes.Ldc_I4_0));
                processor.InsertBefore(before, processor.Create(OpCodes.Stloc, variable));
            }
            else if (type.FullName == "System.UInt64" || type.FullName == "System.Int64")
            {
                processor.InsertBefore(before, processor.Create(OpCodes.Ldc_I4_0));
                processor.InsertBefore(before, processor.Create(OpCodes.Conv_I8));
                processor.InsertBefore(before, processor.Create(OpCodes.Stloc, variable));
            }
            else if (type.FullName == "System.Single")
            {
                processor.InsertBefore(before, processor.Create(OpCodes.Ldc_R4, 0.0f));
                processor.InsertBefore(before, processor.Create(OpCodes.Stloc, variable));
            }
            else if (type.FullName == "System.Double")
            {
                processor.InsertBefore(before, processor.Create(OpCodes.Ldc_R8, 0));
                processor.InsertBefore(before, processor.Create(OpCodes.Stloc, variable));
            }
            else if (type.IsValueType || type.IsPrimitive)
            {
                processor.InsertBefore(before, processor.Create(OpCodes.Ldloca_S, variable));
                processor.InsertBefore(before, processor.Create(OpCodes.Initobj, type));
            }
            else
            {
                processor.InsertBefore(before, processor.Create(OpCodes.Ldnull));
                processor.InsertBefore(before, processor.Create(OpCodes.Stloc, variable));
            }
        }
        public static void InsertAfterAssignStandardValue(this ILProcessor processor, Instruction after, VariableDefinition variable, TypeReference type)
        {
            if (type.FullName == "System.Boolean" || type.FullName == "System.UInt8" || type.FullName == "System.Int8" || type.FullName == "System.UInt16" || type.FullName == "System.Int16" || type.FullName == "System.UInt32" || type.FullName == "System.Int32")
            {
                processor.InsertAfter(after, processor.Create(OpCodes.Stloc, variable));
                processor.InsertAfter(after, processor.Create(OpCodes.Ldc_I4_0));
            }
            else if (type.FullName == "System.UInt64" || type.FullName == "System.Int64")
            {
                processor.InsertAfter(after, processor.Create(OpCodes.Stloc, variable));
                processor.InsertAfter(after, processor.Create(OpCodes.Conv_I8));
                processor.InsertAfter(after, processor.Create(OpCodes.Ldc_I4_0));
            }
            else if (type.FullName == "System.Single")
            {
                processor.InsertAfter(after, processor.Create(OpCodes.Stloc, variable));
                processor.InsertAfter(after, processor.Create(OpCodes.Ldc_R4, 0.0f));
            }
            else if (type.FullName == "System.Double")
            {
                processor.InsertAfter(after, processor.Create(OpCodes.Stloc, variable));
                processor.InsertAfter(after, processor.Create(OpCodes.Ldc_R8, 0));
            }
            else if (type.IsValueType || type.IsPrimitive)
            {
                processor.InsertAfter(after, processor.Create(OpCodes.Initobj, type));
                processor.InsertAfter(after, processor.Create(OpCodes.Ldloca_S, variable));
            }
            else
            {
                processor.InsertAfter(after, processor.Create(OpCodes.Stloc, variable));
                processor.InsertAfter(after, processor.Create(OpCodes.Ldnull));
            }
        }
    }
}
