/*
 * parameter.cs: routine parameters
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
    public class Parameter : ParameterInfo {
        public Parameter(ParameterBuilder pb, Type type,
                         MemberInfo member)
        {
            this.ClassImpl = type;
            this.MemberImpl = member;
            this.NameImpl = pb.Name;
            this.PositionImpl = pb.Position - 1;
            this.AttrsImpl = (ParameterAttributes) pb.Attributes;
        }
    }

    public class ParameterData {
        protected TypeManager typeManager;
        protected ParameterInfo rawParameter;

        public ParameterData(TypeManager typeManager,
                             ParameterInfo rawParameter)
        {
            this.typeManager = typeManager;
            this.rawParameter = rawParameter;
        }

        public virtual ParameterInfo RawParameter {
            get { return rawParameter; }
        }
    }

    public class PredefinedParameterData : ParameterData {
        public PredefinedParameterData(TypeManager typeManager,
                                       ParameterInfo rawParameter)
            : base(typeManager, rawParameter)
        {
        }
        
        public virtual ArgumentMode Mode {
            get {
                object[] attrs =
                    RawParameter.GetCustomAttributes(
                        typeof(ArgumentModeAttribute), false);
                if (attrs == null || attrs.Length == 0)
                    return ArgumentMode.In;
                else
                    return ((ArgumentModeAttribute) attrs[0]).Mode;
            }
        }
    }

    public class UserDefinedParameterData : ParameterData {
        protected ArgumentMode mode;

        public UserDefinedParameterData(TypeManager typeManager,
                                        ParameterInfo rawParameter,
                                        ArgumentMode mode)
            : base(typeManager, rawParameter)
        {
            this.mode = mode;
        }

        public UserDefinedParameterData(TypeManager typeManager,
                                        ParameterInfo rawParameter)
            : this(typeManager, rawParameter, ArgumentMode.In)
        {
        }
        
        public virtual ArgumentMode Mode {
            get { return mode; }
            
            set { mode = value; }
        }
    }

    public interface ParameterList
    {
        ArrayList Parameters
        {
            get;
        }
    }

    public class PredefinedParameterList : ParameterList {
        protected TypeManager typeManager;
        protected MethodBase methodBase;
        protected ArrayList parameters;

        public PredefinedParameterList(TypeManager typeManager,
                                       MethodBase methodBase)
        {
            this.typeManager = typeManager;
            this.methodBase = methodBase;
            parameters = null;
        }

        public ArrayList Parameters
        {
            get {
                if (parameters == null) {
                    parameters = new ArrayList();
                    foreach (ParameterInfo param in methodBase.GetParameters()) {
                        ParameterData paramData =
                            new ParameterData(typeManager, param);
                        parameters.Add(paramData);
                    }
                }
                return parameters;
            }
        }
    }

    public class UserDefinedParameterList : ParameterList {
        protected ArrayList parameters;

        public UserDefinedParameterList()
        {
            parameters = null;
        }

        public ArrayList Parameters
        {
            get {
                return parameters;
            }

            set
            {
                parameters = value;
            }
        }
    }
}
