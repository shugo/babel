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

using Babel.Sather.Base;

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
                                              rout.Arguments,
                                              false);
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
                             rout.ReturnType.NodeType, rout.Arguments,
                             false);
        }

        public override void VisitIter(IterDefinition iter)
        {
            iter.Arguments.Accept(this);
            iter.MoveNextArguments.Accept(this);
            iter.CreatorArguments.Accept(this);
            iter.ReturnType.Accept(this);
            TypeBuilder typeBuilder = currentClass.TypeBuilder;
            iter.Enumerator =
                typeBuilder.DefineNestedType("Enumerator_" + iter.Name,
                                             TypeAttributes.Public,
                                             typeof(object));

            ArrayList list = new ArrayList();
            list.Add(typeBuilder);
            foreach (Argument arg in iter.CreatorArguments) {
                if (arg.Mode == ArgumentMode.Once)
                    list.Add(arg.NodeType);
            }
            Type[] constructorParams = new Type[list.Count];
            list.CopyTo(constructorParams);

            iter.Constructor =
                iter.Enumerator.DefineConstructor(MethodAttributes.Public,
                                                  CallingConventions.Standard,
                                                  constructorParams);
            iter.Self =
                iter.Enumerator.DefineField("_self",
                                            typeBuilder,
                                            FieldAttributes.Private);
            iter.CurrentPosition =
                iter.Enumerator.DefineField("_currentPosition",
                                            typeof(int),
                                            FieldAttributes.Private);

            iter.MoveNext =
                DefineMethod(iter.Enumerator, "MoveNext",
                             MethodAttributes.Virtual |
                             MethodAttributes.HideBySig |
                             MethodAttributes.Public,
                             typeof(bool),
                             iter.MoveNextArguments,
                             false);
            if (!iter.ReturnType.IsNull()) {
                iter.Current =
                    iter.Enumerator.DefineField("_current",
                                                iter.ReturnType.NodeType,
                                                FieldAttributes.Private);
                iter.GetCurrent =
                    DefineMethod(iter.Enumerator, "GetCurrent",
                                 MethodAttributes.Virtual |
                                 MethodAttributes.HideBySig |
                                 MethodAttributes.Public,
                                 iter.ReturnType.NodeType,
                                 new TypedNodeList(),
                                 false);
            }

            MethodAttributes attributes =
                MethodAttributes.Virtual | MethodAttributes.HideBySig;
            switch (iter.Modifier) {
            case RoutineModifier.None:
                attributes |= MethodAttributes.Public;
                break;
            case RoutineModifier.Private:
                attributes |= MethodAttributes.Private;
                break;
            }
            iter.MethodBuilder =
                DefineMethod(typeBuilder, iter.Name, attributes,
                             iter.Enumerator, iter.CreatorArguments,
                             true);

            Type[] cparamTypes = new Type[] { typeof(Type) };
            ConstructorInfo constructor =
                typeof(IterReturnTypeAttribute).GetConstructor(cparamTypes);
            Type returnType = iter.ReturnType.NodeType;
            CustomAttributeBuilder cbuilder =
                new CustomAttributeBuilder(constructor,
                                           new object[] { returnType });
            iter.MethodBuilder.SetCustomAttribute(cbuilder);
            Attribute attr = new IterReturnTypeAttribute(returnType);
            typeManager.AddCustomAttribute(iter.MethodBuilder, attr);
        }

        public override void VisitArgument(Argument arg)
        {
            arg.TypeSpecifier.Accept(this);
            arg.NodeType = arg.TypeSpecifier.NodeType;
            if (arg.Mode == ArgumentMode.Out ||
                arg.Mode == ArgumentMode.InOut) {
                arg.NodeType = typeManager.GetReferenceType(arg.NodeType);
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
            Type type = typeManager.GetType(typeSpecifier.Name);
            if (type == null) {
                report.Error(typeSpecifier.Location,
                             "there is no class named {0}", typeSpecifier.Name);
                return;
            }
            typeSpecifier.NodeType = type;
        }

        protected MethodBuilder DefineMethod(TypeBuilder type,
                                             string name,
                                             MethodAttributes attributes,
                                             Type returnType,
                                             TypedNodeList arguments,
                                             bool isIter)
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
                Type[] cparamTypes = new Type[] { typeof(ArgumentMode) };
                ConstructorInfo constructor =
                    typeof(ArgumentModeAttribute).GetConstructor(cparamTypes);
                CustomAttributeBuilder cbuilder =
                    new CustomAttributeBuilder(constructor,
                                               new object[] { arg.Mode });
                pb.SetCustomAttribute(cbuilder);
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
                                attrType.NodeType, new TypedNodeList(),
                                false);
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
                                typeof(void), args, false);
        }
    }
}
