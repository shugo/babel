/*
 * type.cs: Sather types
 *
 * Copyright (C) 2003-2004 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

using Babel.Core;

namespace Babel.Compiler {
    public abstract class TypeData {
        protected TypeManager typeManager;
        protected Type rawType;
        protected ArrayList parents;
        protected ArrayList ancestors;

        public TypeData(TypeManager typeManager, Type rawType)
        {
            this.typeManager = typeManager;
            this.rawType = rawType;
        }

        public virtual Type RawType {
            get { return rawType; }
            set { rawType = value; }
        }
        
        public virtual string Name {
            get { return rawType.Name; }
        }
        
        public virtual string FullName {
            get { return rawType.FullName.Replace(".", "::"); }
        }

        public override string ToString()
        {
            return FullName;
        }

        public virtual bool IsVoid {
            get { return rawType == typeof(void); }
        }

        public virtual bool IsSubtypeOf(TypeData supertype)
        {
            if (supertype == null)
                return false;
            if (supertype.RawType == rawType)
                return true;
            if (supertype.RawType == typeof(object))
                return true;
            if (Ancestors.Contains(supertype))
                return true;
            Type adapter =
                typeManager.GetSupertypingAdapter(supertype.RawType, rawType);
            return adapter != null;
        }

        public virtual bool IsAbstract {
            get { return rawType.IsInterface; }
        }

        public virtual ArrayList Parents {
            get {
                if (parents == null) {
                    Type[] ifaces = RawType.GetInterfaces();
                    parents = new ArrayList();
                    foreach (Type iface in ifaces) {
                        parents.Add(typeManager.GetTypeData(iface));
                    }
                }
                return parents;
            }

            set
            {
                parents = value;
            }
        }

        public virtual ArrayList Ancestors {
            get {
                if (ancestors == null)
                    ancestors = ExtractAncestors(Parents);
                return ancestors;
            }
        }

        public virtual ArrayList ExtractAncestors(ArrayList parents)
        {
            ArrayList ancestors = new ArrayList();
            foreach (TypeData parent in parents) {
                foreach (TypeData anc in parent.Ancestors) {
                    if (!ancestors.Contains(anc))
                        ancestors.Add(anc);
                }
                if (!ancestors.Contains(parent))
                    ancestors.Add(parent);
            }
            return ancestors;
        }

        public virtual Type[] ParentRawTypes
        {
            get { return GetRawTypes(Parents); }
        }

        public virtual Type[] AncestorRawTypes
        {
            get { return GetRawTypes(Parents); }
        }

        protected virtual Type[] GetRawTypes(ArrayList types)
        {
            Type[] rawTypes = new Type[types.Count];
            for (int i = 0; i < rawTypes.Length; i++) {
                rawTypes[i] = ((TypeData) types[i]).RawType;
            }
            return rawTypes;
        }

        public virtual bool IsValueType {
            get {
                return rawType.IsValueType;
            }
        }

        public virtual bool IsByRef {
            get {
                return rawType.IsByRef;
            }
        }

        public virtual ArrayList GetGenericArguments()
        {
            ArrayList args = new ArrayList();
            foreach (Type type in RawType.GetGenericArguments()) {
                args.Add(typeManager.GetTypeData(type));
            }
            return args;
        }

        public virtual bool ContainsGenericParameters {
            get {
                return rawType.ContainsGenericParameters;
            }
        }

        public virtual bool IsGenericTypeDefinition {
            get {
                return rawType.IsGenericTypeDefinition;
            }
        }

        public virtual TypeData GetGenericTypeDefinition()
        {
            return typeManager.GetTypeData(rawType.GetGenericTypeDefinition());
        }

        public virtual bool IsGenericInstance {
            get {
                return rawType.IsGenericInstance;
            }
        }

        public virtual bool IsTypeParameter {
            get {
                return false;
            }
        }

        public virtual TypeData BindGenericParameters(TypedNodeList parameters)
        {
            Type type = RawType.BindGenericParameters(parameters.NodeTypes);
            return typeManager.GetTypeData(type);
        }

        public virtual TypeData ElementType {
            get {
                return typeManager.GetTypeData(rawType.GetElementType());
            }
        }

        public virtual TypeData ReferenceType {
            get {
                string refTypeName = rawType.FullName + "&";
                return typeManager.GetType(refTypeName);
            }
        }

        public abstract ArrayList Constructors {
            get;
        }

        public abstract ArrayList Methods {
            get;
        }

        public virtual MethodData GetMethodData(MethodInfo method)
        {
            foreach (MethodData m in Methods) {
                if (m.MethodInfo == method)
                    return m;
            }
            return null;
        }

        public virtual ArrayList AncestorMethods {
            get {
                ArrayList result = new ArrayList();
                foreach (TypeData ancestor in Ancestors) {
                    ArrayList methods = ancestor.Methods;
                    result.AddRange(methods);
                }
                return result;
            }
        }

        public virtual ConstructorData
            LookupConstructor(TypedNodeList arguments)
        {
            ArrayList candidates = new ArrayList();
            foreach (ConstructorData constructor in Constructors) {
                if (constructor.DeclaringType != typeManager.ObType &&
                    constructor.Match(arguments))
                    candidates.Add(constructor);
            }
            if (candidates.Count == 0)
                throw new LookupMethodException("no match");
            if (candidates.Count == 1)
                return (ConstructorData) candidates[0];
            return (ConstructorData) SelectBestOverload(candidates, arguments);
        }

        public virtual MethodData LookupMethod(string name,
                                               TypedNodeList arguments,
                                               bool hasReturnValue)
        {
            ArrayList candidates = new ArrayList();
            foreach (MethodData method in Methods) {
                if (method.Match(name, arguments, hasReturnValue))
                    candidates.Add(method);
            }
            if (candidates.Count == 0)
                throw new LookupMethodException("no match");
            if (candidates.Count == 1)
                return (MethodData) candidates[0];
            return (MethodData) SelectBestOverload(candidates, arguments);
        }

        protected virtual MethodBaseData
            SelectBestOverload(ArrayList candidates,
                               TypedNodeList arguments)
        {
            ArrayList winners = null;
            MethodBaseData firstMethod = (MethodBaseData) candidates[0];

            int pos = 0;
            foreach (ModalExpression arg in arguments) {
                if (arg.Mode == ArgumentMode.InOut) {
                    if (winners == null)
                        winners = candidates;
                }
                else {
                    ArrayList currentPosWinners = new ArrayList();
                    ParameterData firstMethodParameter =
                        (ParameterData) firstMethod.Parameters[pos];
                    TypeData t = firstMethodParameter.ParameterType;
                    TypeData bestType;
                    if (t.IsByRef)
                        bestType = t.ElementType;
                    else
                        bestType = t;
                    foreach (MethodBaseData method in candidates) {
                        ParameterData param =
                            (ParameterData) method.Parameters[pos];
                        switch (arg.Mode) {
                        case ArgumentMode.In:
                        case ArgumentMode.Once:
                            if (param.ParameterType.IsSubtypeOf(bestType)) {
                                if (param.ParameterType != bestType) {
                                    bestType = param.ParameterType;
                                    currentPosWinners.Clear();
                                }
                                currentPosWinners.Add(method);
                            }
                            break;
                        case ArgumentMode.Out:
                            TypeData paramType =
                                param.ParameterType.ElementType;
                            if (bestType.IsSubtypeOf(paramType)) {
                                if (paramType != bestType) {
                                    bestType = paramType;
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
                        foreach (MethodBaseData m in winners) {
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
            return (MethodBaseData) winners[0];
        }

        public virtual MethodData LookupMethod(string name)
        {
            ArrayList candidates = new ArrayList();
            foreach (MethodData method in Methods) {
                if (method.Name == name)
                    candidates.Add(method);
            }
            if (candidates.Count == 0)
                throw new LookupMethodException("no match");
            if (candidates.Count > 1)
                throw new LookupMethodException("multiple matches");
            return (MethodData) candidates[0];
        }
    }

    public class PredefinedTypeData : TypeData {
        protected ArrayList constructors;
        protected ArrayList methods;

        public PredefinedTypeData(TypeManager typeManager, Type rawType)
            : base(typeManager, rawType)
        {
            constructors = null;
            methods = null;
        }

        public override ArrayList Constructors {
            get {
                if (constructors == null) {
                    ConstructorInfo[] constructorInfos =
                        rawType.GetConstructors(BindingFlags.Instance |
                                                BindingFlags.Public |
                                                BindingFlags.NonPublic);
                    constructors = new ArrayList();
                    foreach (ConstructorInfo c in constructorInfos) {
                        PredefinedConstructorData pcd =
                            new PredefinedConstructorData(typeManager, c);
                        constructors.Add(pcd);
                    }
                }
                return constructors;
            }
        }

        public override ArrayList Methods {
            get {
                if (methods == null) {
                    MethodInfo[] methodInfos =
                        rawType.GetMethods(BindingFlags.Instance |
                                           BindingFlags.Static |
                                           BindingFlags.Public |
                                           BindingFlags.NonPublic);
                    methods = new ArrayList();
                    foreach (MethodInfo m in methodInfos) {
                        methods.Add(CreateMethodData(m));
                    }
                }
                return methods;
            }
        }

        protected virtual MethodData CreateMethodData(MethodInfo method)
        {
            return new PredefinedMethodData(typeManager, method);
        }
    }

    public class GenericInstanceTypeData : PredefinedTypeData {
        public GenericInstanceTypeData(TypeManager typeManager,
                                       Type rawType)
            : base(typeManager, rawType)
        {
        }

        protected override MethodData CreateMethodData(MethodInfo method)
        {
            return new GenericInstanceMethodData(typeManager, method);
        }
    }

    public class BuiltinTypeData : PredefinedTypeData {
        protected string name;
        protected Type builtinMethodContainer;

        public BuiltinTypeData(TypeManager typeManager,
                               Type rawType, string name)
            : base(typeManager, rawType)
        {
            this.name = name.ToUpper();
            this.builtinMethodContainer = null;
        }
        
        public override string Name {
            get { return name; }
        }
        
        public override string FullName {
            get { return name; }
        }
        
        public virtual Type BuiltinMethodContainer {
            get { return builtinMethodContainer; }
            set { builtinMethodContainer = value; }
        }

        public override ArrayList Methods {
            get {
                if (methods == null) {
                    methods = base.Methods;
                    if (builtinMethodContainer != null) {
                        MethodInfo[] methodInfos =
                            builtinMethodContainer.
                            GetMethods(BindingFlags.Static |
                                       BindingFlags.Public);
                        foreach (MethodInfo m in methodInfos) {
                            methods.Add(new BuiltinMethodData(typeManager, m));
                        }
                    }
                }
                return methods;
            }
        }
    }

    public class UserDefinedTypeData : TypeData {
        protected ArrayList constructors;
        protected ArrayList methods;

        public UserDefinedTypeData(TypeManager typeManager,
                                   TypeBuilder typeBuilder)
            : base(typeManager, typeBuilder)
        {
            constructors = new ArrayList();
            methods = new ArrayList();
        }

        public override ArrayList Constructors {
            get { return constructors; }
        }

        public override ArrayList Methods {
            get { return methods; }
        }
    }

    public class TypeParameterData : TypeData {
        protected TypeData constrainingType;

        public TypeParameterData(TypeManager typeManager,
                                 Type rawType,
                                 TypeData constrainingType)
            : base(typeManager, rawType)
        {
            this.constrainingType = constrainingType;
        }

        public override string FullName {
            get { return Name; }
        }

        public override ArrayList Constructors {
            get {
                return constrainingType.Constructors;
            }
        }

        public override ArrayList Methods {
            get {
                return constrainingType.Methods;
            }
        }

        public override bool IsTypeParameter {
            get {
                return true;
            }
        }
    }

    public class LookupMethodException : Exception {
        public LookupMethodException(string message) : base(message) {}
    }
}
