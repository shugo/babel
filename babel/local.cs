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
    public abstract class LocalVariable
    {
        protected string name;
        protected TypeData localType;
        protected bool isTypecaseVariable;

        public LocalVariable(string name, TypeData localType,
                             bool isTypecaseVariable)
        {
            this.name = name;
            this.localType = localType;
            this.isTypecaseVariable = isTypecaseVariable;
        }

        public virtual string Name
        {
            get { return name; }
        }

        public virtual TypeData LocalType
        {
            get { return localType; }
        }

        public virtual Type RawType
        {
            get { return localType.RawType; }
        }

        public virtual bool IsTypecaseVariable
        {
            get { return isTypecaseVariable; }
            set { isTypecaseVariable = value; }
        }

        public abstract void Declare(ILGenerator ilGenerator);
        public abstract void EmitStorePrefix(ILGenerator ilGenerator);
        public abstract void EmitStore(ILGenerator ilGenerator);
        public abstract void EmitLoad(ILGenerator ilGenerator);
        public abstract void EmitLoadAddress(ILGenerator ilGenerator);
    }

    public abstract class LocalVariableStack : Stack
    {
        public virtual LocalVariable GetLocal(string name)
        {
            foreach (Hashtable tbl in this) {
                LocalVariable local = (LocalVariable) tbl[name];
                if (local != null) {
                    return local;
                }
            }
            return null;
        }

        public abstract LocalVariable CreateLocal(string name, TypeData type,
                                                  bool isTypecaseVariable);

        public virtual LocalVariable AddLocal(string name, TypeData type,
                                              bool isTypecaseVariable)
        {
            Hashtable tbl = (Hashtable) Peek();
            LocalVariable local = CreateLocal(name, type, isTypecaseVariable);
            tbl.Add(name, local);
            return local;
        }

        public virtual LocalVariable AddLocal(string name, TypeData type)
        {
            return AddLocal(name, type, false);
        }
    }

    public class RoutineLocalVariable : LocalVariable
    {
        protected LocalBuilder localBuilder;

        public RoutineLocalVariable(string name, TypeData localType,
                                    bool isTypecaseVariable)
            : base(name, localType, isTypecaseVariable)
        {
            localBuilder = null;
        }

        public RoutineLocalVariable(string name, TypeData localType)
            : this(name, localType, false) {}

        public override void Declare(ILGenerator ilGenerator)
        {
            localBuilder = ilGenerator.DeclareLocal(LocalType.RawType);
        }

        public override void EmitStorePrefix(ILGenerator ilGenerator)
        {
        }

        public override void EmitStore(ILGenerator ilGenerator)
        {
            ilGenerator.Emit(OpCodes.Stloc, localBuilder);
        }

        public override void EmitLoad(ILGenerator ilGenerator)
        {
            ilGenerator.Emit(OpCodes.Ldloc, localBuilder);
        }

        public override void EmitLoadAddress(ILGenerator ilGenerator)
        {
            ilGenerator.Emit(OpCodes.Ldloca, localBuilder);
        }
    }

    public class RoutineLocalVariableStack : LocalVariableStack
    {
        public override LocalVariable CreateLocal(string name, TypeData type,
                                                  bool isTypecaseVariable)
        {
            return new RoutineLocalVariable(name, type, isTypecaseVariable);
        }
    }

    public class IterLocalVariable : LocalVariable
    {
        protected TypeBuilder enumerator;
        protected int index;
        protected FieldBuilder fieldBuilder;

        public IterLocalVariable(string name, TypeData localType,
                                 bool isTypecaseVariable,
                                 TypeBuilder enumerator,
                                 int index)
            : base(name, localType, isTypecaseVariable)
        {
            this.enumerator = enumerator;
            this.index = index;
            fieldBuilder = null;
        }

        public override void Declare(ILGenerator ilGenerator)
        {
            fieldBuilder =
                enumerator.DefineField("__local" + index + "_" + Name,
                                       LocalType.RawType,
                                       FieldAttributes.Private);
        }

        public override void EmitStorePrefix(ILGenerator ilGenerator)
        {
            ilGenerator.Emit(OpCodes.Ldarg_0);
        }

        public override void EmitStore(ILGenerator ilGenerator)
        {
            ilGenerator.Emit(OpCodes.Stfld, fieldBuilder);
        }

        public override void EmitLoad(ILGenerator ilGenerator)
        {
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
        }

        public override void EmitLoadAddress(ILGenerator ilGenerator)
        {
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldflda, fieldBuilder);
        }
    }

    public class IterLocalVariableStack : LocalVariableStack
    {
        protected TypeBuilder enumerator;
        protected int count;

        public IterLocalVariableStack(TypeBuilder enumerator)
        {
            this.enumerator = enumerator;
            count = 0;
        }

        public override LocalVariable CreateLocal(string name, TypeData type,
                                                  bool isTypecaseVariable)
        {
            return new IterLocalVariable(name, type, isTypecaseVariable,
                                         enumerator, count++);
        }
    }
}
