/*
 * local.cs: local variables
 *
 * Copyright (C) 2003 Shugo Maeda
 * Licensed under the terms of the GNU GPL
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;

namespace Babel.Sather.Compiler
{
    public class LocalVariable
    {
        string name;
        Type localType;
        bool isTypecaseVariable;
        LocalBuilder localBuilder;

        public LocalVariable(string name, Type localType,
                             bool isTypecaseVariable)
        {
            this.name = name;
            this.localType = localType;
            this.isTypecaseVariable = isTypecaseVariable;
            localBuilder = null;
        }

        public LocalVariable(string name, Type localType)
            : this(name, localType, false) {}

        public string Name
        {
            get { return name; }
        }

        public Type LocalType
        {
            get { return localType; }
        }

        public bool IsTypecaseVariable
        {
            get { return isTypecaseVariable; }
            set { isTypecaseVariable = value; }
        }

        public LocalBuilder LocalBuilder
        {
            get { return localBuilder; }
            set { localBuilder = value; }
        }
    }

    public class LocalVariableStack : Stack
    {
        public LocalVariable Get(string name)
        {
            foreach (Hashtable tbl in this) {
                LocalVariable local = (LocalVariable) tbl[name];
                if (local != null) {
                    return local;
                }
            }
            return null;
        }

        public LocalVariable Add(string name, Type type)
        {
            Hashtable tbl = (Hashtable) Peek();
            LocalVariable local = new LocalVariable(name, type);
            tbl.Add(name, local);
            return local;
        }
    }
}
