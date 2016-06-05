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
        private const string _exceptionMethodName = "ExceptionMethod";

        private const string _procMethodName = "Process";

        private const string _getMethodName = "Get";
        private const string _setMethodName = "Set";

        public ILCodeRewriter(string assemblyPath)
        {
            _assemblyPath = assemblyPath;
            _assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);
        }

        public void RewriteProperties()
        {
            foreach (var module in _assemblyDefinition.Modules)
            {
                foreach (var type in module.Types)
                {
                    foreach (var property in type.Properties)
                    {
                        if (!property.HasCustomAttributes)
                        {
                            continue;
                        }

                        var currentMethod = property.SetMethod;
                        var ilProcessor = currentMethod.Body.GetILProcessor();
                        var firstUserInstruction = ilProcessor.Body.Instructions.First();
                        var returnInstruction = ilProcessor.Body.Instructions.Last();

                        foreach (var att in property.CustomAttributes)
                        {
                            ((BaseAssemblyResolver)((MetadataResolver)att.AttributeType.Module.MetadataResolver).AssemblyResolver).AddSearchDirectory(System.IO.Path.GetDirectoryName(_assemblyPath));
                        }

                        foreach (var att in property.CustomAttributes)
                        {
                            var setMethod = att.AttributeType.Resolve().Methods.FirstOrDefault(x => x.Name == _setMethodName);
                            if (setMethod != null)
                            {
                                ilProcessor.InsertBefore(firstUserInstruction, ilProcessor.CreateLoadInstruction(property.Name));
                            
                                ilProcessor.InsertBefore(firstUserInstruction, ilProcessor.Create(OpCodes.Ldarg, 1));
                        
                                ilProcessor.InsertBefore(firstUserInstruction, ilProcessor.Create(OpCodes.Call, currentMethod.Module.ImportReference(setMethod)));
                        
                            }
                        }

                        currentMethod = property.GetMethod;
                        ilProcessor = currentMethod.Body.GetILProcessor();
                        firstUserInstruction = ilProcessor.Body.Instructions.First();
                        returnInstruction = ilProcessor.Body.Instructions.Last();

                        
                        foreach (var att in property.CustomAttributes)
                        {
                            var getMethod = att.AttributeType.Resolve().Methods.FirstOrDefault(x => x.Name == _getMethodName);
                            if (getMethod != null)
                            {
                                ilProcessor.InsertBefore(returnInstruction, ilProcessor.CreateLoadInstruction(property.Name));

                                ilProcessor.InsertBefore(returnInstruction, ilProcessor.Create(OpCodes.Ldloc, 0));

                                ilProcessor.InsertBefore(returnInstruction, ilProcessor.Create(OpCodes.Call, currentMethod.Module.ImportReference(getMethod)));

                            }
                        }

                    }
                }
            }
        }

        public void RewriteMethods()
        {
            foreach (var module in _assemblyDefinition.Modules)
            {
                foreach (var type in module.Types)
                {
                    foreach (var currentMethod in type.Methods)
                    {
                        if (!currentMethod.HasCustomAttributes)
                        {
                            continue;
                        }

                        var ilProcessor = currentMethod.Body.GetILProcessor();
                        var firstUserInstruction = ilProcessor.Body.Instructions.First();
                        
                        foreach (var att in currentMethod.CustomAttributes)
                        {
                            ((BaseAssemblyResolver)((MetadataResolver)att.AttributeType.Module.MetadataResolver).AssemblyResolver).AddSearchDirectory(System.IO.Path.GetDirectoryName(_assemblyPath));
                        }


                        int currentAttribute = 0;
                        foreach (var att in currentMethod.CustomAttributes)
                        {                           
                            currentMethod.Body.InitLocals = true;
                            currentMethod.Body.Variables.Add(new VariableDefinition(att.AttributeType));

                            ilProcessor.InsertBefore(firstUserInstruction, ilProcessor.Create(OpCodes.Newobj, currentMethod.Module.ImportReference(att.Constructor)));

                            ilProcessor.InsertBefore(firstUserInstruction, ilProcessor.Create(OpCodes.Stloc, currentAttribute));
                            currentAttribute++;
                        }
                        currentAttribute = 0;
                        foreach(var att in currentMethod.CustomAttributes)
                        {
                            var preMethod = att.AttributeType.Resolve().Methods.FirstOrDefault(x => x.Name == _preMethodName);
                            if (preMethod != null)
                            {
                                ilProcessor.InsertBefore(firstUserInstruction, ilProcessor.Create(OpCodes.Ldloc, currentAttribute));
                                currentAttribute++;
                                AddInterceptCall(ilProcessor, currentMethod, preMethod, att, firstUserInstruction);
                            }
                        }

                        int currentParameter = 0;
                        foreach (var para in currentMethod.Parameters)
                        {
                            foreach (var att in para.CustomAttributes)
                            {
                                ((BaseAssemblyResolver)((MetadataResolver)att.AttributeType.Module.MetadataResolver).AssemblyResolver).AddSearchDirectory(System.IO.Path.GetDirectoryName(_assemblyPath));

                                var processMeth = att.AttributeType.Resolve().Methods.FirstOrDefault(x => x.Name == _procMethodName);
                                if (processMeth != null)
                                {
                                    ilProcessor.InsertBefore(firstUserInstruction, ilProcessor.CreateLoadInstruction(currentMethod.Name));
                                    ilProcessor.InsertBefore(firstUserInstruction, ilProcessor.CreateLoadInstruction(para.Name));
                                    
                                    ilProcessor.InsertBefore(firstUserInstruction, ilProcessor.Create(OpCodes.Ldarg, currentParameter));

                                    ilProcessor.InsertBefore(firstUserInstruction, ilProcessor.Create(OpCodes.Call, currentMethod.Module.ImportReference(processMeth)));
                                }
                            }
                            currentParameter++;
                        }
                        

                        var returnInstruction = NormalizeReturns(currentMethod);
                        
                        var tryStart = currentMethod.Body.Instructions[2 * currentMethod.CustomAttributes.Count];
                        var beforeReturnInstruction = Instruction.Create(OpCodes.Nop);

                        ilProcessor.InsertBefore(returnInstruction, beforeReturnInstruction);

                       var afterPostInstruction = Instruction.Create(OpCodes.Nop);
                        ilProcessor.InsertBefore(returnInstruction, afterPostInstruction);

                        var beforePostInstruction = Instruction.Create(OpCodes.Nop);
                        ilProcessor.InsertBefore(afterPostInstruction, beforePostInstruction);

                        currentAttribute = 0;
                        foreach (var att in currentMethod.CustomAttributes)
                        {
                            var postMethod = att.AttributeType.Resolve().Methods.FirstOrDefault(x => x.Name == _postMethodName);
                            if (postMethod != null)
                            {
                                ilProcessor.InsertBefore(afterPostInstruction, ilProcessor.Create(OpCodes.Ldloc, currentAttribute));
                                currentAttribute++;
                                AddInterceptCall(ilProcessor, currentMethod, postMethod, att, afterPostInstruction);
                            }
                        }

                        ilProcessor.InsertBefore(returnInstruction, Instruction.Create(OpCodes.Endfinally));
                                                
                        var finallyHandler = new ExceptionHandler(ExceptionHandlerType.Finally)
                        {
                            TryStart = tryStart,
                            TryEnd = beforePostInstruction,
                            HandlerStart = beforePostInstruction,
                            HandlerEnd = returnInstruction,
                        };
                        
                        currentMethod.Body.ExceptionHandlers.Add(finallyHandler);
                        currentMethod.Body.InitLocals = true;                        
                    }
                }
            }
        }

        Instruction NormalizeReturns(MethodDefinition Method)
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
