/*
 * class.cs: classes and its elements
 *
 * Copyright (C) 2003 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Text;

using Babel.Sather.Base;

namespace Babel.Sather.Compiler
{
    public enum ClassKind
    {
        Reference,
        Abstract
    }

    public class ClassDefinition : CompositeNode, ICloneable
    {
        string name;
        ClassKind kind;
        TypedNodeList supertypes;
        TypeBuilder typeBuilder;
        ConstructorBuilder constructor;
        ConstructorBuilder staticConstructor;
        ILGenerator staticConstructorIL;

        public ClassDefinition(string name, ClassKind kind,
                               TypedNodeList supertypes,
                               Location location)
            : base(location)
        {
            this.name = name;
            this.kind = kind;
            this.supertypes = supertypes;
            typeBuilder = null;
            constructor = null;
            staticConstructor = null;
            staticConstructorIL = null;
        }

        public string Name
        {
            get { return name; }
        }

        public ClassKind Kind
        {
            get { return kind; }
        }

        public TypedNodeList Supertypes
        {
            get { return supertypes; }
        }

        public TypeBuilder TypeBuilder
        {
            get { return typeBuilder; }
            set { typeBuilder = value; }
        }

        public ConstructorBuilder Constructor
        {
            get { return constructor; }
            set { constructor = value; }
        }

        public ConstructorBuilder StaticConstructor
        {
            get { return staticConstructor; }
            set { staticConstructor = value; }
        }

        public ILGenerator StaticConstructorIL
        {
            get
            {
                if (staticConstructor != null &&
                    staticConstructorIL == null) {
                    staticConstructorIL =
                        staticConstructor.GetILGenerator();
                }
                return staticConstructorIL;
            }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitClass(this);
        }

        public override object Clone()
        {
            ClassDefinition cls = (ClassDefinition) base.Clone();
            cls.supertypes = (TypedNodeList) supertypes.Clone();
            cls.typeBuilder = null;
            cls.constructor = null;
            cls.staticConstructor = null;
            cls.staticConstructorIL = null;
            return cls;
        }
    }

    public class AbstractRoutineSignature : Node
    {
        protected string name;
        protected TypedNodeList arguments;
        protected TypeSpecifier returnType;
        protected Hashtable argumentTable;
        protected MethodBuilder methodBuilder;

        public AbstractRoutineSignature(string name,
                                        TypedNodeList arguments,
                                        TypeSpecifier returnType,
                                        Location location)
            : base(location)
        {
            this.name = name;
            this.arguments = arguments;
            this.returnType = returnType;
            this.methodBuilder = null;
            InitArguments();
        }

        protected virtual void InitArguments()
        {
            int index = 1;
            argumentTable = new Hashtable();
            foreach (Argument arg in arguments) {
                arg.Index = index++;
                argumentTable.Add(arg.Name, arg);
            }
        }

        public string Name
        {
            get { return name; }
        }

        public TypedNodeList Arguments
        {
            get { return arguments; }
        }

        public TypeSpecifier ReturnType
        {
            get { return returnType; }
        }

        public MethodBuilder MethodBuilder
        {
            get { return methodBuilder; }
            set { methodBuilder = value; }
        }

        public Argument GetArgument(string name)
        {
            return (Argument) argumentTable[name];
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitAbstractRoutine(this);
        }

        public override object Clone()
        {
            AbstractRoutineSignature rout =
                (AbstractRoutineSignature) base.Clone();
            rout.arguments = (TypedNodeList) arguments.Clone();
            rout.returnType = (TypeSpecifier) returnType.Clone();
            rout.methodBuilder = null;
            rout.InitArguments();
            return rout;
        }
    }

    public interface ClassElement
    {
        string Name { get; }
        void IncludeTo(ClassDefinition cls,
                       FeatureModifier featureModifier);
    }

    public enum ConstModifier
    {
        None,
        Private
    }

    public class ConstDefinition : Node, ClassElement
    {
        string name;
        TypeSpecifier typeSpecifier;
        object value;
        ConstModifier modifier;
        FieldBuilder fieldBuilder;
        MethodBuilder reader;

        public ConstDefinition(string name,
                               TypeSpecifier typeSpecifier,
                               object value,
                               ConstModifier modifier,
                               Location location)
            : base(location)
        {
            this.name = name;
            this.typeSpecifier = typeSpecifier;
            this.value = value;
            this.modifier = modifier;
            fieldBuilder = null;
            reader = null;
        }

        public string Name
        {
            get { return name; }
        }

        public TypeSpecifier TypeSpecifier
        {
            get { return typeSpecifier; }
        }

        public object Value
        {
            get { return value; }
        }

        public ConstModifier Modifier
        {
            get { return modifier; }
        }

        public FieldBuilder FieldBuilder
        {
            get { return fieldBuilder; }
            set { fieldBuilder = value; }
        }

        public MethodBuilder Reader
        {
            get { return reader; }
            set { reader = value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitConst(this);
        }

        public override object Clone()
        {
            ConstDefinition constDef =
                (ConstDefinition) base.Clone();
            constDef.typeSpecifier = (TypeSpecifier) typeSpecifier.Clone();
            constDef.fieldBuilder = null;
            constDef.reader = null;
            return constDef;
        }

        public void IncludeTo(ClassDefinition cls,
                              FeatureModifier featureModifier)
        {
            ConstDefinition constDef = (ConstDefinition) Clone();
            constDef.name = featureModifier.NewName;
            switch (featureModifier.NewModifier) {
            case IncludeModifier.None:
                constDef.modifier = ConstModifier.None;
                break;
            case IncludeModifier.Private:
                constDef.modifier = ConstModifier.Private;
                break;
            }
            cls.AddChild(constDef);
        }
    }

    public enum AttrModifier
    {
        None,
        Private,
        Readonly
    }

    public class AttrDefinition : Node, ClassElement
    {
        string name;
        TypeSpecifier typeSpecifier;
        AttrModifier modifier;
        FieldBuilder fieldBuilder;
        MethodBuilder reader;
        MethodBuilder writer;

        public AttrDefinition(string name,
                              TypeSpecifier typeSpecifier,
                              AttrModifier modifier,
                              Location location)
            : base(location)
        {
            this.name = name;
            this.typeSpecifier = typeSpecifier;
            this.modifier = modifier;
            fieldBuilder = null;
            reader = null;
            writer = null;
        }

        public string Name
        {
            get { return name; }
        }

        public TypeSpecifier TypeSpecifier
        {
            get { return typeSpecifier; }
        }

        public AttrModifier Modifier
        {
            get { return modifier; }
        }

        public FieldBuilder FieldBuilder
        {
            get { return fieldBuilder; }
            set { fieldBuilder = value; }
        }

        public MethodBuilder Reader
        {
            get { return reader; }
            set { reader = value; }
        }

        public MethodBuilder Writer
        {
            get { return writer; }
            set { writer = value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitAttr(this);
        }

        public override object Clone()
        {
            AttrDefinition attr =
                (AttrDefinition) base.Clone();
            attr.typeSpecifier = (TypeSpecifier) typeSpecifier.Clone();
            attr.fieldBuilder = null;
            attr.reader = null;
            attr.writer = null;
            return attr;
        }

        public void IncludeTo(ClassDefinition cls,
                              FeatureModifier featureModifier)
        {
            AttrDefinition attr = (AttrDefinition) Clone();
            attr.name = featureModifier.NewName;
            switch (featureModifier.NewModifier) {
            case IncludeModifier.None:
                attr.modifier = AttrModifier.None;
                break;
            case IncludeModifier.Private:
                attr.modifier = AttrModifier.Private;
                break;
            case IncludeModifier.Readonly:
                attr.modifier = AttrModifier.Readonly;
                break;
            }
            cls.AddChild(attr);
        }
    }


    public class SharedAttrDefinition : AttrDefinition
    {
        Expression value;

        public SharedAttrDefinition(string name,
                                    TypeSpecifier typeSpecifier,
                                    Expression value,
                                    AttrModifier modifier,
                                    Location location)
            : base(name, typeSpecifier, modifier, location)
        {
            this.value = value;
        }

        public Expression Value
        {
            get { return value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitSharedAttr(this);
        }

        public override object Clone()
        {
            SharedAttrDefinition attr =
                (SharedAttrDefinition) base.Clone();
            attr.value = (Expression) value.Clone();
            return attr;
        }
    }

    public enum RoutineModifier
    {
        None,
        Private
    }

    public class RoutineDefinition : AbstractRoutineSignature, ClassElement
    {
        StatementList statementList;
        RoutineModifier modifier;

        public RoutineDefinition(string name,
                                 TypedNodeList arguments,
                                 TypeSpecifier returnType,
                                 StatementList statementList,
                                 RoutineModifier modifier,
                                 Location location)
            : base(name, arguments, returnType, location)
        {
            this.statementList = statementList;
            this.modifier = modifier;
        }

        public StatementList StatementList
        {
            get { return statementList; }
        }

        public RoutineModifier Modifier
        {
            get { return modifier; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitRoutine(this);
        }

        public override object Clone()
        {
            RoutineDefinition rout =
                (RoutineDefinition) base.Clone();
            rout.statementList = (StatementList) statementList.Clone();
            return rout;
        }

        public void IncludeTo(ClassDefinition cls,
                              FeatureModifier featureModifier)
        {
            RoutineDefinition rout = (RoutineDefinition) Clone();
            rout.name = featureModifier.NewName;
            switch (featureModifier.NewModifier) {
            case IncludeModifier.None:
                rout.modifier = RoutineModifier.None;
                break;
            case IncludeModifier.Private:
                rout.modifier = RoutineModifier.Private;
                break;
            case IncludeModifier.Readonly:
                // FIXME
                break;
            }
            cls.AddChild(rout);
        }
    }

    public class IterDefinition : RoutineDefinition, ClassElement
    {
        TypeBuilder enumerator;
        FieldBuilder self;
        FieldBuilder current;
        FieldBuilder currentPosition;
        ConstructorBuilder constructor;
        MethodBuilder moveNext;
        MethodBuilder getCurrent;
        Hashtable localVariables;
        ArrayList resumePoints;
        TypedNodeList creatorArguments;
        TypedNodeList moveNextArguments;

        public IterDefinition(string name,
                              TypedNodeList arguments,
                              TypeSpecifier returnType,
                              StatementList statementList,
                              RoutineModifier modifier,
                              Location location)
            : base(name, arguments, returnType,
                   statementList, modifier, location)
        {
            enumerator = null;
            self = null;
            current = null;
            currentPosition = null;
            constructor = null;
            moveNext = null;
            getCurrent = null;
            resumePoints = new ArrayList();
            resumePoints.Add(new ResumePoint());
            localVariables = new Hashtable();
            InitArguments();
        }

        protected override void InitArguments()
        {
            int index = 1;
            int creatorPos = 1, moveNextPos = 1;
            creatorArguments = new TypedNodeList();
            moveNextArguments = new TypedNodeList();
            argumentTable = new Hashtable();
            foreach (Argument arg in Arguments) {
                arg.Index = index++;
                Argument ca = (Argument) arg.Clone();
                ca.Index = creatorPos++;
                creatorArguments.Append(ca);
                if (arg.Mode != ArgumentMode.Once) {
                    Argument ma = (Argument) arg.Clone();
                    ma.Index = moveNextPos++;
                    moveNextArguments.Append(ma);
                    argumentTable.Add(ma.Name, ma);
                }
            }
        }

        public TypeBuilder Enumerator
        {
            get { return enumerator; }
            set { enumerator = value; }
        }

        public FieldBuilder Self
        {
            get { return self; }
            set { self = value; }
        }

        public FieldBuilder Current
        {
            get { return current; }
            set { current = value; }
        }

        public FieldBuilder CurrentPosition
        {
            get { return currentPosition; }
            set { currentPosition = value; }
        }

        public ConstructorBuilder Constructor
        {
            get { return constructor; }
            set { constructor = value; }
        }

        public MethodBuilder MoveNext
        {
            get { return moveNext; }
            set { moveNext = value; }
        }

        public MethodBuilder GetCurrent
        {
            get { return getCurrent; }
            set { getCurrent = value; }
        }

        public ArrayList ResumePoints
        {
            get { return resumePoints; }
        }

        public Hashtable LocalVariables
        {
            get { return localVariables; }
        }

        public TypedNodeList CreatorArguments
        {
            get { return creatorArguments; }
        }

        public TypedNodeList MoveNextArguments
        {
            get { return moveNextArguments; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitIter(this);
        }
    }

    public class ResumePoint
    {
        int index;
        Label label;

        public ResumePoint()
        {
            index = 0;
        }

        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        public Label Label
        {
            get { return label; }
            set { label = value; }
        }
    }

    public class Argument : TypedNode
    {
        ArgumentMode mode;
        string name;
        int index;
        TypeSpecifier typeSpecifier;

        public Argument(ArgumentMode mode,
                        string name, TypeSpecifier typeSpecifier,
                        Location location)
            : base(location)
        {
            this.mode = mode;
            this.name = name;
            this.typeSpecifier = typeSpecifier;
        }

        public ArgumentMode Mode
        {
            get { return mode; }
        }

        public string Name
        {
            get { return name; }
        }

        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        public TypeSpecifier TypeSpecifier
        {
            get { return typeSpecifier; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitArgument(this);
        }

        public override object Clone()
        {
            Argument arg =
                (Argument) base.Clone();
            arg.typeSpecifier = (TypeSpecifier) typeSpecifier.Clone();
            return arg;
        }
    }

    public enum IncludeModifier
    {
        None,
        Private,
        Readonly,
        NoChange
    }

    public class IncludeClause : Node, ClassElement
    {
        TypeSpecifier typeSpecifier;
        IncludeModifier modifier;
        NodeList featureModifierList;

        public IncludeClause(TypeSpecifier typeSpecifier,
                             IncludeModifier modifier,
                             NodeList featureModifierList,
                             Location location)
            : base(location)
        {
            this.typeSpecifier = typeSpecifier;
            this.modifier = modifier;
            this.featureModifierList = featureModifierList;
        }

        public string Name
        {
            get { return ""; }
        }

        public TypeSpecifier TypeSpecifier
        {
            get { return typeSpecifier; }
        }

        public IncludeModifier Modifier
        {
            get { return modifier; }
        }

        public NodeList FeatureModifierList
        {
            get { return featureModifierList; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitInclude(this);
        }

        public override object Clone()
        {
            IncludeClause include =
                (IncludeClause) base.Clone();
            include.typeSpecifier = (TypeSpecifier) typeSpecifier.Clone();
            return include;
        }

        public void IncludeTo(ClassDefinition cls,
                              FeatureModifier featureModifier)
        {
        }
    }

    public class FeatureModifier : Node
    {
        string name;
        string newName;
        IncludeModifier newModifier;

        public FeatureModifier(string name,
                               string newName,
                               IncludeModifier newModifier,
                               Location location)
            : base(location)
        {
            this.name = name;
            this.newName = newName;
            this.newModifier = newModifier;
        }

        public string Name
        {
            get { return name; }
        }

        public string NewName
        {
            get { return newName; }
        }

        public IncludeModifier NewModifier
        {
            get { return newModifier; }
        }

        public override void Accept(NodeVisitor visitor)
        {
        }
    }
}
