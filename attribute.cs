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
        ArgumentMode mode;

        public ArgumentModeAttribute(ArgumentMode mode)
        {
            this.mode = mode;
        }

        public ArgumentMode Mode
        {
            get { return mode; }
        }
    }

    public class IterReturnTypeAttribute : Attribute
    {
        Type type;

        public IterReturnTypeAttribute(Type type)
        {
            this.type = type;
        }

        public Type Type
        {
            get { return type; }
        }
    }
}
