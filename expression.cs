/*
 * expression.cs: expressions
 *
 * Copyright (C) 2003 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

using Babel.Sather.Base;

namespace Babel.Sather.Compiler
{
    public abstract class Expression : TypedNode
    {
        bool hasValue;

        public Expression(Location location) : base(location)
        {
            hasValue = true;
        }

        public bool HasValue
        {
            get { return hasValue; }
            set { hasValue = value; }
        }
    }

    public abstract class LiteralExpression : Expression
    {
        public LiteralExpression(Location location) : base(location) {}

        public abstract object ValueAsObject
        {
            get;
        }
    }

    public class BoolLiteralExpression : LiteralExpression
    {
        bool value;

        public BoolLiteralExpression(bool value, Location location)
            : base(location)
        {
            this.value = value;
        }

        public bool Value
        {
            get { return value; }
        }

        public override object ValueAsObject
        {
            get { return value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitBoolLiteral(this);
        }
    }

    public class IntLiteralExpression : LiteralExpression
    {
        int value;

        public IntLiteralExpression(int value, Location location)
            : base(location)
        {
            this.value = value;
        }

        public int Value
        {
            get { return value; }
        }

        public override object ValueAsObject
        {
            get { return value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitIntLiteral(this);
        }
    }

    public class CharLiteralExpression : LiteralExpression
    {
        char value;

        public CharLiteralExpression(char value, Location location)
            : base(location)
        {
            this.value = value;
        }

        public char Value
        {
            get { return value; }
        }

        public override object ValueAsObject
        {
            get { return value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitCharLiteral(this);
        }
    }

    public class StrLiteralExpression : LiteralExpression
    {
        string value;

        public StrLiteralExpression(string value, Location location)
            : base(location)
        {
            this.value = value;
        }

        public string Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public override object ValueAsObject
        {
            get { return value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitStrLiteral(this);
        }
    }

    public class SelfExpression : Expression
    {
        public SelfExpression(Location location)
            : base(location) {}

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitSelf(this);
        }
    }

    public class LocalExpression : Expression
    {
        string name;
        CallExpression call;

        public LocalExpression(string name, Location location)
            : base(location)
        {
            this.name = name;
            call = null;
        }

        public string Name
        {
            get { return name; }
        }

        public CallExpression Call
        {
            get { return call; }
            set { call = value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            if (call == null) {
                visitor.VisitLocal(this);
            }
            else {
                call.Accept(visitor);
            }
        }

        public override object Clone()
        {
            LocalExpression expr =
                (LocalExpression) base.Clone();
            if (expr.call != null)
                expr.call = (CallExpression) call.Clone();
            return expr;
        }
    }

    public class CallExpression : Expression
    {
        protected Expression receiver;
        protected TypeSpecifier typeSpecifier;
        protected string name;
        protected TypedNodeList arguments;
        protected bool flip;
        protected MethodInfo method;
        protected bool isBuiltin;

        public CallExpression(Expression receiver,
                              string name,
                              TypedNodeList arguments,
                              bool flip,
                              Location location)
            : base(location)
        {
            this.receiver = receiver;
            this.name = name;
            this.arguments = arguments;
            this.flip = flip;
            this.isBuiltin = false;
        }

        public CallExpression(Expression receiver,
                              string name,
                              TypedNodeList arguments,
                              Location location)
            : this(receiver, name, arguments, false, location) {}

        public CallExpression(Expression receiver,
                              string name,
                              TypedNodeList arguments)
            : this(receiver, name, arguments, false, Location.Null) {}

        public CallExpression(TypeSpecifier typeSpecifier,
                              string name,
                              TypedNodeList arguments,
                              Location location)
            : base(location)
        {
            this.typeSpecifier = typeSpecifier;
            this.name = name;
            this.arguments = arguments;
        }


        public CallExpression(TypeSpecifier typeSpecifier,
                              string name,
                              TypedNodeList arguments)
            : this(typeSpecifier, name, arguments, Location.Null) {}

        public Expression Receiver
        {
            get { return receiver; }
            set { receiver = value; }
        }

        public TypeSpecifier TypeSpecifier
        {
            get { return typeSpecifier; }
        }

        public string Name
        {
            get { return name; }
        }

        public TypedNodeList Arguments
        {
            get { return arguments; }
        }

        public bool Flip
        {
            get { return flip; }
        }

        public MethodInfo Method
        {
            get { return method; }
            set { method = value; }
        }

        public bool IsBuiltin
        {
            get { return isBuiltin; }
            set { isBuiltin = value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitCall(this);
        }

        public override object Clone()
        {
            CallExpression expr =
                (CallExpression) base.Clone();
            if (receiver != null)
                expr.receiver = (Expression) receiver.Clone();
            if (typeSpecifier != null)
                expr.typeSpecifier = (TypeSpecifier) typeSpecifier.Clone();
            expr.arguments = (TypedNodeList) arguments.Clone();
            return expr;
        }
    }

    public class IterCallExpression : CallExpression
    {
        CallExpression createCall;
        CallExpression moveNextCall;
        CallExpression getCurrentCall;

        public IterCallExpression(Expression receiver,
                                  string name,
                                  TypedNodeList arguments,
                                  Location location)
            : base(receiver, name, arguments, location) {}

        public CallExpression CreateCall
        {
            get { return createCall; }
            set { createCall = value; }
        }

        public CallExpression MoveNextCall
        {
            get { return moveNextCall; }
            set { moveNextCall = value; }
        }

        public CallExpression GetCurrentCall
        {
            get { return getCurrentCall; }
            set { getCurrentCall = value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitIterCall(this);
        }
    }

    public class ModalExpression : Expression
    {
        ArgumentMode mode;
        Expression expression;

        public ModalExpression(ArgumentMode mode, Expression expression,
                               Location location)
            : base(location)
        {
            this.mode = mode;
            this.expression = expression;
        }

        public ArgumentMode Mode
        {
            get { return mode; }
        }

        public Expression Expression
        {
            get { return expression; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitModalExpression(this);
        }

        public override object Clone()
        {
            ModalExpression modalExpr =
                (ModalExpression) base.Clone();
            modalExpr.expression = (Expression) expression.Clone();
            return modalExpr;
        }
    }

    public class VoidExpression : Expression
    {
        public VoidExpression(Location location)
            : base(location) {}

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitVoid(this);
        }
    }

    public class VoidTestExpression : Expression
    {
        Expression expression;

        public VoidTestExpression(Expression expression, Location location)
            : base(location)
        {
            this.expression = expression;
        }

        public Expression Expression
        {
            get { return expression; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitVoidTest(this);
        }

        public override object Clone()
        {
            VoidTestExpression expr =
                (VoidTestExpression) base.Clone();
            expr.expression = (Expression) expression.Clone();
            return expr;
        }
    }

    public class NewExpression : Expression
    {
        public NewExpression(Location location)
            : base(location) {}

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitNew(this);
        }
    }

    public abstract class ConditionalExpression : Expression
    {
        Expression left;
        Expression right;

        public ConditionalExpression(Expression left, Expression right,
                                     Location location)
            : base(location)
        {
            this.left = left;
            this.right = right;
            NodeType = typeof(bool);
        }

        public Expression Left
        {
            get { return left; }
        }

        public Expression Right
        {
            get { return right; }
        }

        public abstract string Name
        {
            get;
        }

        public override object Clone()
        {
            ConditionalExpression expr =
                (ConditionalExpression) base.Clone();
            expr.left = (Expression) left.Clone();
            expr.right = (Expression) right.Clone();
            return expr;
        }
    }

    public class AndExpression : ConditionalExpression
    {
        public AndExpression(Expression left, Expression right,
                             Location location)
            : base(left, right, location) {}

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitAnd(this);
        }

        public override string Name
        {
            get { return "and"; }
        }
    }

    public class OrExpression : ConditionalExpression
    {
        public OrExpression(Expression left, Expression right,
                            Location location)
            : base(left, right, location) {}

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitOr(this);
        }

        public override string Name
        {
            get { return "or"; }
        }
    }

    public class BreakExpression : Expression
    {
        public BreakExpression(Location location)
            : base(location) {}

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitBreak(this);
        }
    }

    public class ExceptionExpression : Expression
    {
        public ExceptionExpression(Location location)
            : base(location) {}

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitException(this);
        }
    }

    public class IfExpression : Expression
    {
        IfStatement ifStatement;

        public IfExpression(Expression test,
                            Node thenPart,
                            Node elsePart,
                            Location location)
            : base(location)
        {
            ifStatement = new IfStatement(test, thenPart, elsePart, location);
            NodeType = typeof(void);
        }

        public override void Accept(NodeVisitor visitor)
        {
            ifStatement.Accept(visitor);
        }

        public override object Clone()
        {
            IfExpression expr =
                (IfExpression) base.Clone();
            expr.ifStatement = (IfStatement) ifStatement.Clone();
            return expr;
        }
    }
}
