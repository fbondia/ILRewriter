using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace  ILRewriter
{
    public static class Extensions
    {
        public static Instruction CreateLoadInstruction(this ILProcessor self, object obj)
        {
            if (obj is string)
                return self.Create(OpCodes.Ldstr, obj as string);
            else if (obj is int)
                return self.Create(OpCodes.Ldc_I4, (int)obj);

            throw new NotSupportedException();
        }
    }
    public class ILCodeRewriter
    {
        private readonly string _assemblyPath;
        private readonly AssemblyDefinition _assemblyDefinition;

        private const string _preMethodName = "PreMethod";
        private const string _postMethodName = "PostMethod";
        public ILCodeRewriter(string assemblyPath)
        {
            _assemblyPath = assemblyPath;
            _assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);             
        }

        public void RewriteMethods()
        {
            foreach(var t in _assemblyDefinition.MainModule.Types)
            {
                foreach(var m in t.Methods)
                {
                    foreach (var att in m.CustomAttributes)
                    {
                        ((BaseAssemblyResolver)((Mono.Cecil.MetadataResolver)att.AttributeType.Module.MetadataResolver).AssemblyResolver).AddSearchDirectory(System.IO.Path.GetDirectoryName(_assemblyPath));

                        var preMethod = att.AttributeType.Resolve().Methods.FirstOrDefault(x => x.Name == _preMethodName);
                        var postMethod = att.AttributeType.Resolve().Methods.FirstOrDefault(x => x.Name == _postMethodName);

                        if (preMethod != null)
                        {
                            var constructorReference = _assemblyDefinition.MainModule.Import(preMethod);

                            var ilProcessor = m.Body.GetILProcessor();
                            var firstInstruction = ilProcessor.Body.Instructions[0];
                            var secInstruction = ilProcessor.Body.Instructions[1];

                            if (secInstruction.OpCode == OpCodes.Call)
                            {
                                var mm = secInstruction.Operand as MethodDefinition;
                                if (mm != null)
                                {
                                    if (mm.Name == _preMethodName)
                                    {
                                        ilProcessor.Body.Instructions.RemoveAt(0);
                                        ilProcessor.Body.Instructions.RemoveAt(0);
                                        firstInstruction = ilProcessor.Body.Instructions.First();
                                    }
                                }

                            }

                            ilProcessor.InsertBefore(firstInstruction, ilProcessor.CreateLoadInstruction(m.Name));                         
                            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Call, constructorReference));
                        }
                        if (postMethod != null)
                        {
                            var constructorReference = _assemblyDefinition.MainModule.Import(postMethod);

                            var ilProcessor = m.Body.GetILProcessor();
                            var lastInstruction = ilProcessor.Body.Instructions.Last();
                            var foreLastInstruction = ilProcessor.Body.Instructions[ilProcessor.Body.Instructions.Count - 2];
                            
                            if (foreLastInstruction.OpCode == OpCodes.Call)
                            {
                                var mm = foreLastInstruction.Operand as MethodDefinition;
                                if (mm != null)
                                {
                                    if (mm.Name == _postMethodName)
                                    {
                                        ilProcessor.Body.Instructions.RemoveAt(ilProcessor.Body.Instructions.Count - 2);
                                        ilProcessor.Body.Instructions.RemoveAt(ilProcessor.Body.Instructions.Count - 2);
                                        lastInstruction = ilProcessor.Body.Instructions.Last();
                                    }
                                }
                            }
                            
                            ilProcessor.InsertBefore(lastInstruction, ilProcessor.CreateLoadInstruction(m.Name));
                            ilProcessor.InsertBefore(lastInstruction, ilProcessor.Create(OpCodes.Call, constructorReference));
                        }
                    }                    
                }
            }
        }

        public void Reweave()
        {
            _assemblyDefinition.Write(_assemblyPath);
        }
    }
}
