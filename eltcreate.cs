/*
 * eltcreate.cs: create elements of types
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
    public class TypeElementCreatingVisitor : AbstractNodeVisitor
    {
        Program program;
        TypeManager typeManager;
        Report report;
        ClassDefinition currentClass;

        public TypeElementCreatingVisitor(Report report)
        {
            this.report = report;
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

        public override void VisitAbstractRoutine(AbstractRoutineSignature rout)
        {
            rout.Arguments.Accept(this);
            rout.ReturnType.Accept(this);
            TypeBuilder typeBuilder = currentClass.TypeBuilder;
            rout.MethodBuilder = DefineMethod(typeBuilder, rout.Name,
                                              MethodAttributes.Public |
                                              MethodAttributes.Virtual |
                                              MethodAttributes.Abstract |
                                              MethodAttributes.HideBySig,
                                              rout.ReturnType.NodeType,
                                              rout.Arguments);
        }

        public override void VisitConst(ConstDefinition constDef)
        {
            constDef.TypeSpecifier.Accept(this);
            Type constType = constDef.TypeSpecifier.NodeType;
            TypeBuilder typeBuilder = currentClass.TypeBuilder;
            MethodAttributes readerAttributes =
                MethodAttributes.Virtual | MethodAttributes.HideBySig;
            switch (constDef.Modifier) {
            case ConstModifier.None:
                readerAttributes |= MethodAttributes.Public;
                break;
            case ConstModifier.Private:
                readerAttributes |= MethodAttributes.Private;
                break;
            }
            constDef.FieldBuilder =
                typeBuilder.DefineField("_" + constDef.Name,
                                        constType,
                                        FieldAttributes.Private |
                                        FieldAttributes.Static |
                                        FieldAttributes.Literal);
            constDef.FieldBuilder.SetConstant(constDef.Value);
            constDef.Reader = DefineReader(typeBuilder, constDef.Name,
                                           readerAttributes,
                                           constDef.TypeSpecifier);
        }

        public override void VisitSharedAttr(SharedAttrDefinition attr)
        {
            attr.TypeSpecifier.Accept(this);
            Type attrType = attr.TypeSpecifier.NodeType;
            TypeBuilder typeBuilder = currentClass.TypeBuilder;
            MethodAttributes readerAttributes =
                MethodAttributes.Virtual | MethodAttributes.HideBySig;
            MethodAttributes writerAttributes =
                MethodAttributes.Virtual | MethodAttributes.HideBySig;
            switch (attr.Modifier) {
            case AttrModifier.None:
                readerAttributes |= MethodAttributes.Public;
                writerAttributes |= MethodAttributes.Public;
                break;
            case AttrModifier.Private:
                readerAttributes |= MethodAttributes.Private;
                writerAttributes |= MethodAttributes.Private;
                break;
            case AttrModifier.Readonly:
                readerAttributes |= MethodAttributes.Public;
                writerAttributes |= MethodAttributes.Private;
                break;
            }
            attr.FieldBuilder =
                typeBuilder.DefineField("_" + attr.Name,
                                        attrType,
                                        FieldAttributes.Private |
                                        FieldAttributes.Static);
            attr.Reader = DefineReader(typeBuilder, attr.Name,
                                       readerAttributes,
                                       attr.TypeSpecifier);
            attr.Writer = DefineWriter(typeBuilder, attr.Name,
                                       readerAttributes,
                                       attr.TypeSpecifier);
        }

        public override void VisitAttr(AttrDefinition attr)
        {
            attr.TypeSpecifier.Accept(this);
            Type attrType = attr.TypeSpecifier.NodeType;
            TypeBuilder typeBuilder = currentClass.TypeBuilder;
            MethodAttributes readerAttributes =
                MethodAttributes.Virtual | MethodAttributes.HideBySig;
            MethodAttributes writerAttributes =
                MethodAttributes.Virtual | MethodAttributes.HideBySig;
            switch (attr.Modifier) {
            case AttrModifier.None:
                readerAttributes |= MethodAttributes.Public;
                writerAttributes |= MethodAttributes.Public;
                break;
            case AttrModifier.Private:
                readerAttributes |= MethodAttributes.Private;
                writerAttributes |= MethodAttributes.Private;
                break;
            case AttrModifier.Readonly:
                readerAttributes |= MethodAttributes.Public;
                writerAttributes |= MethodAttributes.Private;
                break;
            }
            attr.FieldBuilder =
                typeBuilder.DefineField("_" + attr.Name,
                                        attrType,
                                        FieldAttributes.Private);
            attr.Reader = DefineReader(typeBuilder, attr.Name,
                                       readerAttributes,
                                       attr.TypeSpecifier);
            attr.Writer = DefineWriter(typeBuilder, attr.Name,
                                       readerAttributes,
                                       attr.TypeSpecifier);
        }

        public override void VisitRoutine(RoutineDefinition rout)
        {
            rout.Arguments.Accept(this);
            rout.ReturnType.Accept(this);
            TypeBuilder typeBuilder = currentClass.TypeBuilder;
            MethodAttributes attributes =
                MethodAttributes.Virtual | MethodAttributes.HideBySig;
            switch (rout.Modifier) {
            case RoutineModifier.None:
                attributes |= MethodAttributes.Public;
                break;
            case RoutineModifier.Private:
                attributes |= MethodAttributes.Private;
                break;
            }
            rout.MethodBuilder =
                DefineMethod(typeBuilder, rout.Name, attributes,
                             rout.ReturnType.NodeType, rout.Arguments);
        }

        public override void VisitArgument(Argument arg)
        {
            arg.TypeSpecifier.Accept(this);
            arg.NodeType = arg.TypeSpecifier.NodeType;
            if (arg.Mode == ArgumentMode.Out ||
                arg.Mode == ArgumentMode.InOut) {
                // FIXME
            }
        }

        public override void VisitInclude(IncludeClause include)
        {
            include.TypeSpecifier.Accept(this);
            ClassDefinition cls =
                typeManager.GetClass(include.TypeSpecifier.Name);
            if (cls == null) {
                report.Error(include.Location,
                             "cannot find the definition of {0}",
                             include.TypeSpecifier.Name);
                return;
            }
            foreach (ClassElement element in cls.Children) {
                FeatureModifier featureModifier = null;
                foreach (FeatureModifier fm in include.FeatureModifierList) {
                    if (fm.Name == element.Name) {
                        featureModifier = fm;
                        break;
                    }
                }
                if (featureModifier == null) {
                    featureModifier = new FeatureModifier(element.Name,
                                                          element.Name,
                                                          include.Modifier,
                                                          Location.Null);
                }
                else {
                    if (featureModifier.NewName == "")
                        continue;
                }
                element.IncludeTo(currentClass, featureModifier);
            }
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

        protected MethodBuilder DefineMethod(TypeBuilder type,
                                             string name,
                                             MethodAttributes attributes,
                                             Type returnType,
                                             TypedNodeList arguments)
        {
            MethodBuilder method =
                type.DefineMethod(name,
                                  attributes,
                                  returnType,
                                  arguments.NodeTypes);
            ParameterInfo[] parameters = new ParameterInfo[arguments.Length];
            foreach (Argument arg in arguments) {
                ParameterAttributes attrs = 0;
                switch (arg.Mode) {
                case ArgumentMode.Out:
                    attrs |= ParameterAttributes.Out;
                    break;
                case ArgumentMode.InOut:
                    attrs |= ParameterAttributes.In;
                    attrs |= ParameterAttributes.Out;
                    break;
                }
                ParameterBuilder pb =
                    method.DefineParameter(arg.Index, attrs, arg.Name);
                parameters[arg.Index - 1] =
                    new Parameter(pb, arg.NodeType, method);
            }
            typeManager.AddMethod(type, method);
            typeManager.AddParameters(method, parameters);
            return method;
        }

        protected MethodBuilder DefineReader(TypeBuilder type, string name,
                                             MethodAttributes attributes,
                                             TypeSpecifier attrType)
        {
            return DefineMethod(type, name, attributes,
                                attrType.NodeType, new TypedNodeList());
        }

        protected MethodBuilder DefineWriter(TypeBuilder type, string name,
                                             MethodAttributes attributes,
                                             TypeSpecifier attrType)
        {
            Argument arg = new Argument(ArgumentMode.In, "value",
                                        attrType, Location.Null);
            arg.Index = 1;
            arg.NodeType = attrType.NodeType;
            TypedNodeList args = new TypedNodeList(arg);
            return DefineMethod(type, name, attributes,
                                typeof(void), args);
        }
    }
}
