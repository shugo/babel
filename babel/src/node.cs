/*
 * node.cs: abstract syntax tree nodes
 *
 * Copyright (C) 2003-2004 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Collections;

namespace Babel.Compiler {
    public abstract class Node : ICloneable, IEnumerable {
        protected Location location;
        protected Node next;

        public Node()
        {
            this.location = Location.Null;
        }

        public Node(Location location)
        {
            this.location = location;
        }

        public virtual Location Location {
            get { return location; }
            set { location = value; }
        }

        public abstract void Accept(NodeVisitor visitor);

        public virtual Node Next {
            get { return next; }
            set { next = value; }
        }

        public virtual Node Last {
            get {
                Node last = this;
                while (last.Next != null) {
                    last = last.Next;
                }
                return last;
            }
        }

        public virtual int Length {
            get {
                int length = 1;
                Node node = Next;
                while (node != null) {
                    length++;
                    node = node.Next;
                }
                return length;
            }
        }

        public virtual void Insert(Node node)
        {
            node.Next = Next;
            Next = node;
        }

        public virtual void Append(Node node)
        {
            Last.Next = node;
        }

        public virtual object Clone()
        {
            Node node = (Node) MemberwiseClone();
            node.next = null;
            return node;
        }

        public virtual IEnumerator GetEnumerator()
        {
            return new NodeEnumerator(this);
        }
    }

    public class NodeEnumerator : IEnumerator {
        protected Node first;
        protected Node current;

        public NodeEnumerator(Node node)
        {
            first = node;
            current = null;
        }

        public virtual bool MoveNext()
        {
            if (current == null) {
                if (first == null)
                    return false;
                current = first;
                return true;
            }
            if (current.Next == null)
                return false;
            current = current.Next;
            return true;
        }

        public virtual void Reset()
        {
            current = null;
        }

        public virtual Object Current {
            get { return current; }
        }
    }

    public class NodeList : ICloneable, IEnumerable {
        protected Node first;

        public NodeList()
        {
            first = null;
        }

        public NodeList(Node first)
        {
            this.first = first;
        }

        public virtual Node First {
            get { return first; }
        }

        public virtual Node Last {
            get {
                if (first == null)
                    return null;
                return first.Last;
            }
        }

        public virtual int Length {
            get {
                if (first == null)
                    return 0;
                return first.Length;
            }
        }

        public virtual void Append(Node node)
        {
            if (first == null) {
                first = node;
            }
            else {
                first.Append(node);
            }
        }

        public virtual void Accept(NodeVisitor visitor)
        {
            foreach (Node node in this) {
                node.Accept(visitor);
            }
        }

        public virtual object Clone()
        {
            NodeList list = (NodeList) MemberwiseClone();
            list.first = null;
            foreach (Node node in this) {
                list.Append((Node) node.Clone());
            }
            return list;
        }

        public virtual IEnumerator GetEnumerator()
        {
            return new NodeEnumerator(first);
        }
    }

    public abstract class CompositeNode : Node {
        protected NodeList children;

        public CompositeNode()
        {
            children = new NodeList();
        }

        public CompositeNode(Location location)
            : base(location)
        {
            children = new NodeList();
        }

        public virtual NodeList Children {
            get { return children; }
        }

        public virtual void AddChild(Node node)
        {
            children.Append(node);
        }

        public virtual void AcceptToChildren(NodeVisitor visitor)
        {
            foreach (Node child in children) {
                child.Accept(visitor);
            }
        }

        public override object Clone()
        {
            CompositeNode node = (CompositeNode) base.Clone();
            node.children = (NodeList) children.Clone();
            return node;
        }
    }

    public abstract class TypedNode : Node {
        TypeData nodeType;

        public TypedNode()
        {
            nodeType = null;
        }

        public TypedNode(Location location) : base(location)
        {
            nodeType = null;
        }

        public virtual TypeData NodeType {
            get { return nodeType; }
            set { nodeType = value; }
        }
        
        public virtual Type RawType {
            get { return nodeType.RawType; }
        }

        public override object Clone()
        {
            TypedNode node = (TypedNode) base.Clone();
            return node;
        }
    }

    public class TypedNodeList : NodeList {
        public TypedNodeList() {}
        public TypedNodeList(Node first) : base(first) {}

        public virtual Type[] NodeTypes
        {
            get {
                Type[] types = new Type[Length];
                int i = 0;
                foreach (TypedNode node in this) {
                    types[i++] = node.RawType;
                }
                return types;
            }
        }
    }

    public interface NodeVisitor
    {
        void VisitProgram(Program program);
        void VisitSourceFile(SourceFile sourceFile);
        void VisitClass(ClassDefinition cls);
        void VisitAbstractRoutine(AbstractRoutineSignature routine);
        void VisitAbstractIter(AbstractIterSignature iter);
        void VisitConst(ConstDefinition constDef);
        void VisitSharedAttr(SharedAttrDefinition attr);
        void VisitAttr(AttrDefinition attr);
        void VisitRoutine(RoutineDefinition routine);
        void VisitIter(IterDefinition iter);
        void VisitArgument(Argument argument);
        void VisitInclude(IncludeClause include);
        void VisitTypeSpecifier(TypeSpecifier typeSpecifier);
        void VisitStatementList(StatementList statementList);
        void VisitDeclaration(DeclarationStatement decl);
        void VisitAssign(AssignStatement assign);
        void VisitIf(IfStatement ifStmt);
        void VisitReturn(ReturnStatement ret);
        void VisitCase(CaseStatement caseStmt);
        void VisitTypecase(TypecaseStatement typecase);
        void VisitLoop(LoopStatement loop);
        void VisitYield(YieldStatement yield);
        void VisitQuit(QuitStatement quit);
        void VisitProtect(ProtectStatement protect);
        void VisitRaise(RaiseStatement raise);
        void VisitExpressionStatement(ExpressionStatement exprstmt);
        void VisitBoolLiteral(BoolLiteralExpression boolLiteral);
        void VisitIntLiteral(IntLiteralExpression intLiteral);
        void VisitCharLiteral(CharLiteralExpression charLiteral);
        void VisitStrLiteral(StrLiteralExpression strLiteral);
        void VisitSelf(SelfExpression self);
        void VisitLocal(LocalExpression local);
        void VisitCall(CallExpression call);
        void VisitIterCall(IterCallExpression iter);
        void VisitModalExpression(ModalExpression modalExpr);
        void VisitVoid(VoidExpression voidExpr);
        void VisitVoidTest(VoidTestExpression voidTest);
        void VisitNew(NewExpression newExpr);
        void VisitAnd(AndExpression and);
        void VisitOr(OrExpression or);
        void VisitBreak(BreakExpression breakExpr);
        void VisitException(ExceptionExpression exception);
    }

    public abstract class AbstractNodeVisitor : NodeVisitor {
        public virtual void VisitProgram(Program program) {}
        public virtual void VisitSourceFile(SourceFile sourceFile) {}
        public virtual void VisitClass(ClassDefinition cls) {}
        public virtual void VisitAbstractRoutine(AbstractRoutineSignature routine) {}
        public virtual void VisitAbstractIter(AbstractIterSignature iter) {}
        public virtual void VisitConst(ConstDefinition constDef) {}
        public virtual void VisitSharedAttr(SharedAttrDefinition sharedAttr) {}
        public virtual void VisitAttr(AttrDefinition attr) {}
        public virtual void VisitRoutine(RoutineDefinition routine) {}
        public virtual void VisitIter(IterDefinition iter) {}
        public virtual void VisitInclude(IncludeClause include) {}
        public virtual void VisitArgument(Argument argument) {}
        public virtual void VisitTypeSpecifier(TypeSpecifier typeSpecifier) {}
        public virtual void VisitStatementList(StatementList statementList) {}
        public virtual void VisitDeclaration(DeclarationStatement decl) {}
        public virtual void VisitAssign(AssignStatement assign) {}
        public virtual void VisitIf(IfStatement ifStmt) {}
        public virtual void VisitReturn(ReturnStatement ret) {}
        public virtual void VisitCase(CaseStatement caseStmt) {}
        public virtual void VisitTypecase(TypecaseStatement typecase) {}
        public virtual void VisitLoop(LoopStatement loop) {}
        public virtual void VisitYield(YieldStatement yield) {}
        public virtual void VisitQuit(QuitStatement quit) {}
        public virtual void VisitProtect(ProtectStatement protect) {}
        public virtual void VisitRaise(RaiseStatement raise) {}
        public virtual void VisitExpressionStatement(ExpressionStatement exprstmt) {}
        public virtual void VisitBoolLiteral(BoolLiteralExpression boolLiteral) {}
        public virtual void VisitIntLiteral(IntLiteralExpression intLiteral) {}
        public virtual void VisitCharLiteral(CharLiteralExpression charLiteral) {}
        public virtual void VisitStrLiteral(StrLiteralExpression strLiteral) {}
        public virtual void VisitSelf(SelfExpression self) {}
        public virtual void VisitLocal(LocalExpression local) {}
        public virtual void VisitCall(CallExpression call) {}
        public virtual void VisitIterCall(IterCallExpression iter) {}
        public virtual void VisitModalExpression(ModalExpression modalExpr) {}
        public virtual void VisitVoid(VoidExpression voidExpr) {}
        public virtual void VisitVoidTest(VoidTestExpression voidTest) {}
        public virtual void VisitNew(NewExpression newExpr) {}
        public virtual void VisitAnd(AndExpression and) {}
        public virtual void VisitOr(OrExpression or) {}
        public virtual void VisitBreak(BreakExpression breakExpr) {}
        public virtual void VisitException(ExceptionExpression exception) {}
    }
}
