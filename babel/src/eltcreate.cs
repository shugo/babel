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

using Babel.Base;

namespace Babel.Compiler {
    public class TypeElementCreatingVisitor : AbstractNodeVisitor {
        protected Program program;
        protected TypeManager typeManager;
        protected Report report;
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
                ancestorMethods =
                    typeManager.GetAncestorMethods(cls.TypeData);
            }
            cls.Children.Accept(this);
            if (cls.Kind != ClassKind.Abstract && ancestorMethods.Count > 0) {
                foreach (MethodInfo method in ancestorMethods) {
                    report.Error(cls.Location,
                                 "no implementation for {0} in {1}",
                                 typeManager.GetMethodInfo(method),
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
                MethodInfo[] adapteeMethods =
                    typeManager.GetMethods(adapter.AdapteeType);
                ArrayList supertypeMethods =
                    typeManager.GetAncestorMethods(adapter.TypeData);
                foreach (MethodInfo adapteeMethod in adapteeMethods) {
                    ArrayList conformableMethods =
                        CheckMethodConformance(adapteeMethod, supertypeMethods);
                    foreach (MethodInfo m in conformableMethods) {
                        AddAdapterMethod(adapter, m, adapteeMethod);
                    }
                }
                TypeData builtinMethodContainer =
                    typeManager.GetBuiltinMethodContainer(adapter.AdapteeType);
                if (builtinMethodContainer != null) {
                    MethodInfo[] adapteeBuiltinMethods =
                        typeManager.GetMethods(builtinMethodContainer);
                    foreach (MethodInfo adapteeMethod in adapteeBuiltinMethods) {
                        ArrayList conformableMethods =
                            CheckBuiltinMethodConformance(adapteeMethod,
                                                          supertypeMethods);
                        foreach (MethodInfo m in conformableMethods) {
                            AddAdapterMethod(adapter, m, adapteeMethod);
                        }
                    }
                }
                foreach (MethodInfo method in supertypeMethods) {
                    report.Error(cls.Location,
                                 "no implementation for {0} in {1}",
                                 typeManager.GetMethodInfo(method),
                                 adapter.AdapteeType.FullName);
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
                                             TypeAttributes.NestedPublic,
                                             typeof(object));

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
            foreach (MethodInfo m in conformableMethods) {
                iterTypeAncestors[i++] = m.ReturnType;
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
            TypeData type = typeManager.GetType(typeSpecifier.Name);
            if (type == null) {
                report.Error(typeSpecifier.Location,
                             "there is no class named {0}",
                             typeSpecifier.Name);
                return;
            }
            typeSpecifier.NodeType = type;
        }

        protected virtual bool
        ConflictMethod(string name,
                       TypedNodeList arguments,
                       TypeData returnType,
                       MethodInfo method)
        {
            if (name != typeManager.GetMethodName(method))
                return false;
            ParameterInfo[] parameters = typeManager.GetParameters(method);
            if (arguments.Length != parameters.Length)
                return false;
            if (returnType.IsVoid !=
                typeManager.GetReturnType(method).IsVoid)
                return false;

            bool conflict = false;
            bool abs = false;
            bool sameArgs = true;
            int i = 0;
            foreach (Argument arg in arguments) {
                ParameterInfo param = parameters[i++];
                TypeData type1 = arg.NodeType;
                TypeData type2 = typeManager.GetTypeData(param.ParameterType);
                
                if (type1 != type2)
                    sameArgs = false;
                if (type1 != type2 &&
                    !type1.IsAbstract && !type2.IsAbstract) {
                    return false;
                }
                else {
                    if (type1.IsAbstract && type2.IsAbstract) {
                        abs = true; 
                        if (!(type1.IsSubtypeOf(type2) ||
                              type2.IsSubtypeOf(type1)))
                            conflict = true;
                    }
                    else {
                        if (type1 != type2)
                            return false;
                    }
                }
            }
            return (abs && conflict) || !abs || sameArgs;
        }

        protected virtual void
        CheckMethodConfliction(TypeData type,
                               string name,
                               TypeData returnType,
                               TypedNodeList arguments)
        {
            MethodInfo[] methods = typeManager.GetMethods(type);
            foreach (MethodInfo m in methods) {
                if (ConflictMethod(name, arguments, returnType, m)) {
                    string minfo1 = typeManager.GetMethodInfo(type,
                                                              name,
                                                              arguments,
                                                              returnType);
                    string minfo2 = typeManager.GetMethodInfo(m);
                    string msg = "The signature: " + minfo1 +
                        " conflicts with the earlier feature signature: " +
                        minfo2;
                    throw new MethodConflictionException(msg);
                }
            }
        }

        protected virtual bool
        ConformMethod(string name,
                      TypedNodeList arguments,
                      TypeData returnType,
                      MethodInfo method)
        {
            if (name != typeManager.GetMethodName(method))
                return false;
            ParameterInfo[] parameters = typeManager.GetParameters(method);
            if (arguments.Length != parameters.Length)
                return false;
            if (returnType != typeManager.GetReturnType(method))
                return false;

            int i = 0;
            foreach (Argument arg in arguments) {
                ParameterInfo param = parameters[i++];
                ArgumentMode mode = typeManager.GetArgumentMode(param);
                if (arg.Mode != mode)
                    return false;
                if (arg.NodeType !=
                    typeManager.GetTypeData(param.ParameterType))
                    return false;
            }
            return true;
        }

        protected virtual ArrayList
        CheckMethodConformance(TypeData type,
                               string name,
                               TypeData returnType,
                               TypedNodeList arguments,
                               ArrayList ancestorMethods)
        {
            ArrayList conformableMethods = new ArrayList();
            foreach (MethodInfo m in ancestorMethods) {
                if (ConformMethod(name, arguments, returnType, m))
                    conformableMethods.Add(m);
            }
            foreach (MethodInfo m in conformableMethods) {
                ancestorMethods.Remove(m);
            }
            return conformableMethods;
        }

        protected virtual bool
        ConformMethod(MethodInfo method1,
                      MethodInfo method2)
        {
            if (typeManager.GetMethodName(method1) !=
                typeManager.GetMethodName(method2))
                return false;
            ParameterInfo[] parameters1 = typeManager.GetParameters(method1);
            ParameterInfo[] parameters2 = typeManager.GetParameters(method2);
            if (parameters1.Length != parameters2.Length)
                return false;
            if (typeManager.GetReturnType(method1) !=
                typeManager.GetReturnType(method2))
                return false;

            for (int i = 0; i < parameters1.Length; i++) {
                ParameterInfo param1 = parameters1[i];
                ParameterInfo param2 = parameters2[i];
                ArgumentMode mode1 = typeManager.GetArgumentMode(param1);
                ArgumentMode mode2 = typeManager.GetArgumentMode(param2);
                if (mode1 != mode2)
                    return false;
                if (param1.ParameterType != param2.ParameterType)
                    return false;
            }
            return true;
        }

        protected virtual ArrayList
        CheckMethodConformance(MethodInfo method,
                               ArrayList ancestorMethods)
        {
            ArrayList conformableMethods = new ArrayList();
            foreach (MethodInfo m in ancestorMethods) {
                if (ConformMethod(method, m))
                    conformableMethods.Add(m);
            }
            foreach (MethodInfo m in conformableMethods) {
                ancestorMethods.Remove(m);
            }
            return conformableMethods;
        }

        protected virtual bool
        ConformBuiltinMethod(MethodInfo method1,
                      MethodInfo method2)
        {
            if (typeManager.GetMethodName(method1) !=
                typeManager.GetMethodName(method2))
                return false;
            ParameterInfo[] parameters1 = typeManager.GetParameters(method1);
            ParameterInfo[] parameters2 = typeManager.GetParameters(method2);
            if (parameters1.Length != parameters2.Length + 1)
                return false;
            if (typeManager.GetReturnType(method1) !=
                typeManager.GetReturnType(method2))
                return false;

            for (int i = 1; i < parameters1.Length; i++) {
                ParameterInfo param1 = parameters1[i];
                ParameterInfo param2 = parameters2[i - 1];
                ArgumentMode mode1 = typeManager.GetArgumentMode(param1);
                ArgumentMode mode2 = typeManager.GetArgumentMode(param2);
                if (mode1 != mode2)
                    return false;
                if (param1.ParameterType != param2.ParameterType)
                    return false;
            }
            return true;
        }

        protected virtual ArrayList
        CheckBuiltinMethodConformance(MethodInfo method,
                                      ArrayList ancestorMethods)
        {
            ArrayList conformableMethods = new ArrayList();
            foreach (MethodInfo m in ancestorMethods) {
                if (ConformBuiltinMethod(method, m))
                    conformableMethods.Add(m);
            }
            foreach (MethodInfo m in conformableMethods) {
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

            }
            typeManager.AddMethod(type, method);
            typeManager.AddParameters(method, parameters);
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
            for (int i = 0; i < parameters.Length; i++) {
                ParameterBuilder pb =
                    constructor.DefineParameter(i + 1, 0, null);
                parameters[i] =
                    new Parameter(pb, paramTypes[i], constructor);
            }
            typeManager.AddConstructor(type, constructor);
            typeManager.AddParameters(constructor, parameters);
            return constructor;
        }
    }

    public class MethodConflictionException : Exception {
        public MethodConflictionException(string message) : base(message) {}
    }
}