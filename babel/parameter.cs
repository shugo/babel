/*
 * parameter.cs: routine parameters
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
    public class Parameter : ParameterInfo
    {
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

    public class ParameterData
    {
        protected TypeManager typeManager;
        protected ParameterInfo rawParameter;

        public ParameterData(TypeManager typeManager,
                             ParameterInfo rawParameter)
        {
            this.typeManager = typeManager;
            this.rawParameter = rawParameter;
        }

        public virtual ParameterInfo RawParameter
        {
            get { return rawParameter; }
        }
    }

    public class PredefinedParameterData : ParameterData
    {
        public PredefinedParameterData(TypeManager typeManager,
                                       ParameterInfo rawParameter)
            : base(typeManager, rawParameter)
        {
        }
        
        public virtual ArgumentMode Mode
        {
            get
            {
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

    public class DefiningParameterData : ParameterData
    {
        protected ArgumentMode mode;

        public DefiningParameterData(TypeManager typeManager,
                                     ParameterInfo rawParameter)
            : base(typeManager, rawParameter)
        {
            mode = ArgumentMode.In;
        }
        
        public virtual ArgumentMode Mode
        {
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

    public class PredefinedParameterList : ParameterList
    {
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
            get
            {
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

    public class DefiningParameterList : ParameterList
    {
        protected ArrayList parameters;

        public DefiningParameterList()
        {
            parameters = null;
        }

        public ArrayList Parameters
        {
            get
            {
                return parameters;
            }

            set
            {
                parameters = value;
            }
        }
    }
}
