/*
 * typecheck.cs: check types
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
    public class TypeCheckingVisitor : AbstractNodeVisitor
    {
        Program program;
        TypeManager typeManager;
        Report report;
        Hashtable builtinRoutineContainers;
        ClassDefinition currentClass;
        RoutineDefinition currentRoutine;
        LocalVariableStack localVariableStack;
        int temporallyCount;
        Type currentExceptionType;
        bool inSharedContext;

        const string temporallyPrefix = "_$temp$_";

        public TypeCheckingVisitor(Report report)
        {
            this.report = report;
            inSharedContext = false;
            InitBuiltinRoutines();
        }

        protected void InitBuiltinRoutines()
        {
            builtinRoutineContainers = new Hashtable();
            builtinRoutineContainers.Add(typeof(bool),
                                         typeof(Babel.Sather.Base.BOOL));
            builtinRoutineContainers.Add(typeof(int),
                                         typeof(Babel.Sather.Base.INT));
            builtinRoutineContainers.Add(typeof(string),
                                         typeof(Babel.Sather.Base.STR));
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
            localVariableStack = new LocalVariableStack();
            temporallyCount = 0;
            routine.StatementList.Accept(this);
        }

        public override void VisitTypeSpecifier(TypeSpecifier typeSpecifier)
        {
            if (typeSpecifier.Kind == TypeKind.Same) {
                typeSpecifier.NodeType = currentClass.TypeBuilder;
                return;
            }
            Type type = typeManager.GetPredefinedType(typeSpecifier.Name);
            if (type != null) {
                typeSpecifier.NodeType = type;
                return;
            }
            ClassDefinition cls = typeManager.GetClass(typeSpecifier.Name);
            if (cls == null) {
                report.Error(typeSpecifier.Location,
                             "there is no class named {0}", typeSpecifier.Name);
                return;
            }
            typeSpecifier.NodeType = cls.TypeBuilder;
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
                             arg.Name, arg.NodeType);
                return;
            }
            LocalVariable local = localVariableStack.Get(decl.Name);
            if (local != null) {
                report.Error(decl.Location,
                             "this local variable declaration is " +
                             "in the scope of {0}:{1} which has the same name",
                             local.Name, local.LocalType);
                return;
            }
            Type type;
            if (!decl.TypeSpecifier.IsNull()) {
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
            localVariableStack.Add(decl.Name, type);
        }

        public override void VisitAssign(AssignStatement assign)
        {
            Type lhsType;
            Argument arg = currentRoutine.GetArgument(assign.Name);
            if (arg != null) {
                lhsType = arg.NodeType;
            }
            else {
                LocalVariable local = localVariableStack.Get(assign.Name);
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
                    lhsType =
                        assign.Call.Method.GetParameters()[0].ParameterType;
                }
            }
            if (assign.Value is VoidExpression) {
                assign.Value.NodeType = lhsType;
            }
            else {
                // NodeType may be already set by DeclarationStatement
                if (assign.Value.NodeType == null)
                    assign.Value.Accept(this);
                if (!IsSubtype(assign.Value.NodeType, lhsType)) {
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
            if (ifStmt.Test.NodeType != typeof(bool)) {
                report.Error(ifStmt.Test.Location,
                             "BOOL expression expected");
            }
            ifStmt.ThenPart.Accept(this);
            if (ifStmt.ElsePart != null)
                ifStmt.ElsePart.Accept(this);
        }

        public override void VisitReturn(ReturnStatement ret)
        {
            if (ret.Value == null) {
                if (!currentRoutine.ReturnType.IsNull()) {
                    report.Error(ret.Location,
                                 "return value should be provided");
                    return;
                }
            }
            else {
                if (currentRoutine.ReturnType.IsNull()) {
                    report.Error(ret.Location,
                                 "no return value should be provided");
                    return;
                }
                ret.Value.Accept(this);
                if (ret.Value is VoidExpression) {
                    ret.Value.NodeType = currentRoutine.ReturnType.NodeType;
                }
                else {
                    Type expectedType = currentRoutine.ReturnType.NodeType;
                    if (!IsSubtype(ret.Value.NodeType, expectedType)) {
                        report.Error(ret.Location,
                                     "the type of the destination: {0} is " +
                                     "not a supertype of {1}",
                                     expectedType.Name, ret.Value.NodeType);
                        return;
                    }
                }
            }
        }

        public override void VisitCase(CaseStatement caseStmt)
        {
            caseStmt.Test.Accept(this);
            caseStmt.TestName = getTemporallyName();
            localVariableStack.Add(caseStmt.TestName, caseStmt.Test.NodeType);
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
                    if (call.NodeType == typeof(bool)) {
                        when.TestCallList.Append(call);
                    }
                    else {
                        if (call.NodeType != null) {
                            string receiverType =
                                typeManager.GetTypeName(caseStmt.Test.NodeType);
                            string argType =
                                typeManager.GetTypeName(value.NodeType);
                            report.Error(value.Location,
                                         "no match for {0}::is_eq({1}):BOOL",
                                         receiverType, argType);
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
            foreach (TypecaseWhen when in typecase.WhenPartList) {
                when.TypeSpecifier.Accept(this);
                if (when.TypeSpecifier.NodeType == null)
                    continue;
                if (IsSubtype(typecase.Variable.NodeType,
                               when.TypeSpecifier.NodeType)) {
                    when.LocalVariable =
                        new LocalVariable(typecase.Variable.Name,
                                          typecase.Variable.NodeType,
                                          true);
                }
                else {
                    when.LocalVariable =
                        new LocalVariable(typecase.Variable.Name,
                                          when.TypeSpecifier.NodeType,
                                          true);
                }
                when.ThenPart.LocalVariables.Add(typecase.Variable.Name,
                                                 when.LocalVariable);
                when.ThenPart.Accept(this);
            }
            if (typecase.ElsePart != null)
                typecase.ElsePart.Accept(this);
        }

        public override void VisitLoop(LoopStatement loop)
        {
            loop.StatementList.Accept(this);
        }

        public override void VisitProtect(ProtectStatement protect)
        {
            protect.StatementList.Accept(this);
            foreach (ProtectWhen when in protect.WhenPartList) {
                when.TypeSpecifier.Accept(this);
                Type prevExceptionType = currentExceptionType;
                currentExceptionType = when.TypeSpecifier.NodeType;
                when.ThenPart.Accept(this);
                currentExceptionType = prevExceptionType;
            }
            if (protect.ElsePart != null) {
                Type prevExceptionType = currentExceptionType;
                currentExceptionType = typeof(Exception);
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
            if (raise.Value.NodeType != typeof(string) &&
                !IsSubtype(raise.Value.NodeType, typeof(Exception))) {
                report.Error(raise.Location, "exception expected");
            }
        }

        public override void VisitExpressionStatement(ExpressionStatement exprStmt)
        {
            exprStmt.Expression.Accept(this);
            if (exprStmt.Expression.NodeType != null &&
                exprStmt.Expression.NodeType != typeof(void)) {
                report.Error(exprStmt.Location,
                             "expressions used as statements may not have return values");
            }
        }

        public override void VisitBoolLiteral(BoolLiteralExpression boolLiteral)
        {
            boolLiteral.NodeType = typeof(bool);
        }

        public override void VisitIntLiteral(IntLiteralExpression intLiteral)
        {
            intLiteral.NodeType = typeof(int);
        }

        public override void VisitCharLiteral(CharLiteralExpression charLiteral)
        {
            charLiteral.NodeType = typeof(char);
        }

        public override void VisitStrLiteral(StrLiteralExpression strLiteral)
        {
            strLiteral.NodeType = typeof(string);
        }

        public override void VisitSelf(SelfExpression self)
        {
            self.NodeType = currentClass.TypeBuilder;
        }

        public override void VisitLocal(LocalExpression localExpr)
        {
            if (!inSharedContext) {
                Argument arg = currentRoutine.GetArgument(localExpr.Name);
                if (arg != null) {
                    localExpr.NodeType = arg.NodeType;
                    return;
                }
                LocalVariable local = localVariableStack.Get(localExpr.Name);
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
            Type receiverType;
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
            MethodInfo method;
            Type builtinRoutineContainer =
                (Type) builtinRoutineContainers[receiverType];
            if (builtinRoutineContainer != null) {
                try {
                    Expression expr = new VoidExpression(Location.Null);
                    expr.NodeType = receiverType;
                    ModalExpression arg = new ModalExpression(ArgumentMode.In,
                                                              expr,
                                                              Location.Null);
                    TypedNodeList args = new TypedNodeList(arg);
                    args.Append(call.Arguments.First);
                    method = LookupMethod(builtinRoutineContainer,
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
            catch (LookupMethodException e) {
                string routInfo = typeManager.GetTypeName(receiverType) +
                    "::" + call.Name;
                if (call.Arguments.Length > 0) {
                    routInfo += "(";
                    foreach (ModalExpression arg in call.Arguments) {
                        if (arg != call.Arguments.First)
                            routInfo += ",";
                        routInfo += typeManager.GetTypeName(arg.NodeType);
                    }
                    routInfo += ")";
                }
                if (call.HasValue)
                    routInfo += ":_";
                report.Error(call.Location,
                             "{0} for {1}", e.Message, routInfo);
            }
        }

        public override void VisitVoid(VoidExpression voidExpr)
        {
            voidExpr.NodeType = null;
        }

        public override void VisitVoidTest(VoidTestExpression voidTest)
        {
            voidTest.Expression.Accept(this);
            voidTest.NodeType = typeof(bool);
        }

        public override void VisitNew(NewExpression newExpr)
        {
            newExpr.NodeType = currentClass.TypeBuilder;
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
            breakExpr.NodeType = typeof(void);
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

        protected string getTemporallyName()
        {
            string name = temporallyPrefix + temporallyCount.ToString();
            temporallyCount++;
            return name;
        }

        protected void VisitCond(ConditionalExpression cond)
        {
            cond.Left.Accept(this);
            cond.Right.Accept(this);
            if (cond.Left.NodeType != typeof(bool) ||
                cond.Right.NodeType != typeof(bool)) {
                report.Error(cond.Location,
                             "operand of `{0}' must be BOOL",
                             cond.Name);
                return;
            }
        }

        protected MethodInfo LookupMethod(Type type,
                                          string name,
                                          TypedNodeList arguments,
                                          bool hasReturnValue)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance |
                                                   BindingFlags.Static |
                                                   BindingFlags.Public |
                                                   BindingFlags.NonPublic);
            ArrayList candidates = new ArrayList();
            foreach (MethodInfo method in methods) {
                if (ConfirmMethod(method, name, arguments, hasReturnValue))
                    candidates.Add(method);
            }
            if (candidates.Count == 0)
                throw new LookupMethodException("no match");
            if (candidates.Count == 1)
                return (MethodInfo) candidates[0];
            return SelectBestOverload(candidates, arguments);
        }

        protected bool ConfirmMethod(MethodInfo method,
                                     string name,
                                     TypedNodeList arguments,
                                     bool hasReturnValue)
        {
            if (method.Name != name)
                return false;
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != arguments.Length) {
                return false;
            }
            if (hasReturnValue) {
                if (method.ReturnType == typeof(void))
                    return false;
            }
            else {
                if (method.ReturnType != typeof(void))
                    return false;
            }
            int pos = 0;
            foreach (ModalExpression arg in arguments) {
                ParameterInfo param = parameters[pos++];
                switch (arg.Mode) {
                case ArgumentMode.In:
                    if (param.IsOut) {
                        return false;
                    }
                    if (arg.NodeType == null)
                        continue;
                    if (!IsSubtype(arg.NodeType, param.ParameterType))
                        return false;
                    break;
                case ArgumentMode.Out:
                    if (!(param.IsOut && !param.IsIn))
                        return false;
                    if (!IsSubtype(param.ParameterType, arg.NodeType))
                        return false;
                    break;
                case ArgumentMode.InOut:
                    if (!(param.IsIn && param.IsOut))
                        return false;
                    if (arg.NodeType != param.ParameterType)
                        return false;
                    break;
                }
            }
            return true;
        }

        protected MethodInfo SelectBestOverload(ArrayList candidates,
                                                TypedNodeList arguments)
        {
            ArrayList winners = null;
            MethodInfo firstMethod = (MethodInfo) candidates[0];

            int pos = 0;
            foreach (ModalExpression arg in arguments) {
                if (arg.Mode == ArgumentMode.InOut) {
                    if (winners == null)
                        winners = candidates;
                }
                else {
                    ArrayList currentPosWinners = new ArrayList();
                    Type bestType =
                        firstMethod.GetParameters()[pos].ParameterType;
                    foreach (MethodInfo method in candidates) {
                        ParameterInfo param = method.GetParameters()[pos];
                        switch (arg.Mode) {
                        case ArgumentMode.In:
                            if (IsSubtype(param.ParameterType, bestType)) {
                                if (param.ParameterType != bestType) {
                                    bestType = param.ParameterType;
                                    currentPosWinners.Clear();
                                }
                                currentPosWinners.Add(method);
                            }
                            break;
                        case ArgumentMode.Out: 
                            if (IsSubtype(bestType, param.ParameterType)) {
                                if (param.ParameterType != bestType) {
                                    bestType = param.ParameterType;
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
                        foreach (MethodInfo m in winners) {
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
            return (MethodInfo) winners[0];
        }

        protected bool IsSubtype(Type type, Type supertype)
        {
            return typeManager.IsSubtype(type, supertype);
        }

        protected void SetupMethod(CallExpression call, MethodInfo method,
                                   Type receiverType)
        {
            if (!method.IsPublic &&
                currentClass.TypeBuilder != receiverType) {
                report.Error(call.Location,
                             "cannot call private routine {0}",
                             call.Name);
                return;
            }

            call.Method = method;
            call.NodeType = method.ReturnType;
            ParameterInfo[] parameters = method.GetParameters();
            int i = 0;
            foreach (ModalExpression arg in call.Arguments) {
                ParameterInfo param = parameters[i++];
                if (arg.NodeType == null) // void expression
                    arg.NodeType = param.ParameterType;
            }
            if (call.Receiver == null &&
                (call.IsBuiltin || !method.IsStatic)) {
                call.Receiver = new VoidExpression(call.Location);
                call.Receiver.NodeType = receiverType;
            }
        }
    }

    public class LookupMethodException : Exception
    {
        public LookupMethodException(string message) : base(message) {}
    }
}
