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
    public abstract class MethodBaseData {
        protected TypeManager typeManager;
        protected MethodBase methodBase;
        protected ParameterList parameterList;

        protected MethodBaseData(TypeManager typeManager,
                                 MethodBase methodBase)
        {
            this.typeManager = typeManager;
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
    }

    public abstract class ConstructorData : MethodBaseData {
        protected ConstructorData(TypeManager typeManager,
                                  MethodInfo methodInfo)
            : base(typeManager, methodInfo)
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
                                         MethodInfo methodInfo)
            : base(typeManager, methodInfo)
        {
            parameterList = new PredefinedParameterList(typeManager,
                                                        methodInfo);
        }
    }

    public class UserDefinedConstructorData : ConstructorData {
        public UserDefinedConstructorData(TypeManager typeManager,
                                          MethodBuilder methodBuilder)
            : base(typeManager, methodBuilder)
        {
            parameterList = new UserDefinedParameterList();
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
    }
}
