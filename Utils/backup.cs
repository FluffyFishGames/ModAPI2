/*
 * 
        private static DisplayClass CreateDisplayClassFromDisplayMethods(TypeDefinition type, List<MethodDefinition> original, MethodDefinition baseCall, MonoHelper.Delegate @delegate, ref int highestDisplayClassNum)
        {
            var module = type.Module;

            var corlib = (AssemblyNameReference)module.TypeSystem.CoreLibrary;
            var corlibAssembly = module.AssemblyResolver.Resolve(corlib);
            var objectType = corlibAssembly.MainModule.GetType("System.Object");
            var compilerGeneratedAttribute = corlibAssembly.MainModule.GetType("System.Runtime.CompilerServices.CompilerGeneratedAttribute");
            var compilerGeneratedAttributeConstructor = compilerGeneratedAttribute.Methods.First(m => m.IsConstructor);

            highestDisplayClassNum += 1;
            var newName = "<>c__DisplayClass" + highestDisplayClassNum + "_0";
            var newClass = new TypeDefinition(type.Namespace, "<>c__DisplayClass" + highestDisplayClassNum + "_0", TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Sealed | TypeAttributes.NestedPrivate, module.TypeSystem.Object);
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
            var numField = new FieldDefinition("__ModAPI_chain_num", FieldAttributes.Public, module.TypeSystem.Int32);
            var chainField = new FieldDefinition("__ModAPI_chain_methods", FieldAttributes.Public, @delegate.Type.MakeArrayType());
            newFields.Add("__ModAPI_chain_methods", chainField);
            newFields.Add("__ModAPI_chain_num", numField);
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

            var mapping = new Dictionary<string, string>();
            //var mapping = new Dictionary<string, MethodDefinition>();
            for (var i = 0; i < original.Count; i++)
            {
                var match = Regex.Match(original[i].Name, @"\<[^\>]+\>b__([0-9]+)_([0-9]+)");
                if (match.Success)
                {
                    var sub = int.Parse(match.Groups[2].Value);

                    var newMethod = new MethodDefinition("b__" + match.Groups[2].Value, original[i].Attributes, original[i].ReturnType);
                    original[i].Body.Copy(newMethod.Body);
                    newMethods.Add(newMethod.Name, newMethod);
                    newClass.Methods.Add(newMethod);
                    mapping.Add(original[i].Name, newName);
                }
            }

            foreach (var newMethod in newMethods)
            {
                var body = newMethod.Value.Body;
                body.SimplifyMacros();
                var processor = body.GetILProcessor();
                for (var i = 0; i < body.Instructions.Count; i++)
                {
                    var instruction = body.Instructions[i];
                    if (instruction.OpCode == OpCodes.Ldftn && instruction.Operand is MethodReference _mref && mapping.ContainsKey(_mref.Name))
                    {
                        FieldDefinition actionField = null;
                        var fieldName = _mref.Name.Replace("b__", "9__");
                        bool createField = false;
                        if (newFields.ContainsKey(fieldName))
                            actionField = newFields[fieldName];
                        else
                            createField = true;
                        instruction.Operand = module.ImportReference(newMethods[mapping[_mref.Name]]);
                        processor.Remove(instruction.Previous);
                        processor.InsertBefore(instruction, processor.Create(OpCodes.Ldarg_0));
                        processor.InsertBefore(instruction, processor.Create(OpCodes.Ldarg_0));
                        i++;

                        for (var j = i + 1; j < body.Instructions.Count; j++) // remove everything after newobj up to and including stsfld
                        {
                            var _instruction = body.Instructions[j];
                            if (_instruction.OpCode == OpCodes.Stsfld)
                            {
                                _instruction.OpCode = OpCodes.Stfld;
                                if (createField && _instruction.Operand is FieldReference _fref)
                                {
                                    actionField = new FieldDefinition(fieldName, FieldAttributes.Public, _fref.FieldType);
                                    newFields.Add(fieldName, actionField);
                                    newClass.Fields.Add(actionField);
                                }
                                _instruction.Operand = module.ImportReference(actionField);
                                processor.InsertBefore(_instruction, processor.Create(OpCodes.Ldarg_0));
                                break;
                            }
                        }
                        for (var j = i - 1; j >= 0; j--) // remove everything up to and including ldsfld
                        {
                            var _instruction = body.Instructions[j];
                            if (_instruction.OpCode == OpCodes.Ldsfld)
                            {
                                _instruction.OpCode = OpCodes.Ldfld;
                                _instruction.Operand = module.ImportReference(actionField);
                                processor.InsertBefore(_instruction, processor.Create(OpCodes.Ldarg_0));
                                i++;
                                break;
                            }
                        }
                    }
                }
                body.Optimize();
            }
            type.NestedTypes.Add(newClass);

            var ret = new DisplayClass()
            {
                Constructor = constructor,
                ChainMethodsField = chainField,
                ChainNumField = numField,
                Fields = newFields,
                Methods = newMethods,
                Type = newClass
            };
            return ret;
        }

        private static DisplayClass CreateDisplayClassFromMethods(TypeDefinition type, List<MethodDefinition> original, MethodDefinition baseCall, MonoHelper.Delegate @delegate, ref int highestDisplayClassNum)
        {
            var module = type.Module;

            var corlib = (AssemblyNameReference)module.TypeSystem.CoreLibrary;
            var corlibAssembly = module.AssemblyResolver.Resolve(corlib);
            var objectType = corlibAssembly.MainModule.GetType("System.Object");
            var compilerGeneratedAttribute = corlibAssembly.MainModule.GetType("System.Runtime.CompilerServices.CompilerGeneratedAttribute");
            var compilerGeneratedAttributeConstructor = compilerGeneratedAttribute.Methods.First(m => m.IsConstructor);

            highestDisplayClassNum += 1;
            var newName = "<>c__DisplayClass" + highestDisplayClassNum + "_0";
            var newClass = new TypeDefinition(type.Namespace, "<>c__DisplayClass" + highestDisplayClassNum + "_0", TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.Sealed | TypeAttributes.NestedPrivate, module.TypeSystem.Object);
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
            var chainField = new FieldDefinition("__ModAPI_chain_methods", FieldAttributes.Public, @delegate.Type.MakeArrayType());
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

            var mapping = new Dictionary<string, string>();
            //var mapping = new Dictionary<string, MethodDefinition>();
            for (var i = 0; i < original.Count; i++)
            {
                var match = Regex.Match(original[i].Name, @"\<[^\>]+\>b__([0-9]+)_([0-9]+)");
                if (match.Success)
                {
                    var sub = int.Parse(match.Groups[2].Value);

                    var newMethod = new MethodDefinition("b__" + match.Groups[2].Value, original[i].Attributes, original[i].ReturnType);
                    original[i].Body.Copy(newMethod.Body);
                    newMethods.Add(newMethod.Name, newMethod);
                    newClass.Methods.Add(newMethod);
                    mapping.Add(original[i].Name, newName);
                }
            }

            foreach (var newMethod in newMethods)
            {
                var body = newMethod.Value.Body;
                body.SimplifyMacros();
                var processor = body.GetILProcessor();
                for (var i = 0; i < body.Instructions.Count; i++)
                {
                    var instruction = body.Instructions[i];
                    if (instruction.OpCode == OpCodes.Ldftn && instruction.Operand is MethodReference _mref && mapping.ContainsKey(_mref.Name))
                    {
                        FieldDefinition actionField = null;
                        var fieldName = _mref.Name.Replace("b__", "9__");
                        bool createField = false;
                        if (newFields.ContainsKey(fieldName))
                            actionField = newFields[fieldName];
                        else
                            createField = true;
                        instruction.Operand = module.ImportReference(newMethods[mapping[_mref.Name]]);
                        processor.Remove(instruction.Previous);
                        processor.InsertBefore(instruction, processor.Create(OpCodes.Ldarg_0));
                        processor.InsertBefore(instruction, processor.Create(OpCodes.Ldarg_0));
                        i++;

                        for (var j = i + 1; j < body.Instructions.Count; j++) // remove everything after newobj up to and including stsfld
                        {
                            var _instruction = body.Instructions[j];
                            if (_instruction.OpCode == OpCodes.Stsfld)
                            {
                                _instruction.OpCode = OpCodes.Stfld;
                                if (createField && _instruction.Operand is FieldReference _fref)
                                {
                                    actionField = new FieldDefinition(fieldName, FieldAttributes.Public, _fref.FieldType);
                                    newFields.Add(fieldName, actionField);
                                    newClass.Fields.Add(actionField);
                                }
                                _instruction.Operand = module.ImportReference(actionField);
                                processor.InsertBefore(_instruction, processor.Create(OpCodes.Ldarg_0));
                                break;
                            }
                        }
                        for (var j = i - 1; j >= 0; j--) // remove everything up to and including ldsfld
                        {
                            var _instruction = body.Instructions[j];
                            if (_instruction.OpCode == OpCodes.Ldsfld)
                            {
                                _instruction.OpCode = OpCodes.Ldfld;
                                _instruction.Operand = module.ImportReference(actionField);
                                processor.InsertBefore(_instruction, processor.Create(OpCodes.Ldarg_0));
                                i++;
                                break;
                            }
                        }
                    }
                }
                body.Optimize();
            }
            type.NestedTypes.Add(newClass);

            var ret = new DisplayClass()
            {
                Constructor = constructor,
                ChainMethodsField = chainField,
                ChainNumField = numField,
                Fields = newFields,
                Methods = newMethods,
                Type = newClass
            };
            return ret;
        }*/

