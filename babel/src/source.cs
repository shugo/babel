/*
 * source.cs
 *
 * Copyright (C) 2003-2004 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Collections;
using System.Text;

namespace Babel.Compiler {
    public class SourceFile : CompositeNode {
        protected ArrayList importedNamespaces;

        public SourceFile() : base()
        {
            importedNamespaces = new ArrayList();
        }

        public virtual ArrayList ImportedNamespaces {
            get { return importedNamespaces; }
        }

        public virtual void AddNamespace(string ns)
        {
            importedNamespaces.Add(ns);
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitSourceFile(this);
        }
    }
}
