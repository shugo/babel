/*
 * program.cs
 *
 * Copyright (C) 2003 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Text;

namespace Babel.Sather.Compiler
{
    public enum Target
    {
        Exe, WinExe, Library, Module
    }

    public class Program : CompositeNode
    {
        protected AssemblyBuilder assembly;
        protected ModuleBuilder module;
        protected TypeManager typeManager;
        protected Target target;

        public Program(string fileName, Target target)
        {
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            AppDomain domain = AppDomain.CurrentDomain;
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.Name = baseName;
            assembly =
                domain.DefineDynamicAssembly(assemblyName,
                                             AssemblyBuilderAccess.RunAndSave);
            module = assembly.DefineDynamicModule(baseName, fileName);
            typeManager = new TypeManager();
            typeManager.AddModule(module);
            this.target = target;
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

        public virtual Target Target
        {
            get { return target; }
        }

        public override void Accept(NodeVisitor visitor)
        {
            visitor.VisitProgram(this);
        }
    }
}
