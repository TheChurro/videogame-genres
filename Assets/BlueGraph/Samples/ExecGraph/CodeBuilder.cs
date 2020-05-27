﻿
using System;
using System.Collections.Generic;
using System.Text;
using BlueGraph;
using UnityEngine;

namespace BlueGraphExamples.ExecGraph
{
    /// <summary>
    /// An experiment in compiling nodes into a high performance / AOT-supported function.
    /// 
    /// Supports graphs composed entirely of FuncNodes or nodes that implement ICanCompile
    /// </summary>
    public class CodeBuilder
    {
        /// <summary>
        /// Number of spaces to indent scopes
        /// </summary>
        public int indent = 4;

        /// <summary>
        /// Generated class name
        /// </summary>
        public string className;

        StringBuilder m_Builder = new StringBuilder();
        readonly Scope m_Root;
        Scope m_CurrentScope;

        /// <summary>
        /// Namespaces hoisted out of FuncNode calls
        /// </summary>
        HashSet<string> m_Namespaces = new HashSet<string>();

        public class Scope
        {
            public int depth = 1;
            public Scope parent;

            // Nodes executed in this scope
            public List<AbstractNode> nodes = new List<AbstractNode>();

            // Variables with a `const` keyword in scope. Used to determine
            // if other operations within scope should also be constant.
            public HashSet<string> constants = new HashSet<string>();
            
            public Scope(Scope parent = null)
            {
                this.parent = parent;

                if (parent != null)
                {
                    depth = parent.depth + 1;
                }
            }

            /// <summary>
            /// Was the input node executed in this scope or a parent scope
            /// </summary>
            public bool AlreadyExecutedInScope(AbstractNode node)
            {
                if (nodes.Contains(node)) return true;
                if (parent != null) return parent.AlreadyExecutedInScope(node);
                return false;
            }

            public bool IsConstInScope(string varName)
            {
                if (constants.Contains(varName)) return true;
                if (parent != null) return parent.IsConstInScope(varName);
                return false;
            }
        }

        public CodeBuilder()
        {
            m_Root = new Scope();
            m_CurrentScope = m_Root;
        }

        /// <summary>
        /// Start a new scope for code injection. Variable declarations and function calls
        /// will only occur within this scope when calling Append/AppendLine. 
        /// 
        /// Opening brace is automatically added when entering a new scope.
        /// </summary>
        public void BeginScope()
        {
            AppendLine("{");
            m_CurrentScope = new Scope(m_CurrentScope);
            AppendLine("// Scope: " + m_CurrentScope.GetHashCode());
        }

        /// <summary>
        /// End a scope, returning to the parent scope. Closing brace is automatically added.
        /// </summary>
        public void EndScope()
        {
            m_CurrentScope = m_CurrentScope.parent;
            AppendLine("}");
        }

        /// <summary>
        /// Append code without creating a new line or automatic indenting
        /// </summary>
        /// <param name="value"></param>
        public void Append(string value)
        {
            m_Builder.Append(value);
        }

        /// <summary>
        /// Add a blank line
        /// </summary>
        public void AppendLine()
        {
            m_Builder.AppendLine(new string(' ', m_CurrentScope.depth * indent));
        }

        /// <summary>
        /// Add a new line of code, automatically indenting to the current scope
        /// </summary>
        public void AppendLine(string line)
        {
            m_Builder.AppendLine(new string(' ', m_CurrentScope.depth * indent) + line);
        }
        
        /// <summary>
        /// Convert a NodePort to a unique C# variable name 
        /// </summary>
        public string PortToVariableName(Port port)
        {
            // TODO: Drastically improve this 
            return $"{port.name}_{port.node.id.Substring(0, 8)}";
        }

        /// <summary>
        /// Representation of some assignable value
        /// </summary>
        /// <remarks>
        /// Stringified form can represent either:
        /// - Variable name referencing a stored value
        /// - Constant value type (numbers, strings, booleans)
        /// - Stringified object constructor (new Foo(...))
        /// </remarks>
        public struct Assignable
        {
            public string value;

            /// <summary>
            /// Can this value be treated as const to the IL.
            /// This excludes class instances, structs, etc. 
            /// </summary>
            public bool isConst;

            public Assignable(string value, bool isConst = false)
            {
                this.value = value;
                this.isConst = isConst;
            }

            public override string ToString() => value;
        }

        /// <summary>
        /// Get the value to be inserted into the given port. This can either
        /// be a reference to a variable from an output port, or a constant value.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public Assignable PortToValue(Port port, object defaultValue)
        {
            if (port.IsConnected)
            {
                // TODO: Support for multiple inputs to this port

                Port outputPort = port.ConnectedPorts[0];
                CompileInputs(outputPort);

                string varName = PortToVariableName(outputPort);
                return new Assignable(varName, IsConstInScope(varName));
            }

            //  Otherwise, inline default value.
            return GetAssignable(defaultValue);
        }

        public bool IsConstInScope(string varName)
        {
            return m_CurrentScope.IsConstInScope(varName);
        }

        public void AddConstToScope(string varName)
        {
            m_CurrentScope.constants.Add(varName);
        }

