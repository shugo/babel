/*
 * typecheck.cs: check types
 *
 * Copyright (C) 2003-2004 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

using Babel.Core;

namespace Babel.Compiler {
    public class TypeCheckingVisitor : AbstractNodeVisitor {
        protected Program program;
        protected TypeManager typeManager;
        protected Report report;
        protected SourceFile currentSouceFile;
        protected ClassDefinition currentClass;
        protected RoutineDefinition currentRoutine;
        protected IterDefinition currentIter;
        protected LocalVariableStack localVariableStack;
        protected LoopStatement currentLoop;
        protected int temporallyCount;
        protected TypeData currentExceptionType;
        protected bool inSharedContext;

        public TypeCheckingVisitor(Report report)
        {
            this.report = report;
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
            currentSouceFile = sourceFile;
            sourceFile.Children.Accept(this);
        }

        public override void VisitClass(ClassDefinition cls)
        {
            ClassDefinition prevClass = currentClass;
            currentClass = cls;
            cls.Children.Accept(this);
            currentClass = prevClass;
        }

        public override void VisitSharedAttr(SharedAttrDefinition attr)
        {
            inSharedContext = true;
            attr.Value.Accept(this);
            inSharedContext = false;
        }

        public override void VisitRoutine(RoutineDefinition routine)
        {
            currentRoutine = routine;
            localVariableStack = new RoutineLocalVariableStack();
            temporallyCount = 0;
            routine.StatementList.Accept(this);
            currentRoutine = null;
        }

        public override void VisitIter(IterDefinition iter)
        {
            currentRoutine = currentIter = iter;
            localVariableStack = new IterLocalVariableStack(iter.TypeBuilder);
            localVariableStack.Push(iter.LocalVariables);
            foreach (Argument arg in iter.Arguments) {
                if (arg.Mode == ArgumentMode.Once)
                    localVariableStack.AddLocal(arg.Name, arg.NodeType);
            }
            temporallyCount = 0;
            iter.StatementList.Accept(this);
            currentRoutine = currentIter = null;
        }

        public override void VisitTypeSpecifier(TypeSpecifier typeSpecifier)
        {
            if (typeSpecifier.Kind == TypeKind.Same) {
                typeSpecifier.NodeType =
                    typeManager.GetTypeData(currentClass.TypeBuilder);
            }
            else {
                TypeData type =
                    currentClass.GetTypeParameter(typeSpecifier.Name);
                if (type == null) {
                    type = typeManager.GetType(typeSpecifier.Name,
                                        currentSouceFile.ImportedNamespaces);
                }
                if (type == null) {
                    report.Error(typeSpecifier.Location,
                                 "there is no class named {0}",
                                 typeSpecifier);
                    return;
                }
                typeSpecifier.NodeType = type;
            }
        }

        public override void VisitStatementList(StatementList statementList)
        {
            localVariableStack.Push(statementList.LocalVariables);
            statementList.Children.Accept(this);
            localVariableStack.Pop();
        }

        public override void VisitDeclaration(DeclarationStatement decl)
        {
            Argument arg = currentRoutine.GetArgument(decl.Name);
            if (arg != null) {
                report.Error(decl.Location,
                             "this local variable declaration is " +
                             "in the scope of {0}:{1} which has the same name",
                             arg.Name, arg.NodeType.FullName);
                return;
            }
            LocalVariable local = localVariableStack.GetLocal(decl.Name);
            if (local != null) {
                report.Error(decl.Location,
                             "this local variable declaration is " +
                             "in the scope of {0}:{1} which has the same name",
                             local.Name, local.LocalType.FullName);
                return;
            }
            TypeData type;
            if (!decl.TypeSpecifier.IsNull) {
                decl.TypeSpecifier.Accept(this);
                type = decl.TypeSpecifier.NodeType;
            }
            else {
                if (!(decl.Next is AssignStatement)) {
                    report.Error(decl.Location, "type not specified");
                    return;
                }
                AssignStatement assign = (AssignStatement) decl.Next;
                if (assign.Value is VoidExpression) {
                    report.Error(decl.Location,
                                 "right hand side of `::=' may not be `void'");
                    return;
                }
                assign.Value.Accept(this);
                type = assign.Value.NodeType;
            }
            localVariableStack.AddLocal(decl.Name, type);
        }

        public override void VisitAssign(AssignStatement assign)
        {
            TypeData lhsType;
            Argument arg = currentRoutine.GetArgument(assign.Name);
            if (arg != null) {
                if (arg.NodeType.IsByRef)
                    lhsType = arg.NodeType.ElementType;
                else
                    lhsType = arg.NodeType;
            }
            else {
                LocalVariable local = localVariableStack.GetLocal(assign.Name);
                if (local != null) {
                    if (local.IsTypecaseVariable) {
                        report.Error(assign.Location,
                                     "it is illegal " +
                                     "to assign to the typecase variable");
                        return;
                    }
                    lhsType = local.LocalType;
                }
                else {
                    Expression self = new SelfExpression(assign.Location);
                    ModalExpression arg1 =
                        new ModalExpression(ArgumentMode.In, assign.Value,
                                            assign.Value.Location);
                    TypedNodeList args = new TypedNodeList(arg1);
                    assign.Call = new CallExpression(self, assign.Name, args,
                                                     assign.Location);
                    assign.Call.HasValue = false;
                    assign.Call.Accept(this);
                    if (assign.Call.NodeType == null)
                        return;
                    ParameterInfo[] parameters =
                        typeManager.GetParameters(assign.Call.Method);
                    lhsType =
                        typeManager.GetTypeData(parameters[0].ParameterType);
                }
            }
            if (assign.Value is VoidExpression) {
                assign.Value.NodeType = lhsType;
            }
            else {
                // NodeType may be already set by DeclarationStatement
                if (assign.Value.NodeType == null)
                    assign.Value.Accept(this);
                if (assign.Value.NodeType == null)
                    return;
                if (!assign.Value.NodeType.IsSubtypeOf(lhsType)) {
                    report.Error(assign.Location,
                                 "{0} is not a subtype of {1}",
                                 assign.Value.NodeType, lhsType);
                    return;
                }
            }
        }

        public override void VisitIf(IfStatement ifStmt)
        {
            ifStmt.Test.Accept(this);
            if (ifStmt.Test.NodeType != typeManager.BoolType) {
                report.Error(ifStmt.Test.Location,
                             "BOOL expression expected");
            }
            ifStmt.ThenPart.Accept(this);
            if (ifStmt.ElsePart != null)
                ifStmt.ElsePart.Accept(this);
        }

        public override void VisitReturn(ReturnStatement ret)
        {
            if (currentIter != null) {
                report.Error(ret.Location,
                             "`return' statements may not appear in iters.");
                return;
            }
            CheckReturnValue(ret);
        }

        protected virtual void CheckReturnValue(ReturnStatement ret)
        {
            if (ret.Value == null) {
                if (!currentRoutine.ReturnType.IsNull) {
                    report.Error(ret.Location,
                                 "return value should be provided");
                    return;
                }
            }
            else {
                if (currentRoutine.ReturnType.IsNull) {
                    report.Error(ret.Location,
                                 "return value should not be provided");
                    return;
                }
                ret.Value.Accept(this);
                if (ret.Value is VoidExpression) {
                    ret.Value.NodeType = currentRoutine.ReturnType.NodeType;
                }
                else {
                    TypeData expectedType = currentRoutine.ReturnType.NodeType;
                    if (!ret.Value.NodeType.IsSubtypeOf(expectedType)) {
                        report.Error(ret.Location,
                                     "the type of the destination: {0} is " +
                                     "not a supertype of {1}",
                                     expectedType.Name,
                                     ret.Value.NodeType.FullName);
                        return;
                    }
                }
            }
        }

        public override void VisitCase(CaseStatement caseStmt)
        {
            caseStmt.Test.Accept(this);
            caseStmt.TestName = getTemporallyName();
            localVariableStack.AddLocal(caseStmt.TestName,
                                        caseStmt.Test.NodeType);
            foreach (CaseWhen when in caseStmt.WhenPartList) {
                foreach (Expression value in when.ValueList) {
                    LocalExpression test =
                        new LocalExpression(caseStmt.TestName, value.Location);
                    ModalExpression arg =
                        new ModalExpression(ArgumentMode.In, value,
                                            value.Location);
                    TypedNodeList args = new TypedNodeList(arg);
                    CallExpression call =
                        new CallExpression(test, "is_eq", args, value.Location);
                    call.Accept(this);
                    if (call.NodeType == typeManager.BoolType) {
                        when.TestCallList.Append(call);
                    }
                    else {
                        if (call.NodeType != null) {
                            report.Error(value.Location,
                                         "no match for {0}::is_eq({1}):BOOL",
                                         caseStmt.Test.NodeType.FullName,
                                         value.NodeType.FullName);
                        }
                    }
                }
                when.ThenPart.Accept(this);
            }
            if (caseStmt.ElsePart != null)
                caseStmt.ElsePart.Accept(this);
        }

        public override void VisitTypecase(TypecaseStatement typecase)
        {
            typecase.Variable.Accept(this);
            if (typecase.Variable.NodeType == null)
                return;
            if (typecase.Variable.Call != null) {
                report.Error(typecase.Variable.Location,
                             "typecase variable must be a local variable " +
                             "or an argument");
                return;
            }
            TypeData varType = typecase.Variable.NodeType;
            foreach (TypecaseWhen when in typecase.WhenPartList) {
                when.TypeSpecifier.Accept(this);
                if (when.TypeSpecifier.NodeType == null)
                    continue;
                TypeData localType;
                if (varType.IsSubtypeOf(when.TypeSpecifier.NodeType)) {
                    localType = typecase.Variable.NodeType;
                }
                else {
                    localType = when.TypeSpecifier.NodeType;
                }
                when.LocalVariable =
                    localVariableStack.CreateLocal(typecase.Variable.Name,
                                                   localType,
                                                   true);
                when.ThenPart.LocalVariables.Add(typecase.Variable.Name,
                                                 when.LocalVariable);
                when.ThenPart.Accept(this);
            }
            if (typecase.ElsePart != null)
                typecase.ElsePart.Accept(this);
        }

        public override void VisitLoop(LoopStatement loop)
        {
            LoopStatement prevLoop = currentLoop;
            currentLoop = loop;
            loop.StatementList.Accept(this);
            currentLoop = prevLoop;
        }

        public override void VisitYield(YieldStatement yield)
        {
            if (currentIter == null) {
                report.Error(yield.Location,
                             "`yield' statements may not appear in routines.");
                return;
            }
            CheckReturnValue(yield);
            yield.ResumePoint.Index = currentIter.ResumePoints.Count;
            currentIter.ResumePoints.Add(yield.ResumePoint);
        }

        public override void VisitProtect(ProtectStatement protect)
        {
            protect.StatementList.Accept(this);
            foreach (ProtectWhen when in protect.WhenPartList) {
                when.TypeSpecifier.Accept(this);
                TypeData prevExceptionType = currentExceptionType;
                currentExceptionType = when.TypeSpecifier.NodeType;
                when.ThenPart.Accept(this);
                currentExceptionType = prevExceptionType;
            }
            if (protect.ElsePart != null) {
                TypeData prevExceptionType = currentExceptionType;
                currentExceptionType =
                    typeManager.GetTypeData(typeof(Exception));
                protect.ElsePart.Accept(this);
                currentExceptionType = prevExceptionType;
            }
            if (protect.WhenPartList.Length == 0 &&
                protect.ElsePart == null) {
                report.Error(protect.Location,
                             "`protect' statements must have `when' or `else'");
            }
        }

        public override void VisitRaise(RaiseStatement raise)
        {
            raise.Value.Accept(this);
            if (raise.Value.NodeType != typeManager.StrType &&
                !raise.Value.NodeType.IsSubtypeOf(typeManager.ExceptionType)) {
                report.Error(raise.Location, "exception expected");
            }
        }

        public override void
        VisitExpressionStatement(ExpressionStatement exprStmt)
        {
            exprStmt.Expression.Accept(this);
            if (exprStmt.Expression.NodeType != null &&
                !exprStmt.Expression.NodeType.IsVoid) {
                report.Error(exprStmt.Location,
                             "expressions used as statements may not have " +
                             "return values");
            }
        }

        public override void VisitBoolLiteral(BoolLiteralExpression boolLiteral)
        {
            boolLiteral.NodeType = typeManager.BoolType;
        }

        public override void VisitIntLiteral(IntLiteralExpression intLiteral)
        {
            intLiteral.NodeType = typeManager.IntType;
        }

        public override void VisitCharLiteral(CharLiteralExpression charLiteral)
        {
            charLiteral.NodeType = typeManager.CharType;
        }

        public override void VisitStrLiteral(StrLiteralExpression strLiteral)
        {
            strLiteral.NodeType = typeManager.StrType;
        }

        public override void VisitSelf(SelfExpression self)
        {
            self.NodeType = typeManager.GetTypeData(currentClass.TypeBuilder);
        }

        public override void VisitLocal(LocalExpression localExpr)
        {
            if (!inSharedContext) {
                Argument arg = currentRoutine.GetArgument(localExpr.Name);
                if (arg != null) {
                    if (arg.NodeType.IsByRef)
                        localExpr.NodeType = arg.NodeType.ElementType;
                    else
                        localExpr.NodeType = arg.NodeType;
                    return;
                }
                LocalVariable local =
                    localVariableStack.GetLocal(localExpr.Name);
                if (local != null) {
                    localExpr.NodeType = local.LocalType;
                    return;
                }
            }
            Expression self = new SelfExpression(localExpr.Location);
            localExpr.Call = new CallExpression(self, localExpr.Name,
                                                new TypedNodeList(),
                                                localExpr.Location);
            localExpr.Call.HasValue = localExpr.HasValue;
            localExpr.Call.Accept(this);
            localExpr.NodeType = localExpr.Call.NodeType;
        }

        public override void VisitCall(CallExpression call)
        {
            TypeData receiverType;
            if (call.Receiver != null) {
                call.Receiver.Accept(this);
                receiverType = call.Receiver.NodeType;
            }
            else {
                call.TypeSpecifier.Accept(this);
                receiverType = call.TypeSpecifier.NodeType;
            }
            if (receiverType == null) {
                report.Error(call.Location, "no match for {0}", call.Name);
                return;
            }
            call.Arguments.Accept(this);
#if false
            MethodInfo method;
            TypeData builtinMethodContainer =
                typeManager.GetBuiltinMethodContainer(receiverType);
            if (builtinMethodContainer != null) {
                try {
                    Expression expr = new VoidExpression(Location.Null);
                    expr.NodeType = receiverType;
                    ModalExpression arg = new ModalExpression(ArgumentMode.In,
                                                              expr,
                                                              Location.Null);
                    TypedNodeList args = new TypedNodeList(arg);
                    args.Append(call.Arguments.First);
                    method = LookupMethod(builtinMethodContainer,
                                          call.Name, args,
                                          call.HasValue);
                    call.IsBuiltin = true;
                    SetupMethod(call, method, receiverType);
                    return;
                }
                catch (LookupMethodException e) {
                }
            }
            try {
                method = LookupMethod(receiverType,
                                      call.Name, call.Arguments,
                                      call.HasValue);
                SetupMethod(call, method, receiverType);
            }
#else
            try {
                MethodData method = receiverType.LookupMethod(call.Name,
                                                              call.Arguments,
                                                              call.HasValue);
                call.IsBuiltin = method.IsBuiltin;
                SetupMethod(call, method.MethodInfo, receiverType);
            }
#endif
            catch (LookupMethodException e) {
                string routInfo = receiverType.FullName +
                    "::" + call.Name;
                if (call.Arguments.Length > 0) {
                    routInfo += "(";
                    foreach (ModalExpression arg in call.Arguments) {
                        if (arg != call.Arguments.First)
                            routInfo += ",";
                        routInfo += arg.NodeType.FullName;
                    }
                    routInfo += ")";
                }
                if (call.HasValue)
                    routInfo += ":_";
                report.Error(call.Location,
                             "{0} for {1}", e.Message, routInfo);
            }
        }

        public override void VisitIterCall(IterCallExpression iter)
        {
            if (currentLoop == null) {
                report.Error(iter.Location,
                             "iterator calls must appear inside loops");
                return;
            }
            TypeData receiverType;
            if (iter.Receiver != null) {
                iter.Receiver.Accept(this);
                receiverType = iter.Receiver.NodeType;
            }
            else {
                iter.TypeSpecifier.Accept(this);
                receiverType = iter.TypeSpecifier.NodeType;
            }
            if (receiverType == null) {
                report.Error(iter.Location, "no match for {0}", iter.Name);
                return;
            }
            iter.Arguments.Accept(this);
            MethodInfo method;
            TypeData builtinMethodContainer =
                typeManager.GetBuiltinMethodContainer(receiverType);
            if (builtinMethodContainer != null) {
                try {
                    Expression expr = new VoidExpression(Location.Null);
                    expr.NodeType = receiverType;
                    ModalExpression arg = new ModalExpression(ArgumentMode.In,
                                                              expr,
                                                              Location.Null);
                    TypedNodeList args = new TypedNodeList(arg);
                    args.Append(iter.Arguments.First);
                    method = LookupMethod(builtinMethodContainer,
                                          iter.Name, args,
                                          iter.HasValue);
                    iter.IsBuiltin = true;
                    SetupIter(iter, method, receiverType);
                    return;
                }
                catch (LookupMethodException e) {
                }
            }
            try {
                method = LookupMethod(receiverType,
                                      iter.Name, iter.Arguments,
                                      iter.HasValue);
                SetupIter(iter, method, receiverType);
            }
            catch (LookupMethodException e) {
                string iterInfo = receiverType.FullName +
                    "::" + iter.Name;
                if (iter.Arguments.Length > 0) {
                    iterInfo += "(";
                    foreach (ModalExpression arg in iter.Arguments) {
                        if (arg != iter.Arguments.First)
                            iterInfo += ",";
                        iterInfo += arg.NodeType.FullName;
                    }
                    iterInfo += ")";
                }
                if (iter.HasValue)
                    iterInfo += ":_";
                report.Error(iter.Location,
                             "{0} for {1}", e.Message, iterInfo);
            }
        }

        public override void VisitModalExpression(ModalExpression modalExpr)
        {
            modalExpr.Expression.Accept(this);
            if (modalExpr.Mode == ArgumentMode.Out ||
                modalExpr.Mode == ArgumentMode.InOut) {
                LocalExpression local = modalExpr.Expression as LocalExpression;
                if (local == null || local.Call != null) {
                    report.Error(modalExpr.Location,
                                 "out/inout argument must be " +
                                 "a local variable or an argument");
                    return;
                }
            }
            modalExpr.NodeType = modalExpr.Expression.NodeType;
        }

        public override void VisitVoid(VoidExpression voidExpr)
        {
            voidExpr.NodeType = null;
        }

        public override void VisitVoidTest(VoidTestExpression voidTest)
        {
            voidTest.Expression.Accept(this);
            voidTest.NodeType = typeManager.BoolType;
        }

        public override void VisitNew(NewExpression newExpr)
        {
            newExpr.TypeSpecifier.Accept(this);
            if (newExpr.TypeSpecifier.NodeType == null)
                return;
            newExpr.Arguments.Accept(this);
            TypeData type = newExpr.TypeSpecifier.NodeType;
            try {
                ConstructorInfo constructor =
                    LookupConstructor(type, newExpr.Arguments);
                SetupConstructor(newExpr, constructor, type);
            }
            catch (LookupMethodException e) {
                string ctorInfo =
                    type.FullName + "::.ctor";
                if (newExpr.Arguments.Length > 0) {
                    ctorInfo += "(";
                    foreach (ModalExpression arg in newExpr.Arguments) {
                        if (arg != newExpr.Arguments.First)
                            ctorInfo += ",";
                        ctorInfo += arg.NodeType.FullName;
                    }
                    ctorInfo += ")";
                }
                report.Error(newExpr.Location,
                             "{0} for {1}", e.Message, ctorInfo);
            }
        }

        public override void VisitAnd(AndExpression and)
        {
            VisitCond(and);
        }

        public override void VisitOr(OrExpression or)
        {
            VisitCond(or);
        }

        public override void VisitBreak(BreakExpression breakExpr)
        {
            if (currentLoop == null) {
                report.Error(breakExpr.Location,
                             "`break!', `while!', `until!' calls " +
                             "must appear inside loops");
                return;
            }
            breakExpr.NodeType = typeManager.VoidType;
        }

        public override void VisitException(ExceptionExpression exception)
        {
            if (currentExceptionType == null) {
                report.Error(exception.Location,
                             "`exception' expressions may only appear " +
                             "in `then' and `else' clauses of " +
                             "`protect' statements");
                return;
            }
            exception.NodeType = currentExceptionType;
        }

        protected virtual string getTemporallyName()
        {
            string name = "__temp_" + temporallyCount.ToString();
            temporallyCount++;
            return name;
        }

        protected virtual void VisitCond(ConditionalExpression cond)
        {
            cond.Left.Accept(this);
            cond.Right.Accept(this);
            if (cond.Left.NodeType != typeManager.BoolType ||
                cond.Right.NodeType != typeManager.BoolType) {
                report.Error(cond.Location,
                             "operand of `{0}' must be BOOL",
                             cond.Name);
                return;
            }
            cond.NodeType = typeManager.BoolType;
        }

        protected virtual MethodInfo LookupMethod(TypeData type,
                                                  string name,
                                                  TypedNodeList arguments,
                                                  bool hasReturnValue)
        {
            MethodInfo[] methods = typeManager.GetMethods(type);
            ArrayList candidates = new ArrayList();
            foreach (MethodInfo method in methods) {
                if (CheckMethod(method, name, arguments,
                                hasReturnValue))
                    candidates.Add(method);
            }
            if (candidates.Count == 0)
                throw new LookupMethodException("no match");
            if (candidates.Count == 1)
                return (MethodInfo) candidates[0];
            return (MethodInfo) SelectBestOverload(candidates, arguments);
        }

        protected virtual bool CheckMethod(MethodInfo method,
                                           string name,
                                           TypedNodeList arguments,
                                           bool hasReturnValue)
        {
            string methodName = typeManager.GetMethodName(method);
            if (methodName.ToLower() != name.ToLower())
                return false;
            Type returnType = typeManager.GetIterReturnType(method);
            if (returnType == null)
                returnType = method.ReturnType;
            if (hasReturnValue != (returnType != typeof(void)))
                return false;
            ParameterInfo[] parameters = typeManager.GetParameters(method);
            if (parameters.Length != arguments.Length)
                return false;
            int pos = 0;
            foreach (ModalExpression arg in arguments) {
                ParameterInfo param = parameters[pos++];
                TypeData paramType =
                    typeManager.GetTypeData(param.ParameterType);
                switch (arg.Mode) {
                case ArgumentMode.In:
                case ArgumentMode.Once:
                    if (paramType.IsByRef) {
                        return false;
                    }
                    if (arg.NodeType == null)
                        continue;
                    if (!arg.NodeType.IsSubtypeOf(paramType))
                        return false;
                    break;
                case ArgumentMode.Out:
                    if (!(paramType.IsByRef &&
                          !param.IsIn && param.IsOut))
                        return false;
                    TypeData eltType = paramType.ElementType;
                    if (!eltType.IsSubtypeOf(arg.NodeType) ||
                        eltType.IsValueType && !arg.NodeType.IsValueType)
                        return false;
                    break;
                case ArgumentMode.InOut:
                    if (!(paramType.IsByRef &&
                          param.IsIn && param.IsOut))
                        return false;
                    if (arg.NodeType != paramType.ElementType)
                        return false;
                    break;
                }
            }
            return true;
        }

        protected virtual ConstructorInfo
        LookupConstructor(TypeData type,
                          TypedNodeList arguments)
        {
            ConstructorInfo[] constructors =
                typeManager.GetConstructors(type.RawType);
            ArrayList candidates = new ArrayList();
            foreach (ConstructorInfo constructor in constructors) {
                if (CheckConstructor(constructor, arguments))
                    candidates.Add(constructor);
            }
            if (candidates.Count == 0)
                throw new LookupMethodException("no match");
            if (candidates.Count == 1)
                return (ConstructorInfo) candidates[0];
            return (ConstructorInfo) SelectBestOverload(candidates, arguments);
        }

        protected virtual bool CheckConstructor(ConstructorInfo constructor,
                                                TypedNodeList arguments)
        {
            ParameterInfo[] parameters = typeManager.GetParameters(constructor);
            if (parameters.Length != arguments.Length)
                return false;
            int pos = 0;
            foreach (ModalExpression arg in arguments) {
                ParameterInfo param = parameters[pos++];
                TypeData paramType =
                    typeManager.GetTypeData(param.ParameterType);
                switch (arg.Mode) {
                case ArgumentMode.In:
                    if (paramType.IsByRef) {
                        return false;
                    }
                    if (arg.NodeType == null)
                        continue;
                    if (!arg.NodeType.IsSubtypeOf(paramType))
                        return false;
                    break;
                case ArgumentMode.Out:
                    if (!(paramType.IsByRef &&
                          !param.IsIn && param.IsOut))
                        return false;
                    TypeData eltType = paramType.ElementType;
                    if (!eltType.IsSubtypeOf(arg.NodeType) ||
                        eltType.IsValueType && !arg.NodeType.IsValueType)
                        return false;
                    break;
                case ArgumentMode.InOut:
                    if (!(paramType.IsByRef &&
                          param.IsIn && param.IsOut))
                        return false;
                    if (arg.NodeType != paramType.ElementType)
                        return false;
                    break;
                }
            }
            return true;
        }

        protected virtual MethodBase SelectBestOverload(ArrayList candidates,
                                                        TypedNodeList arguments)
        {
            ArrayList winners = null;
            MethodBase firstMethod = (MethodBase) candidates[0];

            int pos = 0;
            foreach (ModalExpression arg in arguments) {
                if (arg.Mode == ArgumentMode.InOut) {
                    if (winners == null)
                        winners = candidates;
                }
                else {
                    ArrayList currentPosWinners = new ArrayList();
                    ParameterInfo[] firstMethodParameters =
                        typeManager.GetParameters(firstMethod);
                    Type t = firstMethodParameters[pos].ParameterType;
                    Type bestType;
                    if (t.IsByRef)
                        bestType = t.GetElementType();
                    else
                        bestType = t;
                    foreach (MethodBase method in candidates) {
                        ParameterInfo param =
                            typeManager.GetParameters(method)[pos];
                        switch (arg.Mode) {
                        case ArgumentMode.In:
                        case ArgumentMode.Once:
                            if (IsSubtype(param.ParameterType, bestType)) {
                                if (param.ParameterType != bestType) {
                                    bestType = param.ParameterType;
                                    currentPosWinners.Clear();
                                }
                                currentPosWinners.Add(method);
                            }
                            break;
                        case ArgumentMode.Out:
                            Type paramType =
                                param.ParameterType.GetElementType();
                            if (IsSubtype(bestType, paramType)) {
                                if (paramType != bestType) {
                                    bestType = paramType;
                                    currentPosWinners.Clear();
                                }
                                currentPosWinners.Add(method);
                            }
                            break;
                        }
                    }
                    if (winners == null) {
                        winners = currentPosWinners;
                    }
                    else {
                        ArrayList newWinners = new ArrayList();
                        foreach (MethodBase m in winners) {
                            if (currentPosWinners.Contains(m))
                                newWinners.Add(m);
                        }
                        winners = newWinners;
                    }
                }
                pos++;
            }
            if (winners == null || winners.Count == 0) {
                throw new LookupMethodException("no match");
            }
            if (winners.Count > 1) {
                throw new LookupMethodException("multiple matches");
            }
            return (MethodBase) winners[0];
        }

        protected virtual bool IsSubtype(Type type, Type supertype)
        {
            return typeManager.IsSubtype(type, supertype);
        }

        protected virtual void SetupMethod(CallExpression call,
                                           MethodInfo method,
                                           TypeData receiverType)
        {
            if (!method.IsPublic &&
                typeManager.GetTypeData(currentClass.TypeBuilder) !=
                receiverType) {
                report.Error(call.Location,
                             "cannot call private routine {0}",
                             call.Name);
                return;
            }

            call.Method = method;
            call.NodeType = typeManager.GetReturnType(method);
            ParameterInfo[] parameters = typeManager.GetParameters(method);
            int i = 0;
            foreach (ModalExpression arg in call.Arguments) {
                ParameterInfo param = parameters[i++];
                if (arg.NodeType == null) // void expression
                    arg.NodeType = typeManager.GetTypeData(param.ParameterType);
            }
            if (call.Receiver == null &&
                (call.IsBuiltin || !method.IsStatic)) {
                call.Receiver = new VoidExpression(call.Location);
                call.Receiver.NodeType = receiverType;
            }
        }

        protected virtual void SetupConstructor(NewExpression newExpr,
                                                ConstructorInfo constructor,
                                                TypeData type)
        {
            if (!constructor.IsPublic &&
                currentClass.TypeData != type) {
                report.Error(newExpr.Location,
                             "cannot call private constructor");
                return;
            }

            newExpr.Constructor = constructor;
            newExpr.NodeType = type;
            ParameterInfo[] parameters = typeManager.GetParameters(constructor);
            int i = 0;
            foreach (ModalExpression arg in newExpr.Arguments) {
                ParameterInfo param = parameters[i++];
                if (arg.NodeType == null) // void expression
                    arg.NodeType = typeManager.GetTypeData(param.ParameterType);
            }
        }

        protected virtual void SetupIter(IterCallExpression iter,
                                         MethodInfo method,
                                         TypeData receiverType)
        {
            if (!method.IsPublic &&
                currentClass.TypeData != receiverType) {
                report.Error(iter.Location,
                             "cannot call private iterator {0}",
                             iter.Name);
                return;
            }

            iter.Method = method;
            iter.NodeType = typeManager.GetReturnType(method);
            if (iter.Receiver == null &&
                (iter.IsBuiltin || !method.IsStatic)) {
                iter.Receiver = new VoidExpression(iter.Location);
                iter.Receiver.NodeType = receiverType;
            }

            TypeData iterType = typeManager.GetTypeData(method.ReturnType);

            string localName = getTemporallyName();
            iter.Local = localVariableStack.AddLocal(localName,
                                                     iterType);

            TypedNodeList moveNextArguments = new TypedNodeList();
            ModalExpression receiver =
                new ModalExpression(ArgumentMode.In,
                                    (Expression) iter.Receiver.Clone(),
                                    iter.Receiver.Location);
            ParameterInfo[] parameters = typeManager.GetParameters(method);
            int i;
            if (iter.IsBuiltin)
                i = 1;
            else
                i = 0;
            foreach (ModalExpression arg in iter.Arguments) {
                ParameterInfo param = parameters[i++];
                if (arg.NodeType == null) // void expression
                    arg.NodeType = typeManager.GetTypeData(param.ParameterType);
                ArgumentMode mode = typeManager.GetArgumentMode(param);
                if (mode != ArgumentMode.Once) {
                    moveNextArguments.Append((ModalExpression) arg.Clone());
                }
            }
            LocalExpression moveNextReceiver =
                new LocalExpression(iter.Local.Name, iter.Location);
            iter.MoveNext = new CallExpression(moveNextReceiver,
                                               "MoveNext",
                                               moveNextArguments,
                                               iter.Location);
            iter.MoveNext.Accept(this);
            if (iter.NodeType != typeManager.VoidType) {
                LocalExpression getCurrentReceiver =
                    new LocalExpression(iter.Local.Name, iter.Location);
                iter.GetCurrent = new CallExpression(getCurrentReceiver,
                                                     "GetCurrent",
                                                     new TypedNodeList(),
                                                     iter.Location);
                iter.GetCurrent.Accept(this);
            }
        }
    }

    public class LookupMethodException : Exception {
        public LookupMethodException(string message) : base(message) {}
    }
}
