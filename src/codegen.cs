/*
 * codegen.cs: code generator
 *
 * Copyright (C) 2003-2004 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

using Babel.Base;

namespace Babel.Compiler {
    public class CodeGeneratingVisitor : AbstractNodeVisitor {
        protected Program program;
        protected TypeManager typeManager;
        protected Report report;
        protected ClassDefinition currentClass;
        protected TypeBuilder currentType;
        protected RoutineDefinition currentRoutine;
        protected IterDefinition currentIter;
        protected ILGenerator ilGenerator;
        protected Label returnLabel;
        protected LocalVariableStack localVariableStack;
        protected LoopStatement currentLoop;
        protected LocalBuilder currentException;
        protected int exceptionLevel;
        protected bool inSharedContext;

        public CodeGeneratingVisitor(Report report)
        {
            this.report = report;
            exceptionLevel = 0;
            inSharedContext = false;
        }

        public override void VisitProgram(Program program)
        {
            this.program = program;
            typeManager = program.TypeManager;
            program.Children.Accept(this);
        }

        public override void VisitSourceFile(SourceFile sourceFile)
        {
            sourceFile.Children.Accept(this);
        }

        public override void VisitClass(ClassDefinition cls)
        {
            currentClass = cls;
            currentType = cls.TypeBuilder;
            cls.Children.Accept(this);
            if (cls.StaticConstructor != null) {
                cls.StaticConstructorIL.Emit(OpCodes.Ret);
            }
            currentType.CreateType();
            foreach (SupertypingAdapter adapter in cls.Adapters) {
                GenerateAdapter(adapter);
            }
        }

        protected virtual void GenerateAdapter(SupertypingAdapter adapter)
        {
            ilGenerator = adapter.Constructor.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stfld, adapter.AdapteeField);
            ilGenerator.Emit(OpCodes.Ret);
            foreach (SupertypingAdapterMethod method in adapter.Methods) {
                ilGenerator = method.MethodBuilder.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, adapter.AdapteeField);
                for (int i = 1; i <= method.ParameterCount; i++) {
                    ilGenerator.Emit(OpCodes.Ldarg, i);
                }
                ilGenerator.Emit(OpCodes.Tailcall);
                if (adapter.AdapteeType.IsAbstract)
                    ilGenerator.EmitCall(OpCodes.Callvirt,
                                         method.AdapteeMethod,
                                         null);
                else
                    ilGenerator.EmitCall(OpCodes.Call,
                                         method.AdapteeMethod,
                                         null);
                ilGenerator.Emit(OpCodes.Ret);
            }
            adapter.TypeBuilder.CreateType();
        }

        public override void VisitConst(ConstDefinition constDef)
        {
            ilGenerator = constDef.Reader.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldsfld, constDef.FieldBuilder);
            ilGenerator.Emit(OpCodes.Ret);
        }

        public override void VisitSharedAttr(SharedAttrDefinition attr)
        {
            ilGenerator = attr.Reader.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldsfld, attr.FieldBuilder);
            ilGenerator.Emit(OpCodes.Ret);

            ilGenerator = attr.Writer.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stsfld, attr.FieldBuilder);
            ilGenerator.Emit(OpCodes.Ret);

            ilGenerator = currentClass.StaticConstructorIL;
            inSharedContext = true;
            attr.Value.Accept(this);
            ilGenerator.Emit(OpCodes.Stsfld, attr.FieldBuilder);
            inSharedContext = false;
        }

        public override void VisitAttr(AttrDefinition attr)
        {
            ilGenerator = attr.Reader.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, attr.FieldBuilder);
            ilGenerator.Emit(OpCodes.Ret);

            ilGenerator = attr.Writer.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stfld, attr.FieldBuilder);
            ilGenerator.Emit(OpCodes.Ret);
        }

        public override void VisitRoutine(RoutineDefinition routine)
        {
            currentRoutine = routine;
            MethodBuilder methodBuilder = routine.MethodBuilder;
            ilGenerator = methodBuilder.GetILGenerator();
            returnLabel = ilGenerator.DefineLabel();
            localVariableStack = new RoutineLocalVariableStack();
            foreach (Argument arg in routine.Arguments) {
                if (arg.Mode == ArgumentMode.Out) {
                    ilGenerator.Emit(OpCodes.Ldarg, arg.Index);
                    Type argType = arg.RawType.GetElementType();
                    EmitVoid(argType);
                    EmitStind(argType);
                }
            }
            routine.StatementList.Accept(this);
            ilGenerator.MarkLabel(returnLabel);
            ilGenerator.Emit(OpCodes.Ret);
            if ((program.Target == Target.Exe ||
                 program.Target == Target.WinExe) &&
                currentType.Name == "MAIN" &&
                methodBuilder.Name == "main") {
                MethodBuilder entryPoint =
                    currentType.DefineMethod("Main",
                                             MethodAttributes.Public |
                                             MethodAttributes.Static,
                                             null, Type.EmptyTypes);
                ILGenerator il = entryPoint.GetILGenerator();
                il.Emit(OpCodes.Newobj, currentClass.Constructor);
                il.EmitCall(OpCodes.Call, methodBuilder, null);
                il.Emit(OpCodes.Ret);
                PEFileKinds kind;
                if (program.Target == Target.Exe)
                    kind = PEFileKinds.ConsoleApplication;
                else
                    kind = PEFileKinds.WindowApplication;
                program.Assembly.SetEntryPoint(entryPoint, kind);
            }
            currentRoutine = null;
        }

        public override void VisitIter(IterDefinition iter)
        {
            currentRoutine = currentIter = iter;

            ilGenerator = iter.Constructor.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stfld, iter.Self);
            int index = 2;
            foreach (Argument arg in iter.Arguments) {
                if (arg.Mode == ArgumentMode.Once) {
                    LocalVariable local =
                        (LocalVariable) iter.LocalVariables[arg.Name];
                    local.Declare(ilGenerator);
                    local.EmitStorePrefix(ilGenerator);
                    ilGenerator.Emit(OpCodes.Ldarg, index++);
                    local.EmitStore(ilGenerator);
                }
            }
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Stfld, iter.CurrentPosition);
            ilGenerator.Emit(OpCodes.Ret);

            ilGenerator = iter.MethodBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            foreach (Argument arg in iter.Arguments) {
                if (arg.Mode == ArgumentMode.Once) {
                    ilGenerator.Emit(OpCodes.Ldarg, arg.Index);
                }
            }
            ilGenerator.Emit(OpCodes.Newobj, iter.Constructor);
            ilGenerator.Emit(OpCodes.Ret);

            foreach (MethodBuilder bridgeMethod in iter.BridgeMethods) {
                ilGenerator = bridgeMethod.GetILGenerator();
                ilGenerator.Emit(OpCodes.Jmp, iter.MethodBuilder);
            }

            ilGenerator = iter.MoveNext.GetILGenerator();
            returnLabel = ilGenerator.DefineLabel();
            localVariableStack = new IterLocalVariableStack(iter.TypeBuilder);
            localVariableStack.Push(iter.LocalVariables);
            foreach (Argument arg in iter.MoveNextArguments) {
                if (arg.Mode == ArgumentMode.Out) {
                    ilGenerator.Emit(OpCodes.Ldarg, arg.Index);
                    Type argType = arg.RawType.GetElementType();
                    EmitVoid(argType);
                    EmitStind(argType);
                }
            }
            Label[] resumePoints = new Label[iter.ResumePoints.Count];
            int i = 0;
            foreach (ResumePoint resumePoint in iter.ResumePoints) {
                resumePoint.Label = ilGenerator.DefineLabel();
                resumePoints[i++] = resumePoint.Label;
            }
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, iter.CurrentPosition);
            ilGenerator.Emit(OpCodes.Switch, resumePoints);
            ilGenerator.MarkLabel(resumePoints[0]);
            iter.StatementList.Accept(this);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.MarkLabel(returnLabel);
            ilGenerator.Emit(OpCodes.Ret);

            if (iter.GetCurrent != null) {
                ilGenerator = iter.GetCurrent.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, iter.Current);
                ilGenerator.Emit(OpCodes.Ret);
            }

            iter.TypeBuilder.CreateType();
            currentRoutine = currentIter = null;
       }

        public override void VisitStatementList(StatementList statementList)
        {
            localVariableStack.Push(statementList.LocalVariables);
            ilGenerator.BeginScope();
            statementList.Children.Accept(this);
            ilGenerator.EndScope();
            localVariableStack.Pop();
        }

        public override void VisitDeclaration(DeclarationStatement decl)
        {
            LocalVariable local = localVariableStack.GetLocal(decl.Name);
            local.Declare(ilGenerator);
        }

        public override void VisitAssign(AssignStatement assign)
        {

            Argument arg = currentRoutine.GetArgument(assign.Name);
            if (arg != null) {
                if (arg.RawType.IsByRef) {
                    Type argType = arg.RawType.GetElementType();
                    ilGenerator.Emit(OpCodes.Ldarg, arg.Index);
                    assign.Value.Accept(this);
                    BoxIfNecessary(assign.Value.RawType, argType);
                    EmitStind(argType);
                }
                else {
                    assign.Value.Accept(this);
                    BoxIfNecessary(assign.Value.RawType, arg.RawType);
                    ilGenerator.Emit(OpCodes.Starg, arg.Index);
                }
            }
            else {
                LocalVariable local = localVariableStack.GetLocal(assign.Name);
                local.EmitStorePrefix(ilGenerator);
                assign.Value.Accept(this);
                BoxIfNecessary(assign.Value.RawType, local.RawType);
                local.EmitStore(ilGenerator);
            }
        }

        public override void VisitIf(IfStatement ifStmt)
        {
            Label endLabel = ilGenerator.DefineLabel();
            ifStmt.Test.Accept(this);
            if (ifStmt.ElsePart == null) {
                ilGenerator.Emit(OpCodes.Brfalse, endLabel);
                ifStmt.ThenPart.Accept(this);
            }
            else {
                Label elseLabel = ilGenerator.DefineLabel();
                ilGenerator.Emit(OpCodes.Brfalse, elseLabel);
                ifStmt.ThenPart.Accept(this);
                ilGenerator.Emit(OpCodes.Br, endLabel);
                ilGenerator.MarkLabel(elseLabel);
                ifStmt.ElsePart.Accept(this);
            }
            ilGenerator.MarkLabel(endLabel);
        }

        public override void VisitReturn(ReturnStatement ret)
        {
            if (ret.Value != null) {
                ret.Value.Accept(this);
                BoxIfNecessary(ret.Value.RawType,
                               currentRoutine.ReturnType.RawType);
            }
            if (exceptionLevel > 0) {
                ilGenerator.Emit(OpCodes.Leave, returnLabel);
            }
            else {
                ilGenerator.Emit(OpCodes.Br, returnLabel);
            }
        }

        public override void VisitCase(CaseStatement caseStmt)
        {
            LocalVariable test = localVariableStack.GetLocal(caseStmt.TestName);
            test.Declare(ilGenerator);
            test.EmitStorePrefix(ilGenerator);
            caseStmt.Test.Accept(this);
            BoxIfNecessary(caseStmt.Test.RawType, test.RawType);
            test.EmitStore(ilGenerator);

            Label endLabel = ilGenerator.DefineLabel();
            foreach (CaseWhen when in caseStmt.WhenPartList) {
                Label thenLabel = ilGenerator.DefineLabel();
                Label nextLabel = ilGenerator.DefineLabel();
                foreach (CallExpression call in when.TestCallList) {
                    call.Accept(this);
                    ilGenerator.Emit(OpCodes.Brtrue, thenLabel);
                }
                ilGenerator.Emit(OpCodes.Br, nextLabel);
                ilGenerator.MarkLabel(thenLabel);
                when.ThenPart.Accept(this);
                ilGenerator.Emit(OpCodes.Br, endLabel);
                ilGenerator.MarkLabel(nextLabel);
            }
            if (caseStmt.ElsePart != null)
                caseStmt.ElsePart.Accept(this);
            ilGenerator.MarkLabel(endLabel);
        }

        public override void VisitTypecase(TypecaseStatement typecase)
        {
            Label endLabel = ilGenerator.DefineLabel();
            foreach (TypecaseWhen when in typecase.WhenPartList) {
                Label nextLabel = ilGenerator.DefineLabel();
                typecase.Variable.Accept(this);
                ilGenerator.Emit(OpCodes.Isinst, when.TypeSpecifier.RawType);
                ilGenerator.Emit(OpCodes.Brfalse, nextLabel);
                when.LocalVariable.Declare(ilGenerator);
                when.LocalVariable.EmitStorePrefix(ilGenerator);
                typecase.Variable.Accept(this);
                TypeData varType = typecase.Variable.NodeType;
                TypeData whenType = when.TypeSpecifier.NodeType;
                if (!varType.IsSubtypeOf(whenType)) {
                    UnboxIfNecessary(varType.RawType, whenType.RawType);
                }
                when.LocalVariable.EmitStore(ilGenerator);
                when.ThenPart.Accept(this);
                ilGenerator.Emit(OpCodes.Br, endLabel);
                ilGenerator.MarkLabel(nextLabel);
            }
            if (typecase.ElsePart != null)
                typecase.ElsePart.Accept(this);
            ilGenerator.MarkLabel(endLabel);
        }

        public override void VisitLoop(LoopStatement loop)
        {
            LoopStatement prevLoop = currentLoop;
            currentLoop = loop;
            Label beginLabel = ilGenerator.DefineLabel();
            loop.EndLabel = ilGenerator.DefineLabel();
            ilGenerator.MarkLabel(beginLabel);
            loop.StatementList.Accept(this);
            ilGenerator.Emit(OpCodes.Br, beginLabel);
            ilGenerator.MarkLabel(loop.EndLabel);
            currentLoop = prevLoop;
        }

        public override void VisitYield(YieldStatement yield)
        {
            if (exceptionLevel > 0) {
                report.Error(yield.Location,
                             "`yield' statements may not appear " +
                             "in `protect' statements.");
                return;
            }
            if (yield.Value != null) {
                ilGenerator.Emit(OpCodes.Ldarg_0);
                yield.Value.Accept(this);
                BoxIfNecessary(yield.Value.RawType,
                               currentIter.Current.FieldType);
                ilGenerator.Emit(OpCodes.Stfld, currentIter.Current);
            }
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldc_I4, yield.ResumePoint.Index);
            ilGenerator.Emit(OpCodes.Stfld, currentIter.CurrentPosition);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.Emit(OpCodes.Br, returnLabel);
            ilGenerator.MarkLabel(yield.ResumePoint.Label);
        }

        public override void VisitQuit(QuitStatement quit)
        {
            if (exceptionLevel > 0) {
                report.Error(quit.Location,
                             "`quit' statements may not appear " +
                             "in `protect' statements.");
                return;
            }
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.Emit(OpCodes.Br, returnLabel);
        }

        public override void VisitProtect(ProtectStatement protect)
        {
            exceptionLevel++;
            ilGenerator.BeginExceptionBlock();
            protect.StatementList.Accept(this);
            foreach (ProtectWhen when in protect.WhenPartList) {
                ilGenerator.BeginCatchBlock(when.TypeSpecifier.RawType);
                LocalBuilder prevException = currentException;
                currentException =
                    ilGenerator.DeclareLocal(when.TypeSpecifier.RawType);
                ilGenerator.Emit(OpCodes.Stloc, currentException);
                when.ThenPart.Accept(this);
                currentException = prevException;
            }
            if (protect.ElsePart != null) {
                ilGenerator.BeginCatchBlock(typeof(Exception));
                LocalBuilder prevException = currentException;
                currentException =
                    ilGenerator.DeclareLocal(typeof(Exception));
                ilGenerator.Emit(OpCodes.Stloc, currentException);
                protect.ElsePart.Accept(this);
                currentException = prevException;
            }
            ilGenerator.EndExceptionBlock();
            exceptionLevel--;
        }

        public override void VisitRaise(RaiseStatement raise)
        {
            raise.Value.Accept(this);
            if (raise.Value.NodeType == typeManager.StrType) {
                Type etype = typeof(Exception);
                ConstructorInfo constructor =
                    etype.GetConstructor(new Type[] { typeof(string) });
                ilGenerator.Emit(OpCodes.Newobj, constructor);
            }
            ilGenerator.Emit(OpCodes.Throw);
        }

        public override void VisitExpressionStatement(ExpressionStatement exprStmt)
        {
            exprStmt.Expression.Accept(this);
        }

        public override void VisitBoolLiteral(BoolLiteralExpression boolLiteral)
        {
            if (boolLiteral.Value) {
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
            }
            else {
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
            }
        }

        public override void VisitIntLiteral(IntLiteralExpression intLiteral)
        {
            ilGenerator.Emit(OpCodes.Ldc_I4, intLiteral.Value);
        }

        public override void VisitCharLiteral(CharLiteralExpression charLiteral)
        {
            ilGenerator.Emit(OpCodes.Ldc_I4, charLiteral.Value);
        }

        public override void VisitStrLiteral(StrLiteralExpression strLiteral)
        {
            ilGenerator.Emit(OpCodes.Ldstr, strLiteral.Value);
        }

        public override void VisitSelf(SelfExpression self)
        {
            if (inSharedContext) {
                EmitVoid(self.RawType);
            }
            else if (currentIter == null) {
                ilGenerator.Emit(OpCodes.Ldarg_0);
            }
            else {
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, currentIter.Self);
            }
        }

        public override void VisitLocal(LocalExpression localExpr)
        {
            Argument arg = currentRoutine.GetArgument(localExpr.Name);
            if (arg != null) {
                ilGenerator.Emit(OpCodes.Ldarg, arg.Index);
                if (arg.RawType.IsByRef)
                    EmitLdind(arg.RawType.GetElementType());
                return;
            }
            LocalVariable local = localVariableStack.GetLocal(localExpr.Name);
            local.EmitLoad(ilGenerator);
        }

        public override void VisitCall(CallExpression call)
        {
            MethodInfo method = call.Method;
            ParameterInfo[] parameters = typeManager.GetParameters(method);
            int i = call.IsBuiltin ? 1 : 0;
            if (call.Flip) {
                ModalExpression arg = (ModalExpression) call.Arguments.First;
                arg.Accept(this);
                BoxIfNecessary(arg.RawType, parameters[i].ParameterType);
                LocalBuilder tmp =
                    ilGenerator.DeclareLocal(parameters[i].ParameterType);
                ilGenerator.Emit(OpCodes.Stloc, tmp);
                call.Receiver.Accept(this);
                ilGenerator.Emit(OpCodes.Ldloc, tmp);
            }
            else {
                if (call.Receiver != null) {
                    if (!call.IsBuiltin &&
                        call.Receiver.RawType.IsValueType) {
                        if (call.Receiver is LocalExpression) {
                            LocalExpression localExpr =
                                (LocalExpression) call.Receiver;
                            Argument arg =
                                currentRoutine.GetArgument(localExpr.Name);
                            if (arg != null) {
                                ilGenerator.Emit(OpCodes.Ldarga, arg.Index);
                            }
                            else {
                                LocalVariable local =
                                    localVariableStack.GetLocal(localExpr.Name);
                                local.EmitLoadAddress(ilGenerator);
                            }
                        }
                        else {
                            LocalBuilder tmp2 =
                                ilGenerator.DeclareLocal(call.Receiver.RawType);
                            call.Receiver.Accept(this);
                            ilGenerator.Emit(OpCodes.Stloc, tmp2);
                            ilGenerator.Emit(OpCodes.Ldloca, tmp2);
                        }
                    }
                    else {
                        call.Receiver.Accept(this);
                    }
                }
                foreach (ModalExpression arg in call.Arguments) {
                    arg.Accept(this);
                    BoxIfNecessary(arg.RawType, parameters[i].ParameterType);
                    i++;
                }
            }
            if (call.Receiver != null &&
                call.Receiver.RawType.IsInterface)
                ilGenerator.EmitCall(OpCodes.Callvirt, method, null);
            else
                ilGenerator.EmitCall(OpCodes.Call, method, null);
        }

        public override void VisitIterCall(IterCallExpression iter)
        {
            Label moveNextLabel = ilGenerator.DefineLabel();
            Label getCurrentLabel = ilGenerator.DefineLabel();
            iter.Local.Declare(ilGenerator);
            iter.Local.EmitLoad(ilGenerator);
            ilGenerator.Emit(OpCodes.Brtrue, moveNextLabel);
            iter.Local.EmitStorePrefix(ilGenerator);
            MethodInfo method = iter.Method;
            ParameterInfo[] parameters = typeManager.GetParameters(method);
            int i = iter.IsBuiltin ? 1 : 0;
            if (iter.Receiver != null)
                iter.Receiver.Accept(this);
            foreach (ModalExpression arg in iter.Arguments) {
                ParameterInfo param = parameters[i++];
                ArgumentMode mode = typeManager.GetArgumentMode(param);
                if (mode == ArgumentMode.Once) {
                    arg.Accept(this);
                    BoxIfNecessary(arg.RawType, param.ParameterType);
                }
                else {
                    EmitVoid(param.ParameterType);
                }
            }
            if (iter.Receiver != null &&
                iter.Receiver.RawType.IsInterface)
                ilGenerator.EmitCall(OpCodes.Callvirt, method, null);
            else
                ilGenerator.EmitCall(OpCodes.Call, method, null);
            iter.Local.EmitStore(ilGenerator);
            ilGenerator.MarkLabel(moveNextLabel);
            iter.MoveNext.Accept(this);
            ilGenerator.Emit(OpCodes.Brtrue, getCurrentLabel);
            ilGenerator.Emit(OpCodes.Leave, currentLoop.EndLabel);
            ilGenerator.MarkLabel(getCurrentLabel);
            if (iter.GetCurrent != null)
                iter.GetCurrent.Accept(this);
        }

        public override void VisitModalExpression(ModalExpression modalExpr)
        {
            if (modalExpr.Mode == ArgumentMode.Out ||
                modalExpr.Mode == ArgumentMode.InOut) {
                LocalExpression localExpr =
                    modalExpr.Expression as LocalExpression;
                Argument arg = currentRoutine.GetArgument(localExpr.Name);
                if (arg != null) {
                    ilGenerator.Emit(OpCodes.Ldarga, arg.Index);
                    return;
                }
                LocalVariable local =
                    localVariableStack.GetLocal(localExpr.Name);
                local.EmitLoadAddress(ilGenerator);
            }
            else {
                modalExpr.Expression.Accept(this);
            }
        }

        public override void VisitVoid(VoidExpression voidExpr)
        {
            EmitVoid(voidExpr.RawType);
        }

        public override void VisitVoidTest(VoidTestExpression voidTest)
        {
            voidTest.Expression.Accept(this);
            EmitVoid(voidTest.Expression.RawType);
            ilGenerator.Emit(OpCodes.Ceq);
        }

        public override void VisitNew(NewExpression newExpr)
        {
            ParameterInfo[] parameters =
                typeManager.GetParameters(newExpr.Constructor);
            ModalExpression arg = (ModalExpression) newExpr.Arguments.First;
            foreach (ParameterInfo param in parameters) {
                if (arg == null)
                    break;
                arg.Accept(this);
                BoxIfNecessary(arg.RawType, param.ParameterType);
                arg = (ModalExpression) arg.Next;
            }
            ilGenerator.Emit(OpCodes.Newobj, newExpr.Constructor);
        }

        public override void VisitAnd(AndExpression and)
        {
            Label falseLabel = ilGenerator.DefineLabel();
            Label endLabel = ilGenerator.DefineLabel();
            and.Left.Accept(this);
            ilGenerator.Emit(OpCodes.Brfalse, falseLabel);
            and.Right.Accept(this);
            ilGenerator.Emit(OpCodes.Br, endLabel);
            ilGenerator.MarkLabel(falseLabel);
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
            ilGenerator.MarkLabel(endLabel);
        }

        public override void VisitOr(OrExpression or)
        {
            Label trueLabel = ilGenerator.DefineLabel();
            Label endLabel = ilGenerator.DefineLabel();
            or.Left.Accept(this);
            ilGenerator.Emit(OpCodes.Brtrue, trueLabel);
            or.Right.Accept(this);
            ilGenerator.Emit(OpCodes.Br, endLabel);
            ilGenerator.MarkLabel(trueLabel);
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            ilGenerator.MarkLabel(endLabel);
        }

        public override void VisitBreak(BreakExpression breakExpr)
        {
            ilGenerator.Emit(OpCodes.Leave, currentLoop.EndLabel);
        }

        public override void VisitException(ExceptionExpression exception)
        {
            ilGenerator.Emit(OpCodes.Ldloc, currentException);
        }

        protected virtual void BoxIfNecessary(Type sourceType,
                                              Type destinationType)
        {
            Type adapterType = typeManager.GetSupertypingAdapter(destinationType,
                                                             sourceType);
            if (adapterType != null) {
                ConstructorInfo[] constructors =
                    typeManager.GetConstructors(adapterType);
                ilGenerator.Emit(OpCodes.Newobj, constructors[0]);
                return;
            }
            if (sourceType.IsValueType &&
                !destinationType.IsValueType)
                ilGenerator.Emit(OpCodes.Box, sourceType);
        }

        protected virtual void UnboxIfNecessary(Type sourceType,
                                                Type destinationType)
        {
            if (!sourceType.IsValueType &&
                destinationType.IsValueType) {
                ilGenerator.Emit(OpCodes.Unbox, destinationType);
                ilGenerator.Emit(OpCodes.Ldobj, destinationType);
            }
        }

        protected virtual void EmitVoid(Type type)
        {
            if (type == typeof(void)) {
            }
            else if (type == typeof(bool) ||
                type == typeof(char) ||
                type == typeof(int)) {
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
            }
            else {
                ilGenerator.Emit(OpCodes.Ldnull);
            }
        }

        protected virtual void EmitLdind(Type type)
        {
            if (type == typeof(bool) ||
                type == typeof(int)) {
                ilGenerator.Emit(OpCodes.Ldind_I4);
            } else if (type == typeof(char)) {
                ilGenerator.Emit(OpCodes.Ldind_I2);
            }
            else {
                ilGenerator.Emit(OpCodes.Ldind_Ref);
            }
        }

        protected virtual void EmitStind(Type type)
        {
            if (type == typeof(bool) ||
                type == typeof(int)) {
                ilGenerator.Emit(OpCodes.Stind_I4);
            } else if (type == typeof(char)) {
                ilGenerator.Emit(OpCodes.Stind_I2);
            }
            else {
                ilGenerator.Emit(OpCodes.Stind_Ref);
            }
        }
    }
}
