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
