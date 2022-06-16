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
        }
    }
}