        /// <summary>
        /// Convert any (within reason) value to a printable constant
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Assignable GetAssignable(object value)
        {
            if (value == null)
            {
                return new Assignable("null");
            }
            
            // This is where things get dicey. 
            // TODO: Figure out a smarter way to deal with 
            // structs so they're not all hand crafted here.
            switch (value)
            {
                case int i: return new Assignable(i.ToString(), true);
                case bool b: return new Assignable(b.ToString(), true);
                case float f: return new Assignable($"{f}f", true);
                case string s: return new Assignable($"\"{s}\"", true);
                case Vector2 v: return  new Assignable($"new Vector2({v.x}f, {v.y}f)");
                case Vector3 v: return  new Assignable($"new Vector3({v.x}f, {v.y}f, {v.z}f)");
                case Vector4 v: return  new Assignable($"new Vector4({v.x}f, {v.y}f, {v.z}f, {v.w}f)");
                case Quaternion q: return  new Assignable($"new Quaternion({q.x}f, {q.y}f, {q.z}f, {q.w}f)");
                case Color c: return  new Assignable($"new Color({c.r}f, {c.g}f, {c.b}f, {c.a}f)");
            }
            
            Type type = value.GetType();
            if (type.IsClass)
            {
                return new Assignable($"new {HoistNamespace(type.FullName)}()");
            }

            // TODO: No clue how to deal with things like AnimationCurves here.
            // Need to reference instances from ... somewhere.

            // Same deal for Prefab GOs. We'll need a separate solution here to
            // reference them. (Maybe somehow deep link into the source graph's SO?)

            throw new Exception($"Cannot create constant form of type `{type}`");
        }

        /// <summary>
        /// Convert default(type) to a printable constant
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Assignable DefaultValue(Type type) 
        {
            if (type.IsValueType)
            {
                return GetAssignable(Activator.CreateInstance(type));
            }

            return GetAssignable(null);
        }

        /// <summary>
        /// Walk up the input tree and add node results. 
        /// </summary>
        /// <param name="outputPort"></param>
        public void CompileInputs(Port outputPort)
        {
            // For now, just deal with singular ports.
            CompileNode(outputPort.node);
        }

        /// <summary>
        /// Has the given node already had code generation happen in / above current scope.
        /// </summary>
        /// <param name="node"></param>
        public bool AlreadyInScope(AbstractNode node)
        {
            return m_CurrentScope.AlreadyExecutedInScope(node);
        }

        public void CompileNode(AbstractNode node)
        {
            // Make sure we don't try to compile the same 
            // node twice to the same scope or higher
            if (AlreadyInScope(node)) 
            {
                AppendLine($"// Already compiled {node.name} ({node.id.Substring(0, 8)}) in scope");
                return;
            }
                
            m_CurrentScope.nodes.Add(node);
            
            if (node is FuncNode funcNode)
            {
                CompileFuncNode(funcNode);
            }
            else if (node is ICanCompile compilableNode)
            {
                compilableNode.Compile(this);
            }
        }

        protected void CompileFuncNode(FuncNode node)
        {
            string className = HoistNamespace(node.className);
            string methodName = node.methodName;
            string returnValue = "";
            
            // Declare outputs / get inputs
            List<string> args = new List<string>();
            for (int i = 0; i < node.ports.Count; i++) 
            {
                Port port = node.ports[i];
                string variableName = PortToVariableName(port);

                // Handle return value separately, since it's not an argument
                if (node.hasReturnValue && i == node.ports.Count - 1)
                {
                    returnValue = $"{HoistNamespace(port.type)} {variableName} = ";
                    continue;
                }

                // Declare out variables
                if (!port.isInput)
                {
                    AppendLine($"{HoistNamespace(port.type)} {variableName};");
                    args.Add($"out {variableName}");
                }
                else
                {
                    if (port.IsConnected)
                    {
                        Port outputPort = port.ConnectedPorts[0];
                        CompileInputs(outputPort);
                        args.Add(PortToVariableName(outputPort));
                    }
                    else
                    {
                        // Constant default (actual inline editables / non default() not yet supported)
                        args.Add(DefaultValue(port.type).ToString());
                    }
                }
            }
            

            // Execute the underlying function
            AppendLine($"{returnValue}{className}.{methodName}({string.Join(", ", args)});");
        }
        
        /// <summary>
        /// Hoist the namespace out of the class name and into global `using` statements
        /// </summary>
        public string HoistNamespace(string className)
        {
            return HoistNamespace(Type.GetType(className));
        }

        public string HoistNamespace(Type type)
        {
            string ns = type.Namespace;
            m_Namespaces.Add(ns);

            return type.FullName.Substring(ns.Length + 1);
        }

        /// <summary>
        /// Compile a ready to use static class for this node. 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder code = new StringBuilder();
            
            code.AppendLine("// Autogenerated - Yadda yadda");
            foreach (var ns in m_Namespaces)
            {
                code.AppendLine($"using {ns};");
            }

            code.AppendLine();
            code.AppendLine("public static class GraphAOT {");
            code.Append(m_Builder);
            code.AppendLine("}");
            
            return code.ToString();
        }
    }
}
