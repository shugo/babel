/*
 * attribute.cs: custom attributes
 *
 * Copyright (C) 2003-2004 Shugo Maeda
 * Licensed under the terms of the GNU LGPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

namespace Babel.Core {
    public class BabelNameAttribute : Attribute {
        protected string name;

        public BabelNameAttribute(string name)
        {
            this.name = name;
        }

        public virtual string Name {
            get { return name; }
        }
    }

    public enum ArgumentMode {
        In,
        Out,
        InOut,
        Once
    }

    public class ArgumentModeAttribute : Attribute {
        protected ArgumentMode mode;

        public ArgumentModeAttribute(ArgumentMode mode)
        {
            this.mode = mode;
        }

        public virtual ArgumentMode Mode {
            get { return mode; }
        }
    }

    public class IterCreatorNameAttribute : Attribute {
        protected string name;

        public IterCreatorNameAttribute(string name)
        {
            this.name = name;
        }

        public virtual string Name {
            get { return name; }
        }
    }

    public class IterCreatorAttribute : Attribute {
        public IterCreatorAttribute() {}
    }

    public class IterReturnTypeAttribute : Attribute {
        protected Type returnType;

        public IterReturnTypeAttribute(Type returnType)
        {
            this.returnType = returnType;
        }

        public virtual Type ReturnType {
            get {
                if (returnType == null)
                    return typeof(void);
                return returnType;
            }
        }
    }

    public class SupertypingAdapterAttribute : Attribute {
        protected Type adapteeType;
        protected Type adapterType;

        public SupertypingAdapterAttribute(Type adapteeType, Type adapterType)
        {
            this.adapteeType = adapteeType;
            this.adapterType = adapterType;
        }

        public virtual Type AdapteeType {
            get { return adapteeType; }
        }

        public virtual Type AdapterType {
            get { return adapterType; }
        }
    }
}
