/*
 * statement.cs: statements
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
    public abstract class Statement : Node
    {
        public Statement() : base() {}
        public Statement(Location location) : base(location) {}
    }

    public class StatementList : CompositeNode
    {
        Hashtable localVariables;

        public StatementList(Location location)
            : base(location)
        {
            localVariables = new Hashtable();
        }

        public Hashtable LocalVariables
        {
            get { return localVariables; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitStatementList(this);
        }

        public override object Clone()
        {
            StatementList statementList =
                (StatementList) base.Clone();
            statementList.localVariables = new Hashtable();
            return statementList;
        }
    }

    public class EmptyStatement : Statement
    {
        public EmptyStatement(Location location) : base(location) {}

        public override void Accept(NodeVisitor visitor)
        {
        }
    }

    public class DeclarationStatement : Statement
    {
        string name;
        TypeSpecifier typeSpecifier;

        public DeclarationStatement(string name, TypeSpecifier typeSpecifier,
                                    Location location)
            : base(location)
        {
            this.name = name;
            this.typeSpecifier = typeSpecifier;
        }

        public string Name
        {
            get { return name; }
        }

        public TypeSpecifier TypeSpecifier
        {
            get { return typeSpecifier; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitDeclaration(this);
        }

        public override object Clone()
        {
            DeclarationStatement decl =
                (DeclarationStatement) base.Clone();
            decl.typeSpecifier = (TypeSpecifier) typeSpecifier.Clone();
            return decl;
        }
    }

    public class AssignStatement : Statement
    {
        string name;
        Expression value;
        CallExpression call;

        public AssignStatement(string name, Expression value, Location location)
            : base(location)
        {
            this.name = name;
            this.value = value;
            call = null;
        }

        public string Name
        {
            get { return name; }
        }

        public Expression Value
        {
            get { return value; }
        }

        public CallExpression Call
        {
            get { return call; }
            set { call = value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            if (call == null) {
                visitor.VisitAssign(this);
            }
            else {
                call.Accept(visitor);
            }
        }

        public override object Clone()
        {
            AssignStatement assign =
                (AssignStatement) base.Clone();
            assign.value = (Expression) value.Clone();
            if (assign.call != null)
                assign.call = (CallExpression) call.Clone();
            return assign;
        }
    }

    public class IfStatement : Statement
    {
        Expression test;
        Node thenPart;
        Node elsePart;

        public IfStatement(Expression test,
                           Node thenPart,
                           Node elsePart,
                           Location location)
            : base(location)
        {
            this.test = test;
            this.thenPart = thenPart;
            this.elsePart = elsePart;
        }

        public Expression Test
        {
            get { return test; }
        }

        public Node ThenPart
        {
            get { return thenPart; }
        }

        public Node ElsePart
        {
            get { return elsePart; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitIf(this);
        }

        public override object Clone()
        {
            IfStatement ifStmt =
                (IfStatement) base.Clone();
            ifStmt.test = (Expression) test.Clone();
            ifStmt.thenPart = (Node) thenPart.Clone();
            if (ifStmt.elsePart != null)
                ifStmt.elsePart = (Node) elsePart.Clone();
            return ifStmt;
        }
    }

    public class ReturnStatement : Statement
    {
        Expression value;

        public ReturnStatement(Expression value, Location location)
            : base(location)
        {
            this.value = value;
        }

        public Expression Value
        {
            get { return value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitReturn(this);
        }

        public override object Clone()
        {
            ReturnStatement returnStmt =
                (ReturnStatement) base.Clone();
            returnStmt.value = (Expression) value.Clone();
            return returnStmt;
        }
    }

    public class CaseStatement : Statement
    {
        Expression test;
        NodeList whenPartList;
        StatementList elsePart;
        string testName;

        public CaseStatement(Expression test,
                             NodeList whenPartList,
                             StatementList elsePart,
                             Location location)
            : base(location)
        {
            this.test = test;
            this.whenPartList = whenPartList;
            this.elsePart = elsePart;
        }

        public Expression Test
        {
            get { return test; }
        }

        public NodeList WhenPartList
        {
            get { return whenPartList; }
        }

        public StatementList ElsePart
        {
            get { return elsePart; }
        }

        public string TestName
        {
            get { return testName; }
            set { testName = value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitCase(this);
        }

        public override object Clone()
        {
            CaseStatement caseStmt =
                (CaseStatement) base.Clone();
            caseStmt.test = (Expression) test.Clone();
            caseStmt.whenPartList = (NodeList) whenPartList.Clone();
            if (caseStmt.elsePart != null)
                caseStmt.elsePart = (StatementList) elsePart.Clone();
            return caseStmt;
        }
    }

    public class CaseWhen : Node
    {
        TypedNodeList valueList;
        StatementList thenPart;
        TypedNodeList testCallList;

        public CaseWhen(TypedNodeList valueList,
                        StatementList thenPart,
                        Location location)
            : base(location)
        {
            this.valueList = valueList;
            this.thenPart = thenPart;
            this.testCallList = new TypedNodeList();
        }

        public TypedNodeList ValueList
        {
            get { return valueList; }
        }

        public StatementList ThenPart
        {
            get { return thenPart; }
        }

        public TypedNodeList TestCallList
        {
            get { return testCallList; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            valueList.Accept(visitor);
            thenPart.Accept(visitor);
        }

        public override object Clone()
        {
            CaseWhen when =
                (CaseWhen) base.Clone();
            when.valueList = (TypedNodeList) valueList.Clone();
            when.thenPart = (StatementList) thenPart.Clone();
            when.testCallList = (TypedNodeList) testCallList.Clone();
            return when;
        }
    }

    public class TypecaseStatement : Statement
    {
        LocalExpression variable;
        NodeList whenPartList;
        StatementList elsePart;

        public TypecaseStatement(LocalExpression variable,
                                 NodeList whenPartList,
                                 StatementList elsePart,
                                 Location location)
            : base(location)
        {
            this.variable = variable;
            this.whenPartList = whenPartList;
            this.elsePart = elsePart;
        }

        public LocalExpression Variable
        {
            get { return variable; }
        }

        public NodeList WhenPartList
        {
            get { return whenPartList; }
        }

        public StatementList ElsePart
        {
            get { return elsePart; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitTypecase(this);
        }

        public override object Clone()
        {
            TypecaseStatement typecase =
                (TypecaseStatement) base.Clone();
            typecase.variable = (LocalExpression) variable.Clone();
            typecase.whenPartList = (NodeList) whenPartList.Clone();
            if (typecase.elsePart != null)
                typecase.elsePart = (StatementList) elsePart.Clone();
            return typecase;
        }
    }

    public class TypecaseWhen : Node
    {
        TypeSpecifier typeSpecifier;
        StatementList thenPart;
        LocalVariable localVariable;

        public TypecaseWhen(TypeSpecifier typeSpecifier,
                            StatementList thenPart,
                            Location location)
            : base(location)
        {
            this.typeSpecifier = typeSpecifier;
            this.thenPart = thenPart;
            localVariable = null;
        }

        public TypeSpecifier TypeSpecifier
        {
            get { return typeSpecifier; }
        }

        public StatementList ThenPart
        {
            get { return thenPart; }
        }

        public LocalVariable LocalVariable
        {
            get { return localVariable; }
            set { localVariable = value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            typeSpecifier.Accept(visitor);
            thenPart.Accept(visitor);
        }

        public override object Clone()
        {
            TypecaseWhen when =
                (TypecaseWhen) base.Clone();
            when.typeSpecifier = (TypeSpecifier) typeSpecifier.Clone();
            when.thenPart = (StatementList) thenPart.Clone();
            return when;
        }
    }

    public class LoopStatement : Statement
    {
        StatementList statementList;
        Label endLabel;

        public LoopStatement(StatementList statementList,
                             Location location)
            : base(location)
        {
            this.statementList = statementList;
        }

        public StatementList StatementList
        {
            get { return statementList; }
        }

        public Label EndLabel
        {
            get { return endLabel; }
            set { endLabel = value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitLoop(this);
        }

        public override object Clone()
        {
            LoopStatement loop =
                (LoopStatement) base.Clone();
            loop.statementList = (StatementList) statementList.Clone();
            return loop;
        }
    }

    public class ProtectStatement : Statement
    {
        StatementList statementList;
        NodeList whenPartList;
        StatementList elsePart;

        public ProtectStatement(StatementList statementList,
                                NodeList whenPartList,
                                StatementList elsePart,
                                Location location)
            : base(location)
        {
            this.statementList = statementList;
            this.whenPartList = whenPartList;
            this.elsePart = elsePart;
        }

        public StatementList StatementList
        {
            get { return statementList; }
        }

        public NodeList WhenPartList
        {
            get { return whenPartList; }
        }

        public StatementList ElsePart
        {
            get { return elsePart; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitProtect(this);
        }

        public override object Clone()
        {
            ProtectStatement protect =
                (ProtectStatement) base.Clone();
            protect.statementList = (StatementList) statementList.Clone();
            protect.whenPartList = (NodeList) whenPartList.Clone();
            if (protect.elsePart != null)
                protect.elsePart = (StatementList) elsePart.Clone();
            return protect;
        }
    }

    public class ProtectWhen : Node
    {
        TypeSpecifier typeSpecifier;
        StatementList thenPart;

        public ProtectWhen(TypeSpecifier typeSpecifier,
                           StatementList thenPart,
                           Location location)
            : base(location)
        {
            this.typeSpecifier = typeSpecifier;
            this.thenPart = thenPart;
        }

        public TypeSpecifier TypeSpecifier
        {
            get { return typeSpecifier; }
        }

        public StatementList ThenPart
        {
            get { return thenPart; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            typeSpecifier.Accept(visitor);
            thenPart.Accept(visitor);
        }

        public override object Clone()
        {
            ProtectWhen when =
                (ProtectWhen) base.Clone();
            when.typeSpecifier = (TypeSpecifier) typeSpecifier.Clone();
            when.thenPart = (StatementList) thenPart.Clone();
            return when;
        }
    }

    public class RaiseStatement : Statement
    {
        Expression value;

        public RaiseStatement(Expression value,
                              Location location)
            : base(location)
        {
            this.value = value;
        }

        public Expression Value
        {
            get { return value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitRaise(this);
        }

        public override object Clone()
        {
            RaiseStatement raiseStmt =
                (RaiseStatement) base.Clone();
            raiseStmt.value = (Expression) value.Clone();
            return raiseStmt;
        }
    }

    public class ExpressionStatement : Statement
    {
        Expression expression;

        public ExpressionStatement(Expression expression, Location location)
            : base(location)
        {
            expression.HasValue = false;
            this.expression = expression;
        }

        public Expression Expression
        {
            get { return expression; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitExpressionStatement(this);
        }

        public override object Clone()
        {
            ExpressionStatement expressionStmt =
                (ExpressionStatement) base.Clone();
            expressionStmt.expression = (Expression) expression.Clone();
            return expressionStmt;
        }
    }
}
