/*
 * parameter.cs: routine parameters
 *
 * Copyright (C) 2003 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;

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
}
