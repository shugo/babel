/*
 * eltcreate.cs: create elements of types
 *
 * Copyright (C) 2003-2004 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

using Babell.Base;

namespace Babell.Compiler {
    public class TypeElementCreatingVisitor : AbstractNodeVisitor {
        protected Program program;
        protected TypeManager typeManager;
        protected Report report;
        protected SourceFile currentSouceFile;
        protected ClassDefinition currentClass;
        protected int iterCount;
        protected ArrayList ancestorMethods;

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
            currentSouceFile = sourceFile;
            sourceFile.Children.Accept(this);
        }

        public override void VisitClass(ClassDefinition cls)
        {
            ClassDefinition prevClass = currentClass;
            int prevIterCount = iterCount;
            ArrayList prevAncestorMethods = ancestorMethods;
            currentClass = cls;
            iterCount = 0;
            if (cls.Kind == ClassKind.Abstract) {
                ancestorMethods = null;
            }
            else {
                ancestorMethods = cls.TypeData.AncestorMethods;
            }
            cls.Children.Accept(this);
            if (cls.Kind != ClassKind.Abstract && ancestorMethods.Count > 0) {
                foreach (MethodData method in ancestorMethods) {
                    report.Error(cls.Location,
                                 "no implementation for {0} in {1}",
                                 method,
                                 cls.TypeData.FullName);
                }
            }
            CreateAdapterMethods(cls);
            currentClass = prevClass;
            iterCount = prevIterCount;
            ancestorMethods = prevAncestorMethods;
        }

        protected virtual void CreateAdapterMethods(ClassDefinition cls)
        {
            foreach (SupertypingAdapter adapter in cls.Adapters) {
                adapter.AdapteeField =
                    adapter.TypeBuilder.DefineField("__adaptee",
                                                    adapter.AdapteeType.RawType,
                                                    FieldAttributes.Private);
                Type[] types = new Type[] { adapter.AdapteeType.RawType };
                adapter.Constructor =
                    DefineConstructor(adapter.TypeBuilder,
                                      MethodAttributes.Public,
                                      CallingConventions.Standard,
                                      types);
                ArrayList adapteeMethods =
                    adapter.AdapteeType.Methods;
                ArrayList supertypeMethods =
                    adapter.TypeData.AncestorMethods;
                foreach (MethodData adapteeMethod in adapteeMethods) {
                    ArrayList conformableMethods =
                        CheckMethodConformance(adapteeMethod, supertypeMethods);
                    foreach (MethodData m in conformableMethods) {
                        AddAdapterMethod(adapter,
                                         m.MethodInfo,
                                         adapteeMethod.MethodInfo);
                    }
                }
                TypeData builtinMethodContainer =
                    typeManager.GetBuiltinMethodContainer(adapter.AdapteeType);
                if (builtinMethodContainer != null) {
                    ArrayList adapteeBuiltinMethods =
                        builtinMethodContainer.Methods;
                    foreach (MethodData adapteeMethod in adapteeBuiltinMethods) {
                        ArrayList conformableMethods =
                            CheckMethodConformance(adapteeMethod,
                                                   supertypeMethods);
                        foreach (MethodData m in conformableMethods) {
                            AddAdapterMethod(adapter,
                                             m.MethodInfo,
                                             adapteeMethod.MethodInfo);
                        }
                    }
                }
                foreach (MethodData method in supertypeMethods) {
                    report.Error(cls.Location,
                                 "no implementation for {0} in {1}",
                                 method, adapter.AdapteeType.FullName);
                }
            }
        }

        protected virtual void AddAdapterMethod(SupertypingAdapter adapter,
                                                MethodInfo method,
                                                MethodInfo adapteeMethod)
        {
            ParameterInfo[] parameters =
                typeManager.GetParameters(method);
            Type[] paramTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++) {
                paramTypes[i] = parameters[i].ParameterType;
            }
            MethodBuilder mb =
                adapter.TypeBuilder.
                DefineMethod(method.DeclaringType.FullName + "." +
                             method.Name,
                             MethodAttributes.Private |
                             MethodAttributes.Virtual |
                             MethodAttributes.HideBySig,
                             method.ReturnType,
                             paramTypes);
            SupertypingAdapterMethod adapterMethod =
                new SupertypingAdapterMethod(mb, adapteeMethod,
                                             parameters.Length);
            adapter.Methods.Add(adapterMethod);
            adapter.TypeBuilder.DefineMethodOverride(mb, method);
        }

        public override void VisitAbstractRoutine(AbstractRoutineSignature rout)
        {
            rout.Arguments.Accept(this);
            rout.ReturnType.Accept(this);
            TypeBuilder typeBuilder = currentClass.TypeBuilder;
            try {
                CheckMethodConfliction(currentClass.TypeData, rout.Name,
                                       rout.ReturnType.NodeType,
                                       rout.Arguments);
            }
            catch (MethodConflictionException e) {
                report.Error(rout.Location, e.Message);
                return;
            }
            rout.MethodBuilder = DefineMethod(typeBuilder, rout.Name,
                                              MethodAttributes.Public |
                                              MethodAttributes.Virtual |
                                              MethodAttributes.Abstract |
                                              MethodAttributes.HideBySig,
                                              rout.ReturnType.NodeType,
                                              rout.Arguments);
        }

        public override void VisitAbstractIter(AbstractIterSignature iter)
        {
            string baseName = iter.Name.Substring(0, iter.Name.Length - 1);

            iter.Arguments.Accept(this);
            iter.MoveNextArguments.Accept(this);
            iter.ReturnType.Accept(this);
            TypeBuilder typeBuilder = currentClass.TypeBuilder;
            try {
                CheckMethodConfliction(currentClass.TypeData, iter.Name,
                                       iter.ReturnType.NodeType,
                                       iter.Arguments);
            }
            catch (MethodConflictionException e) {
                report.Error(iter.Location, e.Message);
                return;
            }

            iter.TypeBuilder =
                typeBuilder.DefineNestedType("__itertype" + iterCount +
                                             "_" + baseName,
                                             TypeAttributes.Interface |
                                             TypeAttributes.Abstract |
                                             TypeAttributes.NestedPublic);

            iter.MoveNext =
                DefineMethod(iter.TypeBuilder, "MoveNext",
                             MethodAttributes.Public |
                             MethodAttributes.Virtual |
                             MethodAttributes.Abstract |
                             MethodAttributes.HideBySig,
                             typeManager.BoolType,
                             iter.MoveNextArguments);
            if (!iter.ReturnType.IsNull) {
                iter.GetCurrent =
                    DefineMethod(iter.TypeBuilder, "GetCurrent",
                                 MethodAttributes.Public |
                                 MethodAttributes.Virtual |
                                 MethodAttributes.Abstract |
                                 MethodAttributes.HideBySig,
                                 iter.ReturnType.NodeType,
                                 new TypedNodeList());
            }

            iter.MethodBuilder =
                DefineMethod(typeBuilder, "__iter_" + baseName,
                             MethodAttributes.Public |
                             MethodAttributes.Virtual |
                             MethodAttributes.Abstract |
                             MethodAttributes.HideBySig,
                             typeManager.GetTypeData(iter.TypeBuilder),
                             iter.Arguments);
            
            typeManager.AddSatherName(iter.MethodBuilder, iter.Name);
            typeManager.AddIterReturnType(iter.MethodBuilder,
                                          iter.ReturnType.NodeType);

            iterCount++;
        }

        public override void VisitConst(ConstDefinition constDef)
        {
            constDef.TypeSpecifier.Accept(this);
            TypeData constType = constDef.TypeSpecifier.NodeType;
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
                                        constType.RawType,
                                        FieldAttributes.Private |
                                        FieldAttributes.Static |
                                        FieldAttributes.Literal);
            constDef.FieldBuilder.SetConstant(constDef.Value);
            try {
                constDef.Reader = DefineReader(typeBuilder, constDef.Name,
                                               readerAttributes,
                                               constDef.TypeSpecifier);
            }
            catch (MethodConflictionException e) {
                report.Error(constDef.Location, e.Message);
            }
        }
        
        public override void VisitSharedAttr(SharedAttrDefinition attr)
        {
            attr.TypeSpecifier.Accept(this);
            Type attrType = attr.TypeSpecifier.RawType;
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
            try {
                attr.Reader = DefineReader(typeBuilder, attr.Name,
                                           readerAttributes,
                                           attr.TypeSpecifier);
                attr.Writer = DefineWriter(typeBuilder, attr.Name,
                                           readerAttributes,
                                           attr.TypeSpecifier);
            }
            catch (MethodConflictionException e) {
                report.Error(attr.Location, e.Message);
            }
        }

        public override void VisitAttr(AttrDefinition attr)
        {
            attr.TypeSpecifier.Accept(this);
            Type attrType = attr.TypeSpecifier.RawType;
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
            try {
                attr.Reader = DefineReader(typeBuilder, attr.Name,
                                           readerAttributes,
                                           attr.TypeSpecifier);
                attr.Writer = DefineWriter(typeBuilder, attr.Name,
                                           readerAttributes,
                                           attr.TypeSpecifier);
            }
            catch (MethodConflictionException e) {
                report.Error(attr.Location, e.Message);
            }
        }

        public override void VisitRoutine(RoutineDefinition rout)
        {
            rout.Arguments.Accept(this);
            rout.ReturnType.Accept(this);
            TypeBuilder typeBuilder = currentClass.TypeBuilder;
            try {
                CheckMethodConfliction(currentClass.TypeData, rout.Name,
                                       rout.ReturnType.NodeType,
                                       rout.Arguments);
            }
            catch (MethodConflictionException e) {
                report.Error(rout.Location, e.Message);
                return;
            }
            CheckMethodConformance(currentClass.TypeData, rout.Name,
                                   rout.ReturnType.NodeType,
                                   rout.Arguments,
                                   ancestorMethods);

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

        public override void VisitIter(IterDefinition iter)
        {
            iter.Arguments.Accept(this);
            iter.MoveNextArguments.Accept(this);
            iter.ReturnType.Accept(this);
            TypeBuilder typeBuilder = currentClass.TypeBuilder;
            try {
                CheckMethodConfliction(currentClass.TypeData, iter.Name,
                                       iter.ReturnType.NodeType,
                                       iter.Arguments);
            }
            catch (MethodConflictionException e) {
                report.Error(iter.Location, e.Message);
                return;
            }
            ArrayList conformableMethods =
                CheckMethodConformance(currentClass.TypeData, iter.Name,
                                       iter.ReturnType.NodeType,
                                       iter.Arguments,
                                       ancestorMethods);

            string baseName = iter.Name.Substring(0, iter.Name.Length - 1);

            Type[] iterTypeAncestors = new Type[conformableMethods.Count];
            int i = 0;
            foreach (MethodData m in conformableMethods) {
                iterTypeAncestors[i++] = m.MethodInfo.ReturnType;
            }

            iter.TypeBuilder =
                typeBuilder.DefineNestedType("__itertype" + iterCount +
                                             "_" + baseName,
                                             TypeAttributes.Class |
                                             TypeAttributes.NestedPublic,
                                             typeof(object),
                                             iterTypeAncestors);
            
            ArrayList list = new ArrayList();
            list.Add(typeBuilder);
            foreach (Argument arg in iter.Arguments) {
                if (arg.Mode == ArgumentMode.Once)
                    list.Add(arg.RawType);
            }
            Type[] constructorParams = new Type[list.Count];
            list.CopyTo(constructorParams);

            iter.Constructor =
                DefineConstructor(iter.TypeBuilder,
                                  MethodAttributes.Public,
                                  CallingConventions.Standard,
                                  constructorParams);
            iter.Self =
                iter.TypeBuilder.DefineField("__self",
                                             typeBuilder,
                                             FieldAttributes.Private);
            iter.CurrentPosition =
                iter.TypeBuilder.DefineField("__currentPosition",
                                             typeof(int),
                                             FieldAttributes.Private);

            iter.MoveNext =
                DefineMethod(iter.TypeBuilder, "MoveNext",
                             MethodAttributes.Virtual |
                             MethodAttributes.HideBySig |
                             MethodAttributes.Public,
                             typeManager.BoolType,
                             iter.MoveNextArguments);
            if (!iter.ReturnType.IsNull) {
                iter.Current =
                    iter.TypeBuilder.DefineField("__current",
                                                 iter.ReturnType.RawType,
                                                 FieldAttributes.Private);
                iter.GetCurrent =
                    DefineMethod(iter.TypeBuilder, "GetCurrent",
                                 MethodAttributes.Virtual |
                                 MethodAttributes.HideBySig |
                                 MethodAttributes.Public,
                                 iter.ReturnType.NodeType,
                                 new TypedNodeList());
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
                DefineMethod(typeBuilder, "__iter_" + baseName, attributes,
                             typeManager.GetTypeData(iter.TypeBuilder),
                             iter.Arguments);

            typeManager.AddSatherName(iter.MethodBuilder, iter.Name);
            typeManager.AddIterReturnType(iter.MethodBuilder,
                                          iter.ReturnType.NodeType);

            foreach (Type t in iterTypeAncestors) {
                MethodBuilder bridgeMethod =
                    DefineMethod(typeBuilder, "__iter_" + baseName, attributes,
                                 typeManager.GetTypeData(t),
                                 iter.Arguments);
                iter.BridgeMethods.Add(bridgeMethod);
            }

            iterCount++;
        }

        public override void VisitArgument(Argument arg)
        {
            arg.TypeSpecifier.Accept(this);
            arg.NodeType = arg.TypeSpecifier.NodeType;
            if (arg.Mode == ArgumentMode.Out ||
                arg.Mode == ArgumentMode.InOut) {
                arg.NodeType = arg.NodeType.ReferenceType;
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
                typeSpecifier.NodeType =
                    typeManager.GetTypeData(currentClass.TypeBuilder);
                return;
            }
            TypeData type =
                typeManager.GetType(typeSpecifier.Name,
                                    currentSouceFile.ImportedNamespaces);
            if (type == null) {
                report.Error(typeSpecifier.Location,
                             "there is no class named {0}",
                             typeSpecifier);
                return;
            }
            typeSpecifier.NodeType = type;
        }

        protected virtual void
        CheckMethodConfliction(TypeData type,
                               string name,
                               TypeData returnType,
                               TypedNodeList arguments)
        {
            MethodSignature sig =
                new MethodSignature(type, name, returnType, arguments);

            foreach (MethodData m in type.Methods) {
                if (m.ConflictWith(sig)) {
                    string msg = "The signature: " + sig +
                        " conflicts with the earlier feature signature: " + m;
                    throw new MethodConflictionException(msg);
                }
            }
        }

        protected virtual ArrayList
        CheckMethodConformance(TypeData type,
                               string name,
                               TypeData returnType,
                               TypedNodeList arguments,
                               ArrayList ancestorMethods)
        {
            MethodSignature sig =
                new MethodSignature(type, name, returnType, arguments);
            ArrayList conformableMethods = new ArrayList();
            foreach (MethodData m in ancestorMethods) {
                if (sig.ConformTo(m))
                    conformableMethods.Add(m);
            }
            foreach (MethodData m in conformableMethods) {
                ancestorMethods.Remove(m);
            }
            return conformableMethods;
        }

        protected virtual ArrayList
        CheckMethodConformance(MethodData method,
                               ArrayList ancestorMethods)
        {
            ArrayList conformableMethods = new ArrayList();
            foreach (MethodData m in ancestorMethods) {
                if (method.ConformTo(m))
                    conformableMethods.Add(m);
            }
            foreach (MethodData m in conformableMethods) {
                ancestorMethods.Remove(m);
            }
            return conformableMethods;
        }

        protected virtual MethodBuilder
        DefineMethod(TypeBuilder type,
                     string name,
                     MethodAttributes attributes,
                     TypeData returnType,
                     TypedNodeList arguments)
        {
            MethodBuilder method =
                type.DefineMethod(name,
                                  attributes,
                                  returnType.RawType,
                                  arguments.NodeTypes);
            ParameterInfo[] parameters = new ParameterInfo[arguments.Length];
            ArrayList parameterList = new ArrayList();
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
                    new Parameter(pb, arg.NodeType.RawType, method);
                ArgumentModeAttribute attr =
                    new ArgumentModeAttribute(arg.Mode);
                typeManager.AddCustomAttribute(parameters[arg.Index - 1], attr);

                UserDefinedParameterData paramData =
                    new UserDefinedParameterData(typeManager,
                                                 parameters[arg.Index - 1],
                                                 arg.Mode);
                parameterList.Add(paramData);
            }
            UserDefinedMethodData methodData =
                typeManager.AddMethod(type, method);
            typeManager.AddParameters(method, parameters);
            methodData.Parameters = parameterList;
            return method;
        }

        protected virtual MethodBuilder
        DefineReader(TypeBuilder type,
                     string name,
                     MethodAttributes attributes,
                     TypeSpecifier attrType)
        {
            CheckMethodConfliction(typeManager.GetTypeData(type),
                                   name,
                                   attrType.NodeType,
                                   new TypedNodeList());
            return DefineMethod(type, name, attributes,
                                attrType.NodeType, new TypedNodeList());
        }

        protected virtual MethodBuilder
        DefineWriter(TypeBuilder type,
                     string name,
                     MethodAttributes attributes,
                     TypeSpecifier attrType)
        {
            Argument arg = new Argument(ArgumentMode.In, "value",
                                        attrType, Location.Null);
            arg.Index = 1;
            arg.NodeType = attrType.NodeType;
            TypedNodeList args = new TypedNodeList(arg);
            CheckMethodConfliction(typeManager.GetTypeData(type),
                                   name,
                                   typeManager.VoidType,
                                   new TypedNodeList());
            return DefineMethod(type, name, attributes,
                                typeManager.VoidType, args);
        }

        protected virtual ConstructorBuilder
        DefineConstructor(TypeBuilder type,
                          MethodAttributes attributes,
                          CallingConventions callingConventions,
                          Type[] paramTypes)
        {
            ConstructorBuilder constructor =
                type.DefineConstructor(attributes,
                                       callingConventions,
                                       paramTypes);
            ParameterInfo[] parameters = new ParameterInfo[paramTypes.Length];
            ArrayList parameterList = new ArrayList();
            for (int i = 0; i < parameters.Length; i++) {
                ParameterBuilder pb =
                    constructor.DefineParameter(i + 1, 0, null);
                parameters[i] =
                    new Parameter(pb, paramTypes[i], constructor);
                UserDefinedParameterData paramData =
                    new UserDefinedParameterData(typeManager,
                                                 parameters[i],
                                                 ArgumentMode.In);
                parameterList.Add(paramData);
            }
            UserDefinedConstructorData constructorData =
                typeManager.AddConstructor(type, constructor);
            typeManager.AddParameters(constructor, parameters);
            constructorData.Parameters = parameterList;
            return constructor;
        }
    }

    public class MethodConflictionException : Exception {
        public MethodConflictionException(string message) : base(message) {}
    }
}
