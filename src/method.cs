/*
 * method.cs: Sather methods
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
    public class MethodSignature {
        protected TypeData declaringType;
        protected string name;
        protected TypeData returnType;
        protected TypedNodeList arguments;

        public MethodSignature(TypeData declaringType,
                               string name,
                               TypeData returnType,
                               TypedNodeList arguments)
        {
            this.declaringType = declaringType;
            this.name = name;
            this.returnType = returnType;
            this.arguments = arguments;
        }

        public TypeData DeclaringType {
            get { return declaringType; }
        }

        public string Name {
            get { return name; }
        }

        public TypeData ReturnType {
            get { return returnType; }
        }

        public TypedNodeList Arguments {
            get { return arguments; }
        }

        public override string ToString()
        {
            string s = DeclaringType.FullName + "::" + Name;
            if (Arguments.Length > 0) {
                s += "(";
                foreach (Argument arg in Arguments) {
                    if (arg != Arguments.First) {
                        s += ",";
                    }
                    s += arg.NodeType.FullName;
                }
                s += ")";
            }
            if (!ReturnType.IsVoid)
                s += ":" + ReturnType.FullName;
            return s;
        }

        public virtual bool ConformTo(MethodData method)
        {
            if (method.Name.ToLower() != Name.ToLower())
                return false;
            if (method.Parameters.Count != Arguments.Length)
                return false;
            if (method.ReturnType != ReturnType)
                return false;

            int i = 0;
            foreach (Argument arg in Arguments) {
                ParameterData param = (ParameterData) method.Parameters[i++];
                if (arg.Mode != param.Mode)
                    return false;
                if (arg.NodeType != param.ParameterType)
                    return false;
            }
            return true;
        }
    }

    public abstract class MethodBaseData {
        protected TypeManager typeManager;
        protected MethodBase methodBase;
        protected ParameterList parameterList;

        protected MethodBaseData(TypeManager typeManager,
                                 MethodBase methodBase)
        {
            this.typeManager = typeManager;
            this.methodBase = methodBase;
        }

        public virtual MethodBase MethodBase {
            get { return methodBase; }
        }
        
        public virtual TypeData DeclaringType {
            get {
                return typeManager.GetTypeData(MethodBase.DeclaringType);
            }
        }
        
        public virtual string Name {
            get {
                return MethodBase.Name;
            }
        }

        public virtual ArrayList Parameters {
            get {
                return parameterList.Parameters;
            }
        }

        public override string ToString()
        {
            string s = DeclaringType.FullName + "::" + Name;
            if (Parameters.Count > 0) {
                bool first = true;
                s += "(";
                foreach (ParameterData param in Parameters) {
                    if (first) {
                        first = false;
                    }
                    else {
                        s += ",";
                    }
                    s += param.ParameterType.FullName;
                }
                s += ")";
            }
            return s;
        }
    }

    public abstract class ConstructorData : MethodBaseData {
        protected ConstructorData(TypeManager typeManager,
                                  ConstructorInfo constructorInfo)
            : base(typeManager, constructorInfo)
        {
        }

        public virtual ConstructorInfo ConstructorInfo {
            get {
                return (ConstructorInfo) MethodBase;
            }
        }
    }

    public class PredefinedConstructorData : ConstructorData {
        public PredefinedConstructorData(TypeManager typeManager,
                                         ConstructorInfo constructorInfo)
            : base(typeManager, constructorInfo)
        {
            parameterList = new PredefinedParameterList(typeManager,
                                                        constructorInfo);
        }
    }

    public class UserDefinedConstructorData : ConstructorData {
        public UserDefinedConstructorData(TypeManager typeManager,
                                          ConstructorBuilder constructorBuilder)
            : base(typeManager, constructorBuilder)
        {
            parameterList = new UserDefinedParameterList();
        }

        public override ArrayList Parameters {
            set {
                ((UserDefinedParameterList) parameterList).Parameters = value;
            }
        }
    }

    public abstract class MethodData : MethodBaseData {
        protected MethodData(TypeManager typeManager,
                             MethodInfo methodInfo)
            : base(typeManager, methodInfo)
        {
        }

        public virtual MethodInfo MethodInfo {
            get {
                return (MethodInfo) MethodBase;
            }
        }

        public override string Name {
            get {
                return typeManager.GetMethodName(MethodInfo);
            }
        }

        public virtual TypeData ReturnType {
            get {
                return typeManager.GetReturnType(MethodInfo);
            }
        }

        public override string ToString()
        {
            string s = base.ToString();
            if (!ReturnType.IsVoid)
                s += ":" + ReturnType.FullName;
            return s;
        }

        public virtual bool ConflictWith(MethodSignature method)
        {
            if (Name.ToLower() != method.Name.ToLower()) {
                return false;
            }
            if (Parameters.Count != method.Arguments.Length)
                return false;
            if (ReturnType.IsVoid != method.ReturnType.IsVoid)
                return false;

            bool conflict = false;
            bool abs = false;
            bool sameArgs = true;
            int i = 0;
            foreach (Argument arg in method.Arguments) {
                ParameterData param = (ParameterData) Parameters[i++];
                TypeData type1 = arg.NodeType;
                TypeData type2 = param.ParameterType;
                
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

        public virtual bool ConformTo(MethodData method)
        {
            if (Name.ToLower() != method.Name.ToLower())
                return false;
            if (Parameters.Count != method.Parameters.Count)
                return false;
            if (ReturnType != method.ReturnType)
                return false;

            for (int i = 0; i < Parameters.Count; i++) {
                ParameterData p1 = (ParameterData) Parameters[i];
                ParameterData p2 = (ParameterData) method.Parameters[i];
                if (p1.Mode != p2.Mode)
                    return false;
                if (p1.ParameterType != p2.ParameterType)
                    return false;
            }
            return true;
        }
    }

    public class PredefinedMethodData : MethodData {
        public PredefinedMethodData(TypeManager typeManager,
                                    MethodInfo methodInfo)
            : base(typeManager, methodInfo)
        {
            parameterList = new PredefinedParameterList(typeManager,
                                                        methodInfo);
        }
    }

    public class UserDefinedMethodData : MethodData {
        public UserDefinedMethodData(TypeManager typeManager,
                                     MethodBuilder methodBuilder)
            : base(typeManager, methodBuilder)
        {
            parameterList = new UserDefinedParameterList();
        }

        public virtual MethodBuilder MethodBuilder {
            get {
                return (MethodBuilder) MethodBase;
            }
        }

        public override ArrayList Parameters {
            set {
                ((UserDefinedParameterList) parameterList).Parameters = value;
            }
        }
    }

    public class BuiltinMethodData : PredefinedMethodData {
        public BuiltinMethodData(TypeManager typeManager,
                                 MethodInfo methodInfo)
            : base(typeManager, methodInfo)
        {
            parameterList =
                new BuiltinMethodParameterList(typeManager, methodInfo);
        }
    }
}
