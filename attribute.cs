/*
 * attribute.cs: custom attributes
 *
 * Copyright (C) 2003 Shugo Maeda
 * Licensed under the terms of the GNU LGPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

namespace Babel.Sather.Base
{
    public class SatherNameAttribute : Attribute
    {
        protected string name;

        public SatherNameAttribute(string name)
        {
            this.name = name;
        }

        public virtual string Name
        {
            get { return name; }
        }
    }

    public enum ArgumentMode
    {
        In,
        Out,
        InOut,
        Once
    }

    public class ArgumentModeAttribute : Attribute
    {
        protected ArgumentMode mode;

        public ArgumentModeAttribute(ArgumentMode mode)
        {
            this.mode = mode;
        }

        public virtual ArgumentMode Mode
        {
            get { return mode; }
        }
    }

    public class IterReturnTypeAttribute : Attribute
    {
        protected Type returnType;

        public IterReturnTypeAttribute(Type returnType)
        {
            this.returnType = returnType;
        }

        public virtual Type ReturnType
        {
            get
            {
                if (returnType == null)
                    return typeof(void);
                return returnType;
            }
        }
    }
}
