/*
 * codegen.cs: code generator
 *
 * Copyright (C) 2003 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

namespace Babel.Sather.Compiler
{
    public class CodeGeneratingVisitor : AbstractNodeVisitor
    {
        Program program;
        TypeManager typeManager;
        Report report;
        ClassDefinition currentClass;
        TypeBuilder currentType;
        RoutineDefinition currentRoutine;
        ILGenerator ilGenerator;
        Label returnLabel;
        LocalVariableStack localVariableStack;
        LoopStatement currentLoop;
        LocalBuilder currentException;
        int exceptionLevel;
        bool inSharedContext;

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
            MethodBuilder methodBuilder = routine.MethodBuilder;
            ilGenerator = methodBuilder.GetILGenerator();
            returnLabel = ilGenerator.DefineLabel();
            currentRoutine = routine;
            localVariableStack = new LocalVariableStack();
            routine.StatementList.Accept(this);
            ilGenerator.MarkLabel(returnLabel);
            ilGenerator.Emit(OpCodes.Ret);
            if (currentType.Name == "MAIN" &&
                methodBuilder.Name == "main") {
                MethodBuilder main =
                    currentType.DefineMethod("Main",
                                             MethodAttributes.Public |
                                             MethodAttributes.Static,
                                             null, Type.EmptyTypes);
                ILGenerator il = main.GetILGenerator();
                il.Emit(OpCodes.Newobj, currentClass.Constructor);
                il.EmitCall(OpCodes.Call, methodBuilder, null);
                il.Emit(OpCodes.Ret);
                program.Assembly.SetEntryPoint(main,
                                               PEFileKinds.ConsoleApplication);
            }
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
            LocalVariable local = localVariableStack.Get(decl.Name);
            local.LocalBuilder = ilGenerator.DeclareLocal(local.LocalType);
        }

        public override void VisitAssign(AssignStatement assign)
        {
            assign.Value.Accept(this);

            Argument arg = currentRoutine.GetArgument(assign.Name);
            if (arg != null) {
                BoxIfNecessary(assign.Value.NodeType, arg.NodeType);
                ilGenerator.Emit(OpCodes.Starg, arg.Index);
            }
            else {
                LocalVariable local = localVariableStack.Get(assign.Name);
                BoxIfNecessary(assign.Value.NodeType, local.LocalType);
                ilGenerator.Emit(OpCodes.Stloc, local.LocalBuilder);
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
                BoxIfNecessary(ret.Value.NodeType,
                               currentRoutine.ReturnType.NodeType);
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
            LocalVariable test = localVariableStack.Get(caseStmt.TestName);
            test.LocalBuilder = ilGenerator.DeclareLocal(test.LocalType);
            caseStmt.Test.Accept(this);
            BoxIfNecessary(caseStmt.Test.NodeType, test.LocalType);
            ilGenerator.Emit(OpCodes.Stloc, test.LocalBuilder);

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
                ilGenerator.Emit(OpCodes.Isinst, when.TypeSpecifier.NodeType);
                ilGenerator.Emit(OpCodes.Brfalse, nextLabel);
                if (typeManager.IsSubtype(typecase.Variable.NodeType,
                                          when.TypeSpecifier.NodeType)) {
                    when.LocalVariable.LocalBuilder =
                        ilGenerator.DeclareLocal(typecase.Variable.NodeType);
                    typecase.Variable.Accept(this);
                }
                else {
                    when.LocalVariable.LocalBuilder =
                        ilGenerator.DeclareLocal(when.TypeSpecifier.NodeType);
                    typecase.Variable.Accept(this);
                    UnboxIfNecessary(typecase.Variable.NodeType,
                                     when.TypeSpecifier.NodeType);
                }
                ilGenerator.Emit(OpCodes.Stloc,
                                 when.LocalVariable.LocalBuilder);
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

        public override void VisitProtect(ProtectStatement protect)
        {
            exceptionLevel++;
            ilGenerator.BeginExceptionBlock();
            protect.StatementList.Accept(this);
            foreach (ProtectWhen when in protect.WhenPartList) {
                ilGenerator.BeginCatchBlock(when.TypeSpecifier.NodeType);
                LocalBuilder prevException = currentException;
                currentException =
                    ilGenerator.DeclareLocal(when.TypeSpecifier.NodeType);
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
            if (raise.Value.NodeType == typeof(string)) {
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
                EmitVoid(self.NodeType);
            }
            else {
                ilGenerator.Emit(OpCodes.Ldarg_0);
            }
        }

        public override void VisitLocal(LocalExpression localExpr)
        {
            Argument arg = currentRoutine.GetArgument(localExpr.Name);
            if (arg != null) {
                ilGenerator.Emit(OpCodes.Ldarg, arg.Index);
                return;
            }
            LocalVariable local = localVariableStack.Get(localExpr.Name);
            ilGenerator.Emit(OpCodes.Ldloc, local.LocalBuilder);
        }

        public override void VisitCall(CallExpression call)
        {
            MethodInfo method = call.Method;
            ParameterInfo[] parameters = typeManager.GetParameters(method);
            int i = call.IsBuiltin ? 1 : 0;
            if (call.Flip) {
                ModalExpression arg = (ModalExpression) call.Arguments.First;
                arg.Accept(this);
                BoxIfNecessary(arg.NodeType, parameters[i].ParameterType);
                LocalBuilder local =
                    ilGenerator.DeclareLocal(parameters[i].ParameterType);
                ilGenerator.Emit(OpCodes.Stloc, local);
                call.Receiver.Accept(this);
                ilGenerator.Emit(OpCodes.Ldloc, local);
            }
            else {
                if (call.Receiver != null)
                    call.Receiver.Accept(this);
                foreach (ModalExpression arg in call.Arguments) {
                    arg.Accept(this);
                    BoxIfNecessary(arg.NodeType, parameters[i].ParameterType);
                    i++;
                }
            }
            if (call.Receiver != null &&
                call.Receiver.NodeType.IsInterface)
                ilGenerator.EmitCall(OpCodes.Callvirt, method, null);
            else
                ilGenerator.EmitCall(OpCodes.Call, method, null);
        }

        public override void VisitVoid(VoidExpression voidExpr)
        {
            EmitVoid(voidExpr.NodeType);
        }

        public override void VisitVoidTest(VoidTestExpression voidTest)
        {
            voidTest.Expression.Accept(this);
            EmitVoid(voidTest.Expression.NodeType);
            ilGenerator.Emit(OpCodes.Ceq);
        }

        public override void VisitNew(NewExpression newExpr)
        {
            ilGenerator.Emit(OpCodes.Newobj, currentClass.Constructor);
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
            if (currentLoop == null) {
                report.Error(breakExpr.Location,
                             "`break!', `while!', `until!' calls must appear inside loops");
                return;
            }
            ilGenerator.Emit(OpCodes.Br, currentLoop.EndLabel);
        }

        public override void VisitException(ExceptionExpression exception)
        {
            ilGenerator.Emit(OpCodes.Ldloc, currentException);
        }

        protected void BoxIfNecessary(Type sourceType, Type destinationType)
        {
            if (sourceType.IsValueType &&
                !destinationType.IsValueType)
                ilGenerator.Emit(OpCodes.Box, sourceType);
        }

        protected void UnboxIfNecessary(Type sourceType, Type destinationType)
        {
            if (!sourceType.IsValueType &&
                destinationType.IsValueType) {
                ilGenerator.Emit(OpCodes.Unbox, destinationType);
                ilGenerator.Emit(OpCodes.Ldobj, destinationType);
            }
        }

        protected void EmitVoid(Type type)
        {
            if (type == typeof(bool) ||
                type == typeof(char) ||
                type == typeof(int)) {
                ilGenerator.Emit(OpCodes.Ldc_I4_0);
            }
            else {
                ilGenerator.Emit(OpCodes.Ldnull);
            }
        }
    }
}
