/*
 * typemanager.cs: type manager
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
    public class TypeManager
    {
        Hashtable builtinTypes;
        Hashtable builtinTypeNames;
        Hashtable classes;
        Hashtable parentsTable;
        Hashtable ancestorsTable;
        Hashtable visitingClasses;
        ClassDefinition currentClass;

        public TypeManager()
        {
            builtinTypes = new Hashtable();
            builtinTypeNames = new Hashtable();
            classes = new Hashtable();
            parentsTable = new Hashtable();
            ancestorsTable = new Hashtable();
            visitingClasses = new Hashtable();
            InitBuiltinTypes();
        }

        protected void InitBuiltinTypes()
        {
            AddBuiltinType("$OB", typeof(object));
            AddBuiltinType("BOOL", typeof(bool));
            AddBuiltinType("INT", typeof(int));
            AddBuiltinType("FLT", typeof(float));
            AddBuiltinType("CHAR", typeof(char));
            AddBuiltinType("STR", typeof(string));
        }

        protected void AddBuiltinType(string name, Type type)
        {
            builtinTypes.Add(name, type);
            builtinTypeNames.Add(type, name);
        }

        public Type GetPredefinedType(string name)
        {
            Type type = (Type) builtinTypes[name];
            if (type != null)
                return type;
            return Type.GetType(name);
        }

        public void AddClass(ClassDefinition cls)
        {
            classes.Add(cls.Name, cls);
        }

        public ClassDefinition GetClass(string name)
        {
            return (ClassDefinition) classes[name];
        }

        public void AddTypeGraph(Type type, Type[] parents, Type[] ancestors)
        {
            parentsTable.Add(type, parents);
            ancestorsTable.Add(type, ancestors);
        }

        public Type[] GetParents(Type type)
        {
            Type[] parents = (Type[]) parentsTable[type];
            if (parents != null)
                return parents;
            return type.GetInterfaces();
        }

        public Type[] GetAncestors(Type type)
        {
            Type[] ancestors = (Type[]) ancestorsTable[type];
            if (ancestors != null)
                return ancestors;
            return ExtractAncestors(GetParents(type));
        }

        public Type[] ExtractAncestors(Type[] parents)
        {
            Hashtable tbl = new Hashtable();
            foreach (Type parent in parents) {
                tbl[parent] = parent;
                foreach (Type anc in GetAncestors(parent)) {
                    tbl[anc] = anc;
                }
            }
            Type[] ancestors = new Type[tbl.Count];
            tbl.Values.CopyTo(ancestors, 0);
            return ancestors;
        }

        public bool IsSubtype(Type type, Type supertype)
        {
            if (type == null)
                return false;
            if (type == supertype)
                return true;
            if (supertype == typeof(object))
                return true;
            Type[] ancestors = GetAncestors(type);
            return ((IList) ancestors).Contains(supertype);
        }

        public string GetTypeName(Type type)
        {
            if (type == null)
                return "_";
            string name = (string) builtinTypeNames[type];
            if (name != null)
                return name;
            return type.Name;
        }
    }
}
