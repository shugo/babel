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

    public class IterTypeAttribute : Attribute
    {
        protected Type iterType;

        public IterTypeAttribute(Type iterType)
        {
            this.iterType = iterType;
        }

        public virtual Type IterType
        {
            get { return iterType; }
        }
    }
}