/*
while (inst != null)
{

        if (_instruction.OpCode == OpCodes.Stsfld)
        {
            _instruction.OpCode = OpCodes.Stfld;
            if (createField && _instruction.Operand is FieldReference _fref)
            {
                actionField = new FieldDefinition(fieldName, FieldAttributes.Public, _fref.FieldType);
                newFields.Add(fieldName, actionField);
                newClass.Fields.Add(actionField);
            }
            _instruction.Operand = module.ImportReference(actionField);
            processor.InsertBefore(_instruction, processor.Create(OpCodes.Ldarg_0));
            break;
        }
    }
    inst = inst.Previous;
}
/*
foreach (var newMethod in newMethods)
{
    var body = newMethod.Value.Body;
    body.SimplifyMacros();
    var processor = body.GetILProcessor();
    for (var i = 0; i < body.Instructions.Count; i++)
    {
        var instruction = body.Instructions[i];
        if (instruction.OpCode == OpCodes.Ldftn && instruction.Operand is MethodReference _mref && mapping.ContainsKey(_mref.Name))
        {
            FieldDefinition actionField = null;
            var fieldName = _mref.Name.Replace("b__", "9__");
            bool createField = false;
            if (newFields.ContainsKey(fieldName))
                actionField = newFields[fieldName];
            else
                createField = true;
            instruction.Operand = module.ImportReference(newMethods[mapping[_mref.Name]]);
            processor.Remove(instruction.Previous);
            processor.InsertBefore(instruction, processor.Create(OpCodes.Ldarg_0));
            processor.InsertBefore(instruction, processor.Create(OpCodes.Ldarg_0));
            i++;

            for (var j = i + 1; j < body.Instructions.Count; j++) // remove everything after newobj up to and including stsfld
            {
                var _instruction = body.Instructions[j];
                if (_instruction.OpCode == OpCodes.Stsfld)
                {
                    _instruction.OpCode = OpCodes.Stfld;
                    if (createField && _instruction.Operand is FieldReference _fref)
                    {
                        actionField = new FieldDefinition(fieldName, FieldAttributes.Public, _fref.FieldType);
                        newFields.Add(fieldName, actionField);
                        newClass.Fields.Add(actionField);
                    }
                    _instruction.Operand = module.ImportReference(actionField);
                    processor.InsertBefore(_instruction, processor.Create(OpCodes.Ldarg_0));
                    break;
                }
            }
            for (var j = i - 1; j >= 0; j--) // remove everything up to and including ldsfld
            {
                var _instruction = body.Instructions[j];
                if (_instruction.OpCode == OpCodes.Ldsfld)
                {
                    _instruction.OpCode = OpCodes.Ldfld;
                    _instruction.Operand = module.ImportReference(actionField);
                    processor.InsertBefore(_instruction, processor.Create(OpCodes.Ldarg_0));
                    i++;
                    break;
                }
            }
        }
    }
    body.Optimize();
}
type.NestedTypes.Add(newClass);
*/