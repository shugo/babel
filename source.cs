/*
 * program.cs
 *
 * Copyright (C) 2003 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Collections;
using System.Text;

namespace Babel.Sather.Compiler
{
    public class SourceFile : CompositeNode
    {
        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitSourceFile(this);
        }
    }
}
