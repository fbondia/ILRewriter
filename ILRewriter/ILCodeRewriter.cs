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
                            AddInterceptCall(m, preMethod, true);
                        }
                        if (postMethod != null)
                        {
                            AddInterceptCall(m, postMethod, false);
                        }
                    }
                }
            }
        }

        private void AddInterceptCall(MethodDefinition m, MethodDefinition preMethod, bool prepend)
        {
            
                var metDef = _assemblyDefinition.MainModule.Import(preMethod);

                var ilProcessor = m.Body.GetILProcessor();
            Instruction firstInstruction = null;

            if (prepend)
            {
                firstInstruction = ilProcessor.Body.Instructions.First();
            }
            else
            {
                firstInstruction = ilProcessor.Body.Instructions.Last();
            }

                ilProcessor.InsertBefore(firstInstruction, ilProcessor.CreateLoadInstruction(m.Name));
            
                int methodParamCount = m.Parameters.Count;
                int arrayVarNr = m.Body.Variables.Count;

                if (methodParamCount > 0)
                {
                    ArrayType objArrType = new ArrayType(_assemblyDefinition.MainModule.TypeSystem.Object);
                    m.Body.Variables.Add(new VariableDefinition(objArrType));

                    m.Body.InitLocals = true;

                    ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldc_I4, methodParamCount));
                    ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Newarr, _assemblyDefinition.MainModule.TypeSystem.Object));
                    ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Stloc, arrayVarNr));

                    MetadataType paramMetaData;
                    bool pointerToValueTypeVariable;
                    TypeSpecification referencedTypeSpec = null;

                    for (int i = 0; i < methodParamCount; i++)
                    {
                        paramMetaData = metDef.Parameters[i].ParameterType.MetadataType;
                        if (paramMetaData == MetadataType.UIntPtr || paramMetaData == MetadataType.FunctionPointer ||
                            paramMetaData == MetadataType.IntPtr || paramMetaData == MetadataType.Pointer)
                        {
                            break;
                        }

                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldloc, arrayVarNr));
                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldc_I4, i));

                        if (m.IsStatic)
                        {
                            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldarg, i));
                        }
                        else
                        {
                            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldarg, i + 1));
                        }

                        pointerToValueTypeVariable = false;

                        TypeReference paramType = metDef.Parameters[i].ParameterType;
                        if (paramType.IsByReference)
                        {
                            referencedTypeSpec = paramType as TypeSpecification;

                            if (referencedTypeSpec != null)
                            {
                                switch (referencedTypeSpec.ElementType.MetadataType)
                                {
                                    case MetadataType.Boolean:
                                    case MetadataType.SByte:
                                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldind_I1));
                                        pointerToValueTypeVariable = true;
                                        break;

                                    case MetadataType.Int16:
                                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldind_I2));
                                        pointerToValueTypeVariable = true;
                                        break;

                                    case MetadataType.Int32:
                                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldind_I4));
                                        pointerToValueTypeVariable = true;
                                        break;

                                    case MetadataType.Int64:
                                    case MetadataType.UInt64:
                                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldind_I8));
                                        pointerToValueTypeVariable = true;
                                        break;

                                    case MetadataType.Byte:
                                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldind_U1));
                                        pointerToValueTypeVariable = true;
                                        break;

                                    case MetadataType.UInt16:
                                    case MetadataType.Char:
                                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldind_U2));
                                        pointerToValueTypeVariable = true;
                                        break;

                                    case MetadataType.UInt32:
                                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldind_U4));
                                        pointerToValueTypeVariable = true;
                                        break;

                                    case MetadataType.Single:
                                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldind_R4));
                                        pointerToValueTypeVariable = true;
                                        break;

                                    case MetadataType.Double:
                                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldind_R8));
                                        pointerToValueTypeVariable = true;
                                        break;

                                    case MetadataType.IntPtr:
                                    case MetadataType.UIntPtr:
                                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldind_I));
                                        pointerToValueTypeVariable = true;
                                        break;

                                    default:
                                        if (referencedTypeSpec.ElementType.IsValueType)
                                        {
                                            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldobj, referencedTypeSpec.ElementType));
                                            pointerToValueTypeVariable = true;
                                        }
                                        else
                                        {
                                            ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldind_Ref));
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
                                ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Box, referencedTypeSpec.ElementType));
                            }
                            else
                            {
                                ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Box, paramType));
                            }
                        }
                        ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Stelem_Ref));
                    }


                    ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Ldloc, arrayVarNr));
                }

                ilProcessor.InsertBefore(firstInstruction, ilProcessor.Create(OpCodes.Call, metDef));
            
        }

        public void Reweave()
        {
            _assemblyDefinition.Write(_assemblyPath);
        }
    }
}
