/*
 * typespec.cs: type specifier
 *
 * Copyright (C) 2003-2004 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Collections;

namespace Babel.Compiler {
    public enum TypeKind {
        Normal,
        Routine,
        Iterator,
        Same
    }

    public class TypeSpecifier : TypedNode {
        protected string name;
        protected TypeKind kind;
        protected TypedNodeList typeParameters;

        public static TypeSpecifier Null;

        static TypeSpecifier()
        {
            Null = new NullTypeSpecifier();
        }

        public TypeSpecifier(string name, TypeKind kind,
                             TypedNodeList typeParameters, Location location)
            : base(location)
        {
            this.name = name;
            this.kind = kind;
            this.typeParameters = typeParameters;
        }

        public TypeSpecifier(string name, TypeKind kind, Location location)
            : this(name, kind, new TypedNodeList(), location)
        {
        }

        public override string ToString()
        {
            return name.Replace(".", "::");
        }

        public virtual string Name {
            get { return name; }
        }

        public virtual TypeKind Kind {
            get { return kind; }
        }

        public virtual TypedNodeList TypeParameters {
            get { return typeParameters; }
        }

        public virtual bool IsNull {
            get { return false; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitTypeSpecifier(this);
        }
    }

    public class NullTypeSpecifier : TypeSpecifier {
        public NullTypeSpecifier()
            : base(null, TypeKind.Normal, Location.Null)
        {
            name = "System.Void";
            kind = TypeKind.Normal;
        }

        public override bool IsNull {
            get { return true; }
        }
    }
}
