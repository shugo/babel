/*
 * class.cs: classes and its elements
 *
 * Copyright (C) 2003-2004 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Text;

using Babel.Core;

namespace Babel.Compiler {
    public enum ClassKind {
        Reference,
        Abstract
    }

    public class ClassDefinition : CompositeNode, ICloneable {
        protected string name;
        protected ClassKind kind;
        protected TypedNodeList typeParameters;
        protected TypedNodeList supertypes;
        protected TypedNodeList subtypes;
        protected TypeBuilder typeBuilder;
        protected TypeData typeData;
        protected ConstructorBuilder constructor;
        protected ConstructorBuilder staticConstructor;
        protected ILGenerator staticConstructorIL;
        protected ArrayList adapters;

        public ClassDefinition(string name, ClassKind kind,
                               TypedNodeList typeParameters,
                               TypedNodeList supertypes,
                               TypedNodeList subtypes,
                               Location location)
            : base(location)
        {
            this.name = name;
            this.kind = kind;
            this.typeParameters = typeParameters;
            this.supertypes = supertypes;
            this.subtypes = subtypes;
            typeBuilder = null;
            typeData = null;
            constructor = null;
            staticConstructor = null;
            staticConstructorIL = null;
            adapters = new ArrayList();
        }


        public ClassDefinition(string name, ClassKind kind,
                               TypedNodeList typeParameters,
                               TypedNodeList supertypes,
                               Location location)
            : this(name, kind, typeParameters, supertypes, null, location) {}

        public virtual string Name {
            get { return name; }
        }

        public virtual ClassKind Kind {
            get { return kind; }
        }

        public virtual TypedNodeList TypeParameters {
            get { return typeParameters; }
        }

        public virtual TypedNodeList Supertypes {
            get { return supertypes; }
        }

        public virtual TypedNodeList Subtypes {
            get { return subtypes; }
        }

        public virtual TypeBuilder TypeBuilder {
            get { return typeBuilder; }
            set { typeBuilder = value; }
        }

        public virtual TypeData TypeData {
            get { return typeData; }
            set { typeData = value; }
        }

        public virtual TypeData BoundTypeData {
            get {
                if (TypeParameters.Length > 0) {
                    TypeData td = TypeData.GetGenericTypeDefinition();
                    return td.BindGenericParameters(TypeParameters);
                }
                else {
                    return TypeData;
                }
            }
        }

        public virtual ConstructorBuilder Constructor {
            get { return constructor; }
            set { constructor = value; }
        }

        public virtual ConstructorBuilder StaticConstructor {
            get { return staticConstructor; }
            set { staticConstructor = value; }
        }

        public virtual ILGenerator StaticConstructorIL {
            get {
                if (staticConstructor != null &&
                    staticConstructorIL == null) {
                    staticConstructorIL =
                        staticConstructor.GetILGenerator();
                }
                return staticConstructorIL;
            }
        }

        public virtual ArrayList Adapters {
            get { return adapters; }
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

        public TypeData GetTypeParameter(string name)
        {
            string lowerName = name.ToLower();
            foreach (ParameterDeclaration pd in TypeParameters) {
                if (pd.Name.ToLower() == lowerName) {
                    return pd.NodeType;
                }
            }
            return null;
        }
    }

    public class ParameterDeclaration : TypedNode {
        protected string name;
        protected TypeSpecifier constrainingType;
        protected GenericTypeParameterBuilder builder;

        public ParameterDeclaration(string name,
                                    TypeSpecifier constrainingType,
                                    Location location)
            : base(location)
        {
            this.name = name;
            this.constrainingType = constrainingType;
            builder = null;
        }

        public string Name {
            get { return name; }
        }

        public TypeSpecifier ConstrainingType {
            get { return constrainingType; }
        }

        public GenericTypeParameterBuilder Builder {
            get { return builder; }
            set { builder = value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitParameterDeclaration(this);
        }
    }

    public class SupertypingAdapter {
        protected TypeData adapteeType;
        protected TypeBuilder typeBuilder;
        protected TypeData typeData;
        protected FieldBuilder adapteeField;
        protected ConstructorBuilder constructor;
        protected ArrayList methods;

        public SupertypingAdapter(TypeData adapteeType)
        {
            this.adapteeType = adapteeType;
            this.typeBuilder = null;
            this.typeData = null;
            this.adapteeField = null;
            this.constructor = null;
            this.methods = new ArrayList();
        }

        public virtual TypeData AdapteeType {
            get { return adapteeType; }
        }

        public virtual TypeBuilder TypeBuilder {
            get { return typeBuilder; }
            set { typeBuilder = value; }
        }

        public virtual TypeData TypeData {
            get { return typeData; }
            set { typeData = value; }
        }

        public virtual FieldBuilder AdapteeField {
            get { return adapteeField; }
            set { adapteeField = value; }
        }

        public virtual ConstructorBuilder Constructor {
            get { return constructor; }
            set { constructor = value; }
        }

        public virtual ArrayList Methods {
            get { return methods; }
        }
    }

    public class SupertypingAdapterMethod {
        MethodBuilder methodBuilder;
        MethodInfo adapteeMethod;
        int parameterCount;

        public SupertypingAdapterMethod(MethodBuilder methodBuilder,
                                    MethodInfo adapteeMethod,
                                    int parameterCount)
        {
            this.methodBuilder = methodBuilder;
            this.adapteeMethod = adapteeMethod;
            this.parameterCount = parameterCount;
        }

        public virtual MethodBuilder MethodBuilder {
            get { return methodBuilder; }
        }

        public virtual MethodInfo AdapteeMethod {
            get { return adapteeMethod; }
        }

        public virtual int ParameterCount {
            get { return parameterCount; }
        }
    }

    public class AbstractRoutineSignature : Node {
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
                argumentTable.Add(arg.Name.ToLower(), arg);
            }
        }

        public virtual string Name {
            get { return name; }
        }

        public virtual TypedNodeList Arguments {
            get { return arguments; }
        }

        public virtual TypeSpecifier ReturnType {
            get { return returnType; }
        }

        public virtual MethodBuilder MethodBuilder {
            get { return methodBuilder; }
            set { methodBuilder = value; }
        }

        public virtual Argument GetArgument(string name)
        {
            return (Argument) argumentTable[name.ToLower()];
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

    public class AbstractIterSignature : AbstractRoutineSignature {
        protected TypeBuilder typeBuilder;
        protected ConstructorBuilder constructor;
        protected MethodBuilder moveNext;
        protected MethodBuilder getCurrent;
        protected TypedNodeList moveNextArguments;
        protected MethodBuilder creator;

        public AbstractIterSignature(string name,
                                     TypedNodeList arguments,
                                     TypeSpecifier returnType,
                                     Location location)
            : base(name, arguments, returnType, location)
        {
            typeBuilder = null;
            constructor = null;
            moveNext = null;
            getCurrent = null;
            moveNextArguments = null;
            creator = null;
            InitArguments();
        }

        protected override void InitArguments()
        {
            int index = 1;
            int moveNextPos = 1;
            moveNextArguments = new TypedNodeList();
            argumentTable = new Hashtable();
            foreach (Argument arg in Arguments) {
                arg.Index = index++;
                if (arg.Mode != ArgumentMode.Once) {
                    Argument ma = (Argument) arg.Clone();
                    ma.Index = moveNextPos++;
                    moveNextArguments.Append(ma);
                    argumentTable.Add(ma.Name, ma);
                }
            }
        }

        public virtual TypeBuilder TypeBuilder {
            get { return typeBuilder; }
            set { typeBuilder = value; }
        }

        public virtual ConstructorBuilder Constructor {
            get { return constructor; }
            set { constructor = value; }
        }

        public virtual MethodBuilder MoveNext {
            get { return moveNext; }
            set { moveNext = value; }
        }

        public virtual MethodBuilder GetCurrent {
            get { return getCurrent; }
            set { getCurrent = value; }
        }

        public virtual TypedNodeList MoveNextArguments {
            get { return moveNextArguments; }
        }

        public virtual MethodBuilder Creator {
            get { return creator; }
            set { creator = value; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitAbstractIter(this);
        }
    }

    public interface ClassElement
    {
        string Name { get; }
        void IncludeTo(ClassDefinition cls,
                       FeatureModifier featureModifier);
    }

    public enum ConstModifier {
        None,
        Private
    }

    public class ConstDefinition : Node, ClassElement {
        protected string name;
        protected TypeSpecifier typeSpecifier;
        protected object value;
        protected ConstModifier modifier;
        protected FieldBuilder fieldBuilder;
        protected MethodBuilder reader;

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

        public virtual string Name {
            get { return name; }
        }

        public virtual TypeSpecifier TypeSpecifier {
            get { return typeSpecifier; }
        }

        public virtual object Value {
            get { return value; }
        }

        public virtual ConstModifier Modifier {
            get { return modifier; }
        }

        public virtual FieldBuilder FieldBuilder {
            get { return fieldBuilder; }
            set { fieldBuilder = value; }
        }

        public virtual MethodBuilder Reader {
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

        public virtual void IncludeTo(ClassDefinition cls,
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

    public enum AttrModifier {
        None,
        Private,
        Readonly
    }

    public class AttrDefinition : Node, ClassElement {
        protected string name;
        protected TypeSpecifier typeSpecifier;
        protected AttrModifier modifier;
        protected FieldBuilder fieldBuilder;
        protected MethodBuilder reader;
        protected MethodBuilder writer;

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

        public virtual string Name {
            get { return name; }
        }

        public virtual TypeSpecifier TypeSpecifier {
            get { return typeSpecifier; }
        }

        public virtual AttrModifier Modifier {
            get { return modifier; }
        }

        public virtual FieldBuilder FieldBuilder {
            get { return fieldBuilder; }
            set { fieldBuilder = value; }
        }

        public virtual MethodBuilder Reader {
            get { return reader; }
            set { reader = value; }
        }

        public virtual MethodBuilder Writer {
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

        public virtual void IncludeTo(ClassDefinition cls,
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


    public class SharedAttrDefinition : AttrDefinition {
        protected Expression value;

        public SharedAttrDefinition(string name,
                                    TypeSpecifier typeSpecifier,
                                    Expression value,
                                    AttrModifier modifier,
                                    Location location)
            : base(name, typeSpecifier, modifier, location)
        {
            this.value = value;
        }

        public virtual Expression Value {
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

    public enum RoutineModifier {
        None,
        Private
    }

    public class RoutineDefinition : AbstractRoutineSignature, ClassElement {
        protected StatementList statementList;
        protected RoutineModifier modifier;

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

        public virtual StatementList StatementList {
            get { return statementList; }
        }

        public virtual RoutineModifier Modifier {
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

        public virtual void IncludeTo(ClassDefinition cls,
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

    public class IterDefinition : RoutineDefinition, ClassElement {
        protected TypeBuilder typeBuilder;
        protected FieldBuilder self;
        protected FieldBuilder current;
        protected FieldBuilder currentPosition;
        protected ConstructorBuilder constructor;
        protected MethodBuilder moveNext;
        protected MethodBuilder getCurrent;
        protected Hashtable localVariables;
        protected ArrayList resumePoints;
        protected TypedNodeList moveNextArguments;
        protected MethodBuilder creator;
        protected ArrayList bridgeMethods;

        public IterDefinition(string name,
                              TypedNodeList arguments,
                              TypeSpecifier returnType,
                              StatementList statementList,
                              RoutineModifier modifier,
                              Location location)
            : base(name, arguments, returnType,
                   statementList, modifier, location)
        {
            typeBuilder = null;
            self = null;
            current = null;
            currentPosition = null;
            constructor = null;
            moveNext = null;
            getCurrent = null;
            creator = null;
            localVariables = new Hashtable();
            resumePoints = new ArrayList();
            resumePoints.Add(new ResumePoint());
            InitArguments();
            bridgeMethods = new ArrayList();
        }

        protected override void InitArguments()
        {
            int index = 1;
            int moveNextPos = 1;
            moveNextArguments = new TypedNodeList();
            argumentTable = new Hashtable();
            foreach (Argument arg in Arguments) {
                arg.Index = index++;
                if (arg.Mode != ArgumentMode.Once) {
                    Argument ma = (Argument) arg.Clone();
                    ma.Index = moveNextPos++;
                    moveNextArguments.Append(ma);
                    argumentTable.Add(ma.Name, ma);
                }
            }
        }

        public virtual TypeBuilder TypeBuilder {
            get { return typeBuilder; }
            set { typeBuilder = value; }
        }

        public virtual FieldBuilder Self {
            get { return self; }
            set { self = value; }
        }

        public virtual FieldBuilder Current {
            get { return current; }
            set { current = value; }
        }

        public virtual FieldBuilder CurrentPosition {
            get { return currentPosition; }
            set { currentPosition = value; }
        }

        public virtual ConstructorBuilder Constructor {
            get { return constructor; }
            set { constructor = value; }
        }

        public virtual MethodBuilder MoveNext {
            get { return moveNext; }
            set { moveNext = value; }
        }

        public virtual MethodBuilder GetCurrent {
            get { return getCurrent; }
            set { getCurrent = value; }
        }

        public virtual ArrayList ResumePoints {
            get { return resumePoints; }
        }

        public virtual Hashtable LocalVariables {
            get { return localVariables; }
        }

        public virtual TypedNodeList MoveNextArguments {
            get { return moveNextArguments; }
        }

        public virtual MethodBuilder Creator {
            get { return creator; }
            set { creator = value; }
        }

        public virtual ArrayList BridgeMethods {
            get { return bridgeMethods; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitIter(this);
        }
    }

    public class ResumePoint {
        protected int index;
        protected Label label;

        public ResumePoint()
        {
            index = 0;
        }

        public virtual int Index {
            get { return index; }
            set { index = value; }
        }

        public virtual Label Label {
            get { return label; }
            set { label = value; }
        }
    }

    public class Argument : TypedNode {
        protected ArgumentMode mode;
        protected string name;
        protected int index;
        protected TypeSpecifier typeSpecifier;

        public Argument(ArgumentMode mode,
                        string name, TypeSpecifier typeSpecifier,
                        Location location)
            : base(location)
        {
            this.mode = mode;
            this.name = name;
            this.typeSpecifier = typeSpecifier;
        }

        public virtual ArgumentMode Mode {
            get { return mode; }
            set { mode = value; }
        }

        public virtual string Name {
            get { return name; }
        }

        public virtual int Index {
            get { return index; }
            set { index = value; }
        }

        public virtual TypeSpecifier TypeSpecifier {
            get { return typeSpecifier; }
            set { typeSpecifier = value; }
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

    public enum IncludeModifier {
        None,
        Private,
        Readonly,
        NoChange
    }

    public class IncludeClause : Node, ClassElement {
        protected TypeSpecifier typeSpecifier;
        protected IncludeModifier modifier;
        protected NodeList featureModifierList;

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

        public virtual string Name {
            get { return ""; }
        }

        public virtual TypeSpecifier TypeSpecifier {
            get { return typeSpecifier; }
        }

        public virtual IncludeModifier Modifier {
            get { return modifier; }
        }

        public virtual NodeList FeatureModifierList {
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

        public virtual void IncludeTo(ClassDefinition cls,
                              FeatureModifier featureModifier)
        {
        }
    }

    public class FeatureModifier : Node {
        protected string name;
        protected string newName;
        protected IncludeModifier newModifier;

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

        public virtual string Name {
            get { return name; }
        }

        public virtual string NewName {
            get { return newName; }
        }

        public virtual IncludeModifier NewModifier {
            get { return newModifier; }
        }

        public override void Accept(NodeVisitor visitor)
        {
        }
    }
}
