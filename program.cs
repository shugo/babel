/*
 * program.cs
 *
 * Copyright (C) 2003 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Text;

namespace Babel.Sather.Compiler
{
    public class Program : CompositeNode
    {
        protected AssemblyBuilder assembly;
        protected ModuleBuilder module;
        protected TypeManager typeManager;

        public Program(string name)
        {
            AppDomain domain = AppDomain.CurrentDomain;
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.Name = name;
            assembly =
                domain.DefineDynamicAssembly(assemblyName,
                                             AssemblyBuilderAccess.RunAndSave);
            module = assembly.DefineDynamicModule(name);
            typeManager = new TypeManager();
            typeManager.AddModule(module);
        }

        public virtual AssemblyBuilder Assembly
        {
            get { return assembly; }
        }

        public virtual ModuleBuilder Module
        {
            get { return module; }
        }

        public virtual TypeManager TypeManager
        {
            get { return typeManager; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitProgram(this);
        }
    }
}
