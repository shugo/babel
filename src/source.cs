/*
 * source.cs
 *
 * Copyright (C) 2003-2004 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Collections;
using System.Text;

namespace Babell.Compiler {
    public class SourceFile : CompositeNode {
        protected string _namespace;
        protected ArrayList importedNamespaces;

        public SourceFile() : base()
        {
            _namespace = null;
            importedNamespaces = new ArrayList();
        }

        public virtual string Namespace {
            get { return _namespace; }

            set {
                _namespace = value;
                ImportNamespace(value);
            }
        }

        public virtual ArrayList ImportedNamespaces {
            get { return importedNamespaces; }
        }

        public virtual void ImportNamespace(string ns)
        {
            importedNamespaces.Add(ns);
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitSourceFile(this);
        }
    }
}
