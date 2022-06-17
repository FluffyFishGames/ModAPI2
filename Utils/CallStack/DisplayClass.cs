using Mono.Cecil;
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
        public class DisplayClass
        {
            public TypeDefinition Type;
            public MethodDefinition Constructor;
            public Dictionary<string, MethodDefinition> Methods = new();
            public Dictionary<string, FieldDefinition> Fields = new();
            public FieldDefinition ThisField;
            public Dictionary<string, FieldDefinition> AddedFields;
            public int HighestSub = -1;

            public DisplayClass()
            {

            }
            public DisplayClass(TypeDefinition type, CallStackCopyContext context = null)
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
                    else if (context != null && context.AddParameters.ContainsKey(type.Fields[i].Name))
                        AddedFields.Add(type.Fields[i].Name, type.Fields[i]);
                }
            }

            public DisplayClass Copy(CallStackCopyContext context)
            {
                var parent = context.Type;
                var module = context.Module;
                var addedFields = new Dictionary<string, FieldDefinition>();

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
                    newClass.Fields.Add(newField);
                    newFields.Add(newField.Name, newField);
                }

                foreach (var param in context.AddParameters)
                {
                    var newField = new FieldDefinition(param.Key, FieldAttributes.Public, module.Import(param.Value));
                    newClass.Fields.Add(newField);
                    newFields.Add(param.Key, newField);
                    addedFields.Add(param.Key, newField);
                }

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
                            context.Methods.Add(newMethod.FullName, (newMethod, newMethod.Body.Instructions.ToArray()));
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
                    AddedFields = addedFields,
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
                                instruction.Operand = method.Value.Module.ImportReference(context.Methods[context.MethodMappings[mref.FullName]].Item1);
                            else if (mref.Name == ".ctor" && context.ClassMappings.ContainsKey(mref.DeclaringType.Name))
                                instruction.Operand = method.Value.Module.ImportReference(context.DisplayClasses[context.ClassMappings[mref.DeclaringType.Name]].Constructor);
                        }
                    }
                }
            }

        }
    }
}
