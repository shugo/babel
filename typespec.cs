/*
 * typespec.cs: type specifier
 *
 * Copyright (C) 2003 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Collections;

namespace Babel.Sather.Compiler
{
    public enum TypeKind
    {
        Normal,
        Routine,
        Iterator,
        Same
    }

    public class TypeSpecifier : TypedNode
    {
        string name;
        TypeKind kind;

        public static TypeSpecifier Null;

        static TypeSpecifier()
        {
            Null = new NullTypeSpecifier();
        }

        public TypeSpecifier(string name, TypeKind kind, Location location)
            : base(location)
        {
            this.name = name;
            this.kind = kind;
        }

        public override string ToString()
        {
            return name;
        }

        public string Name
        {
            get { return name; }
        }

        public TypeKind Kind
        {
            get { return kind; }
        }

        public virtual bool IsNull()
        {
            return false;
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitTypeSpecifier(this);
        }
    }

    public class NullTypeSpecifier : TypeSpecifier
    {
        public NullTypeSpecifier()
            : base(null, TypeKind.Normal, Location.Null)
        {
            NodeType = typeof(void);
        }

        public override bool IsNull()
        {
            return true;
        }

        public override void Accept(NodeVisitor visitor) {}
    }
}
