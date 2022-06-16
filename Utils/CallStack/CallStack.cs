using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModAPI.Utils
{
    internal partial class CallStack
    {
        public static List<Node> FindCallsTo(MethodDefinition method, MethodReference callTo)
        {
            var ret = new List<Node>();
            __FindCallsTo(method, callTo, null, ret);
            for (var i = 0; i < ret.Count; i++)
            {
                var r = ret[i];
                r.Clean();
                if (!r.ContainsCall())
                {
                    ret.RemoveAt(i);
                    i--;
                }
            }
            return ret;
        }


        private static void __FindCallsTo(MethodDefinition method, MethodReference baseMethod, Node parent, List<Node> ret)
        {
            var module = method.Module;
            var body = method.Body;
            if (body == null) return;
            for (var i = 0; i < body.Instructions.Count; i++)
            {
                var instruction = body.Instructions[i];
                if (instruction.Operand is MethodReference mref)
                {
                    if (instruction.OpCode == OpCodes.Call && mref.FullName == baseMethod.FullName)
                    {
                        var node = new Node()
                        {
                            Method = method,
                            Instruction = instruction,
                            Parent = parent,
                            Type = Node.NodeType.Call
                        };
                        if (parent == null)
                            ret.Add(node);
                        else
                            parent.Children.Add(node);
                    }
                    else if ((instruction.OpCode == OpCodes.Ldftn || instruction.OpCode == OpCodes.Call) && mref.Module.Name == module.Name)
                    {
                        var methodDef = mref.Resolve();
                        var node = new Node()
                        {
                            Method = method,
                            CalledMethod = methodDef,
                            Instruction = instruction,
                            Parent = parent,
                            Type = Node.NodeType.Method
                        };
                        if (parent == null)
                            ret.Add(node);
                        else
                            parent.Children.Add(node);
                        __FindCallsTo(methodDef, baseMethod, node, ret);
                    }
                }
            }
        }
    }
}
