using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ILRewriter
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

        private const string _procMethodName = "Process";

        public ILCodeRewriter(string assemblyPath)
        {
            _assemblyPath = assemblyPath;
            _assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);
        }

        public void RewriteMethods()
        {
            foreach (var module in _assemblyDefinition.Modules)
            {
                foreach (var type in module.Types)
                {
                    foreach (var meth in type.Methods)
                    {
                        if (!meth.HasCustomAttributes)
                        {
                            continue;
                        }

                        var ilProcessor = meth.Body.GetILProcessor();
                        var firstUserInstruction = ilProcessor.Body.Instructions.First();
                        
                        foreach (var att in meth.CustomAttributes)
                        {
                            ((BaseAssemblyResolver)((MetadataResolver)att.AttributeType.Module.MetadataResolver).AssemblyResolver).AddSearchDirectory(System.IO.Path.GetDirectoryName(_assemblyPath));
                        }


                        int argCount = 0;
                        foreach (var att in meth.CustomAttributes)
                        {                           
                            meth.Body.InitLocals = true;
                            meth.Body.Variables.Add(new VariableDefinition(att.AttributeType));

                            ilProcessor.InsertBefore(firstUserInstruction, ilProcessor.Create(OpCodes.Newobj, meth.Module.ImportReference(att.Constructor)));

                            ilProcessor.InsertBefore(firstUserInstruction, ilProcessor.Create(OpCodes.Stloc, argCount));
                            argCount++;
                        }
                        argCount = 0;
                        foreach(var att in meth.CustomAttributes)
                        {
                            var preMethod = att.AttributeType.Resolve().Methods.FirstOrDefault(x => x.Name == _preMethodName);
                            if (preMethod != null)
                            {
                                ilProcessor.InsertBefore(firstUserInstruction, ilProcessor.Create(OpCodes.Ldloc, argCount));
                                argCount++;
                                AddInterceptCall(ilProcessor, meth, preMethod, att, firstUserInstruction);
                            }
                        }

                        int currPara = 0;
                        foreach (var para in meth.Parameters)
                        {
                            foreach (var att in para.CustomAttributes)
                            {
                                ((BaseAssemblyResolver)((MetadataResolver)att.AttributeType.Module.MetadataResolver).AssemblyResolver).AddSearchDirectory(System.IO.Path.GetDirectoryName(_assemblyPath));

                                var processMeth = att.AttributeType.Resolve().Methods.FirstOrDefault(x => x.Name == _procMethodName);
                                if (processMeth != null)
                                {
                                    ilProcessor.InsertBefore(firstUserInstruction, ilProcessor.CreateLoadInstruction(meth.Name));
                                    ilProcessor.InsertBefore(firstUserInstruction, ilProcessor.CreateLoadInstruction(para.Name));
                                    
                                    ilProcessor.InsertBefore(firstUserInstruction, ilProcessor.Create(OpCodes.Ldarg, currPara));

                                    ilProcessor.InsertBefore(firstUserInstruction, ilProcessor.Create(OpCodes.Call, meth.Module.ImportReference(processMeth)));
                                }
                            }
                            currPara++;
                        }




                        var retInstruction = FixReturns(meth);
                        
                        var firstInstruction = meth.Body.Instructions[2 * meth.CustomAttributes.Count];
                        
                        var beforeReturn = Instruction.Create(OpCodes.Nop);
                        ilProcessor.InsertBefore(retInstruction, beforeReturn);

                        argCount = 0;
                        foreach (var att in meth.CustomAttributes)
                       {
                           var postMethod = att.AttributeType.Resolve().Methods.FirstOrDefault(x => x.Name == _postMethodName);
                           if (postMethod != null)
                           {
                               ilProcessor.InsertBefore(retInstruction, ilProcessor.Create(OpCodes.Ldloc, argCount));
                                argCount++;
                               AddInterceptCall(ilProcessor, meth, postMethod, att, retInstruction);
                           }
                       }

                        ilProcessor.InsertBefore(retInstruction, Instruction.Create(OpCodes.Endfinally));
                        
                        var handler = new ExceptionHandler(ExceptionHandlerType.Finally)
                        {
                            TryStart = firstInstruction,
                            TryEnd = beforeReturn,
                            HandlerStart = beforeReturn,
                            HandlerEnd = retInstruction,
                        };
                        
                        meth.Body.ExceptionHandlers.Add(handler);
                        meth.Body.InitLocals = true;





                        
                    }
                }
            }
        }

        Instruction FixReturns(MethodDefinition Method)
        {
            if (Method.ReturnType == Method.Module.TypeSystem.Void)
            {
                var instructions = Method.Body.Instructions;
                var lastRet = Instruction.Create(OpCodes.Ret);
                instructions.Add(lastRet);

                for (var index = 0; index < Method.Body.Instructions.Count - 1; index++)
                {
                    var instruction = instructions[index];
                    if (instruction.OpCode == OpCodes.Ret)
                    {
                        instructions[index] = Instruction.Create(OpCodes.Leave, lastRet);
                    }
                }
                return lastRet;
            }
            else
            {
                var instructions = Method.Body.Instructions;
                var returnVariable = new VariableDefinition("methodTimerReturn", Method.ReturnType);
                Method.Body.Variables.Add(returnVariable);
                var lastLd = Instruction.Create(OpCodes.Ldloc, returnVariable);
                instructions.Add(lastLd);
                instructions.Add(Instruction.Create(OpCodes.Ret));

                for (var index = 0; index < instructions.Count - 2; index++)
                {
                    var instruction = instructions[index];
                    if (instruction.OpCode == OpCodes.Ret)
                    {
                        instructions[index] = Instruction.Create(OpCodes.Leave, lastLd);
                        instructions.Insert(index, Instruction.Create(OpCodes.Stloc, returnVariable));
                        index++;
                    }
                }
                return lastLd;
            }
        }

        private void CreateAttrObjectInMethod(ILProcessor ilProcessor, Instruction firstInstruction, MethodDefinition methDef, CustomAttribute att)
        {
            methDef.Body.InitLocals = true;

            methDef.Body.Variables.Add(new VariableDefinition(att.AttributeType));

            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Newobj, methDef.Module.ImportReference(att.Constructor)));

        }

        private Instruction AddInterceptCall(ILProcessor ilProcessor, MethodDefinition methDef, MethodDefinition interceptMethDef, CustomAttribute att, Instruction insertBefore)
        {
            var methRef = _assemblyDefinition.MainModule.ImportReference(interceptMethDef);

            ilProcessor.InsertBefore(insertBefore, ilProcessor.CreateLoadInstruction(methDef.Name));

            int methodParamCount = methDef.Parameters.Count;
            int arrayVarNr = methDef.Body.Variables.Count;

            if (methodParamCount > 0)
            {
                ArrayType objArrType = new ArrayType(_assemblyDefinition.MainModule.TypeSystem.Object);
                methDef.Body.Variables.Add(new VariableDefinition(objArrType));

                methDef.Body.InitLocals = true;

                ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldc_I4, methodParamCount));
                ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Newarr, _assemblyDefinition.MainModule.TypeSystem.Object));
                ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Stloc, arrayVarNr));

                bool pointerToValueTypeVariable;
                TypeSpecification referencedTypeSpec = null;

                for (int i = 0; i < methodParamCount; i++)
                {
                    var paramMetaData = methDef.Parameters[i].ParameterType.MetadataType;
                    if (paramMetaData == MetadataType.UIntPtr || paramMetaData == MetadataType.FunctionPointer ||
                        paramMetaData == MetadataType.IntPtr || paramMetaData == MetadataType.Pointer)
                    {
                        break;
                    }

                    ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldloc, arrayVarNr));
                    ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldc_I4, i));

                    if (methDef.IsStatic)
                    {
                        ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldarg, i));
                    }
                    else
                    {
                        ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldarg, i + 1));
                    }

                    pointerToValueTypeVariable = false;

                    TypeReference paramType = methDef.Parameters[i].ParameterType;
                    if (paramType.IsByReference)
                    {
                        referencedTypeSpec = paramType as TypeSpecification;

                        if (referencedTypeSpec != null)
                        {
                            switch (referencedTypeSpec.ElementType.MetadataType)
                            {
                                case MetadataType.Boolean:
                                case MetadataType.SByte:
                                    ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldind_I1));
                                    pointerToValueTypeVariable = true;
                                    break;

                                case MetadataType.Int16:
                                    ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldind_I2));
                                    pointerToValueTypeVariable = true;
                                    break;

                                case MetadataType.Int32:
                                    ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldind_I4));
                                    pointerToValueTypeVariable = true;
                                    break;

                                case MetadataType.Int64:
                                case MetadataType.UInt64:
                                    ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldind_I8));
                                    pointerToValueTypeVariable = true;
                                    break;

                                case MetadataType.Byte:
                                    ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldind_U1));
                                    pointerToValueTypeVariable = true;
                                    break;

                                case MetadataType.UInt16:
                                case MetadataType.Char:
                                    ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldind_U2));
                                    pointerToValueTypeVariable = true;
                                    break;

                                case MetadataType.UInt32:
                                    ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldind_U4));
                                    pointerToValueTypeVariable = true;
                                    break;

                                case MetadataType.Single:
                                    ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldind_R4));
                                    pointerToValueTypeVariable = true;
                                    break;

                                case MetadataType.Double:
                                    ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldind_R8));
                                    pointerToValueTypeVariable = true;
                                    break;

                                case MetadataType.IntPtr:
                                case MetadataType.UIntPtr:
                                    ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldind_I));
                                    pointerToValueTypeVariable = true;
                                    break;

                                default:
                                    if (referencedTypeSpec.ElementType.IsValueType)
                                    {
                                        ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldobj, referencedTypeSpec.ElementType));
                                        pointerToValueTypeVariable = true;
                                    }
                                    else
                                    {
                                        ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldind_Ref));
                                        pointerToValueTypeVariable = false;
                                    }
                                    break;
                            }
                        }
                        else
                        {

                        }
                    }

                    if (paramType.IsValueType || pointerToValueTypeVariable)
                    {
                        if (pointerToValueTypeVariable)
                        {
                            ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Box, referencedTypeSpec.ElementType));
                        }
                        else
                        {
                            ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Box, paramType));
                        }
                    }
                    ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Stelem_Ref));
                }


                ilProcessor.InsertBefore(insertBefore, ilProcessor.Create(OpCodes.Ldloc, arrayVarNr));
            }
            var ins = ilProcessor.Create(OpCodes.Callvirt, methDef.Module.ImportReference(interceptMethDef));
            ilProcessor.InsertBefore(insertBefore, ins);
            return ins;
        }

        public void Reweave()
        {
            _assemblyDefinition.Write(_assemblyPath);
        }
    }
}
